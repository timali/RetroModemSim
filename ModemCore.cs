using System.Text.RegularExpressions;
using System.Text;
using System.Diagnostics;

namespace RetroModemSim
{
    /*************************************************************************************************************/
    /// <summary>
    /// Generic modem simulator.
    /// </summary>
    /*************************************************************************************************************/
    public abstract partial class ModemCore
    {
        // Abstract classes to be implemented in derived classes.
        /// <summary>
        /// Sends a byte to the remote host.
        /// </summary>
        /// <param name="b">The byte to send.</param>
        protected abstract void TxByteToRemoteHost(int b);

        /// <summary>
        /// Creates a connection to the remote destination.
        /// </summary>
        /// <param name="destination">A string describing the remote destination.</param>
        /// <returns>The connect command upon success, or any other response upon error.</returns>
        protected abstract CmdResponse Dial(string destination);

        /// <summary>
        /// Terminates the remote connection.
        /// </summary>
        protected abstract void HangUpModem();

        /*************************************************************************************************************/
        /// <summary>
        /// Called from derived classes when they receive data from the remote host.
        /// </summary>
        /*************************************************************************************************************/
        protected void OnRxData(int rxData)
        {
            lock (stateLock)
            {
                // If we are in online data mode, deliver the byte to the DTE. Otherwise, discard the data.
                if (state == StateEnum.Online)
                {
                    iDTE.TxByte(rxData);
                }
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Called from derived classes when they detect their connection has been disconnected.
        /// </summary>
        /*************************************************************************************************************/
        protected void OnDisconnected()
        {
            lock (stateLock)
            {
                if (connected)
                {
                    Hangup();
                    ExitOnlineDataMode();
                    SendResponse(CmdRsp.NoCarrier);
                }
            }
        }

        /// <summary>
        /// The various states the modem can be in.
        /// </summary>
        enum StateEnum
        {
            AwaitingA,
            AwaitingT,
            AwaitingCommand,
            Online,
        }

        /// <summary>
        /// All the S registers we support.
        /// </summary>
        enum SRegEnum
        {
            RingsBeforeAnswering            = 0,
            RingCount                       = 1,
            EscapeCode                      = 2,
            CR                              = 3,
            LF                              = 4,
            BS                              = 5,
            DialToneDelay                   = 6,
            CarrierDelay                    = 7,
            DialPause                       = 8,
            Unused1                         = 9,
            CarrierLossDelay                = 10,
            TouchToneDelay                  = 11,
            EscapeGuardTime                 = 12,

            LastValue
        }

        const int PETSCII_START = 0xC1, PETSCII_END = 0xDA, PETSCII_SHIFT = 0x80;
        const int RESULT_CODE_ALL = int.MaxValue;
        bool echo=true, petscii, zap, connected, halfDuplex, hideResponses, numericResponses;
        StringBuilder cmdStrBuilder;
        int escapeCharCount, resultCodeLimit = RESULT_CODE_ALL;
        StateEnum state;
        IDTE iDTE;
        Stopwatch escapeSw = new Stopwatch();
        object stateLock = new object();
        Timer EscapeSequenceTimer;
        int[] sReg = new int[(int)SRegEnum.LastValue] {
            2,          // Rings before answering
            0,          // Ring count
            '+',        // Escape code
            '\r',       // CR
            '\n',       // LF
            8,          // Backspace
            2,          // DialToneDelay
            30,         // CarrierDelay
            2,          // DialPause
            0,          // Unused1
            7,          // CarrierLossDelay
            70,         // TouchToneDelay
            50,         // EscapeGuardTime
        };

        // Protected members available to derived classes.
        protected IDiagMsg iDiagMsg;
        protected List<CommandHandler> cmdList = new List<CommandHandler>();
        protected delegate CmdResponse CmdHandlerDelegate(string cmdStr, Match match);

        /*************************************************************************************************************/
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="iDTE">The DTE instance to use.</param>
        /// <param name="iDiagMsg">The diagnostics message instance to use.</param>
        /*************************************************************************************************************/
        public ModemCore(IDTE iDTE, IDiagMsg iDiagMsg)
        {
            this.iDTE = iDTE;
            this.iDiagMsg = iDiagMsg;

            EscapeSequenceTimer = new Timer(OnEscapeSequenceTimeout);

            // Install our command handlers.
            cmdList.Add(new CommandHandler("^T$",                                   CmdToneDialing));
            cmdList.Add(new CommandHandler("^P$",                                   CmdPulseDialing));
            cmdList.Add(new CommandHandler("^Z$",                                   CmdZap));
            cmdList.Add(new CommandHandler("^O$",                                   CmdOnline));
            cmdList.Add(new CommandHandler("^C[01]?$",                              CmdCarrier));
            cmdList.Add(new CommandHandler("^E[01]?$",                              CmdEcho));
            cmdList.Add(new CommandHandler("^F[01]?$",                              CmdDuplex));
            cmdList.Add(new CommandHandler("^H[01]?$",                              CmdHangup));
            cmdList.Add(new CommandHandler("^Q[01]?$",                              CmdQuiet));
            cmdList.Add(new CommandHandler("^V[01]?$",                              CmdVerbal));
            cmdList.Add(new CommandHandler("^M[012]?$",                             CmdMonitor));
            cmdList.Add(new CommandHandler("^X[012]?$",                             CmdResultCodeSet));
            cmdList.Add(new CommandHandler("^D.*$",                                 CmdDial));
            cmdList.Add(new CommandHandler("^S(?<reg>\\d+)\\?$",                    CmdSRegQuery));
            cmdList.Add(new CommandHandler("^S(?<reg>\\d+)=(?<val>\\d+)$",          CmdSRegSet));

            // Install the generic AT command handler last because it will match any command. Do this here instead of
            // in the constructor to allow the user to install custom commands.
            cmdList.Add(new CommandHandler("", CmdAt));
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Attempts to hang up the phone and terminate the connection.
        /// </summary>
        /*************************************************************************************************************/
        void Hangup()
        {
            if (connected)
            {
                iDiagMsg.WriteLine("Hanging Up");
                iDTE.SetDCD(false);
                HangUpModem();
                connected = false;
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Enters online data mode from command mode.
        /// </summary>
        /*************************************************************************************************************/
        void EnterOnlineDataMode()
        {
            iDiagMsg.WriteLine($"Online Data Mode");

            state = StateEnum.Online;
            escapeSw.Restart();
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Dials a remote host.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdDial(string cmdStr, Match match)
        {
            bool enterOnlineMode = true;
            int subStrLen = cmdStr.Length - 1, startIdx = 1;

            // If the last character is a ';', then dial, but remain in command mode.
            if (cmdStr.EndsWith(";"))
            {
                // Remove the semicolon before giving the string to the modem for dialing.
                subStrLen--;
                enterOnlineMode = false;
            }

            // Remove the touch-tone or pulse dialing indicator if present.
            if ((cmdStr.Length >= 2) && ((cmdStr[1] == 'T') || (cmdStr[1] == 'P')))
            {
                startIdx++;
                subStrLen--;
            }

            // Remove the beginning D, and the ';' if necessary.
            cmdStr = cmdStr.Substring(startIdx, subStrLen);

            // Use our modem instance to dial the remote destination.
            iDiagMsg.WriteLine($"Dialing \"{cmdStr}\"...");
            CmdResponse cmdRsp = Dial(cmdStr);

            // See if the modem was able to connect to the destination.
            if (cmdRsp == CmdRsp.Connect)
            {
                connected = true;
                iDiagMsg.WriteLine($"Connected to \"{cmdStr}\"");

                // Inform the DTE that we're now connected (the data carrier is detected).
                iDTE.SetDCD(true);

                // Move into online mode if requested.
                if (enterOnlineMode)
                {
                    EnterOnlineDataMode();
                }
            }
            else
            {
                iDiagMsg.WriteLine($"Unable to connect to \"{cmdStr}\": {cmdRsp.ResponseStr}");
            }

            return cmdRsp;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Translates the character to ASCII from PETSCII if necessary.
        /// </summary>
        /// <param name="inChar">The character to translate.</param>
        /// <returns>The ASCII representation of the character.</returns>
        /// <remarks>
        /// PETSCII translation is only enabled if the 'A' in the AT command is in PETSCII.
        /// </remarks>
        /*************************************************************************************************************/
        int TranslateFromPetscii(int inChar)
        {
            if (state == StateEnum.AwaitingA)
            {
                petscii = (inChar == PETSCII_START);
            }

            if (petscii && (inChar >= PETSCII_START) && (inChar <= PETSCII_END))
            {
                return inChar - PETSCII_SHIFT;
            }
            else
            {
                return inChar;
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Translates the given character to PETSCII if necessary.
        /// </summary>
        /// <param name="inChar">The character to translate.</param>
        /// <returns>The ASCII representation of the character.</returns>
        /*************************************************************************************************************/
        int TranslateToPetscii(int inChar)
        {
            if (petscii)
            {
                return inChar + PETSCII_SHIFT;
            }
            else
            {
                return inChar;
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Transmits a string to the DTE, translating to PETSCII if necessary.
        /// </summary>
        /// <param name="str">The string to transmit.</param>
        /*************************************************************************************************************/
        void TxStr(string str)
        {
            foreach(char c in str)
            {
                iDTE.TxByte(TranslateToPetscii(c));
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Transmits a string to the DTE preceding and following with CR+LF.
        /// </summary>
        /// <param name="str">The string to transmit.</param>
        /*************************************************************************************************************/
        void TxLine(string str)
        {
            iDTE.TxByte(sReg[(int)SRegEnum.CR]);
            iDTE.TxByte(sReg[(int)SRegEnum.LF]);
            TxStr(str);
            iDTE.TxByte(sReg[(int)SRegEnum.CR]);
            iDTE.TxByte(sReg[(int)SRegEnum.LF]);
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Sends a command response to the DTE.
        /// </summary>
        /// <param name="rsp">The response to send.</param>
        /*************************************************************************************************************/
        void SendResponse(CmdResponse rsp)
        {
            if ((rsp != CmdRsp.None) && (!hideResponses) && (rsp.Code <= resultCodeLimit))
            {
                if (numericResponses)
                {
                    TxLine(rsp.Code.ToString());
                }
                else
                {
                    TxLine(rsp.ResponseStr);
                }
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Executes the command current stored in cmdStrBuilder.
        /// </summary>
        /*************************************************************************************************************/
        void ExecuteCmd()
        {
            CmdResponse rsp = null;

            // Search our list of commands for one matching the one received.
            foreach(CommandHandler cmd in cmdList)
            {
                if ((rsp = cmd.ExecuteCommand(cmdStrBuilder.ToString())) != null)
                {
                    SendResponse(rsp);
                    break;
                }
            }

            // No command handler for the given command, so alert the user.
            if (rsp == null)
            {
                SendResponse(CmdRsp.Error);
            }

            // The command is now complete.
            petscii = false;

            // If we're not in online mode now, then we're awaiting another AT command.
            if (state != StateEnum.Online)
            {
                state = StateEnum.AwaitingA;
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Process a character in offline mode (command mode).
        /// </summary>
        /// <param name="dataFromDte">Data received from the DTE to be processed as an AT command.</param>
        /*************************************************************************************************************/
        void ProcessByteInCommandMode(int dataFromDte)
        {
            // Read a character from the DTE, and translate it from PETSCII to ASCII if necessary.
            int inChar = TranslateFromPetscii(dataFromDte);

            if (echo)
            {
                iDTE.TxByte(TranslateToPetscii(inChar));
            }

            // Capitalize everything so we can also work with lowercase AT commands.
            inChar = char.ToUpper((char)inChar);

            // Run the AT command state machine.
            switch (state)
            {
                case StateEnum.AwaitingA:
                    if (inChar == 'A')
                    {
                        state = StateEnum.AwaitingT;
                    }
                    break;

                case StateEnum.AwaitingT:
                    switch (inChar)
                    {
                        case 'T':
                            state = StateEnum.AwaitingCommand;
                            cmdStrBuilder = new StringBuilder();
                            break;

                        // A/ immediately executes the previous command, which is still in cmdStrBuilder.
                        case '/':
                            ExecuteCmd();
                            break;

                        default:
                            state = StateEnum.AwaitingA;
                            break;
                    }

                    break;

                case StateEnum.AwaitingCommand:
                    if (inChar == sReg[(int)SRegEnum.CR])
                    {
                        ExecuteCmd();
                    }
                    else if (inChar == sReg[(int)SRegEnum.BS])
                    {
                        if (cmdStrBuilder.Length > 0)
                        {
                            cmdStrBuilder.Remove(cmdStrBuilder.Length - 1, 1);
                        }
                    }
                    else
                    {
                        cmdStrBuilder.Append((char) inChar);
                    }
                    break;
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Returns the current guardband time as a TimeSpan.
        /// </summary>
        /*************************************************************************************************************/
        TimeSpan GuardBand
        {
            get
            {
                return new TimeSpan(0, 0, 0, 0, 20 * sReg[(int)SRegEnum.EscapeGuardTime]);
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Exits online data mode and returns to command mode.
        /// </summary>
        /*************************************************************************************************************/
        void ExitOnlineDataMode()
        {
            if (state == StateEnum.Online)
            {
                iDiagMsg.WriteLine("Command Mode");
                escapeCharCount = 0;
                state = StateEnum.AwaitingA;
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Called in an arbitrary thread when the guardband timer fires.
        /// </summary>
        /*************************************************************************************************************/
        void OnEscapeSequenceTimeout(object unused)
        {
            lock (stateLock)
            {
                if (escapeCharCount == 3)
                {
                    ExitOnlineDataMode();
                }
                else
                {
                    // We did not receive all the escape characters in time, so the escape sequence has ended.
                    TerminateEscapeSequence();
                }
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Terminates the current escape sequence attempt, stopping the timer, and sending any escape characters.
        /// </summary>
        /*************************************************************************************************************/
        void TerminateEscapeSequence()
        {
            lock (stateLock)
            {
                // Stop the guardband timer.
                EscapeSequenceTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

                // Send any escape codes that we have buffered.
                while (escapeCharCount > 0)
                {
                    TxByteToRemoteHost(sReg[(int)SRegEnum.EscapeCode]);

                    // Echo the data back to the DTE in half-duplex mode.
                    if (halfDuplex)
                    {
                        iDTE.TxByte(sReg[(int)SRegEnum.EscapeCode]);
                    }

                    escapeCharCount--;
                }
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Process a character in online data mode.
        /// </summary>
        /// <param name="dataFromDte">The data received from the DTE to be sent to the remote host.</param>
        /*************************************************************************************************************/
        void ProcessByteInOnlineDataMode(int dataFromDte)
        {
            lock (stateLock)
            {
                if (dataFromDte == sReg[(int)SRegEnum.EscapeCode])
                {
                    escapeCharCount++;

                    switch (escapeCharCount)
                    {
                        case 1:
                            if (escapeSw.Elapsed >= GuardBand)
                            {
                                // We must receive the entire escape sequence within the guardband time.
                                EscapeSequenceTimer.Change(GuardBand, Timeout.InfiniteTimeSpan);
                            }
                            else
                            {
                                // We received the escape character, but it was too close in time to another character.
                                TerminateEscapeSequence();
                            }
                            break;

                        case 2:
                            break;

                        case 3:
                            // There can be no more data following the last escape char within the guardband.
                            EscapeSequenceTimer.Change(GuardBand, Timeout.InfiniteTimeSpan);
                            break;

                        default:
                            // We received too many escape sequence characters.
                            TerminateEscapeSequence();
                            break;
                    }
                }
                else
                {
                    TerminateEscapeSequence();
                    TxByteToRemoteHost(dataFromDte);

                    // Echo the data back to the DTE in half-duplex mode.
                    if (halfDuplex)
                    {
                        iDTE.TxByte(dataFromDte);
                    }
                }

                escapeSw.Restart();
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Runs the modem simulation.
        /// </summary>
        /// <remarks>
        /// Should be called in a loop. The simulation returns when the ZAP command is executed, in which case you
        /// should create a new instance of the modem simulation and begin running that one.
        /// </remarks>
        /*************************************************************************************************************/
        public void RunSimulation()
        {
            // Initialize the state of the GPIOs.
            iDTE.SetDCD(false);
            iDTE.SetRING(false);

            // Process data until the ZAP command is executed, which resets everything to the default.
            while (!zap)
            {
                int dataFromDte = iDTE.RxByte();

                if (state == StateEnum.Online)
                {
                    ProcessByteInOnlineDataMode(dataFromDte);
                }
                else
                {
                    ProcessByteInCommandMode(dataFromDte);
                }
            }
        }
    }
}