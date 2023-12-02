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
    public class ModemCore
    {
        /*************************************************************************************************************/
        /// <summary>
        /// Encapsulates a response to an AT command.
        /// </summary>
        /*************************************************************************************************************/
        public class CmdResponse
        {
            public string ResponseStr { get; private set; }

            public int Code { get; private set; }

            public CmdResponse(int code, string str)
            {
                Code = code;
                ResponseStr = str;
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Helper class encapsulating the functionality required to process a single command.
        /// </summary>
        /*************************************************************************************************************/
        public class CommandHandler
        {
            public Regex CmdStrRegEx { get; private set; }

            public CmdHandlerDelegate CmdDelegate { get; private set; }

            public CommandHandler(string cmdStrRegEx, CmdHandlerDelegate handler)
            {

                CmdStrRegEx = new Regex(cmdStrRegEx);
                CmdDelegate = handler;
            }

            /*************************************************************************************************************/
            /// <summary>
            /// Executes the command, if it matches.
            /// </summary>
            /// <param name="cmdStr">The command received.</param>
            /// <returns>null if the command does not match, or a command response if it does.</returns>
            /*************************************************************************************************************/
            public CmdResponse ExecuteCommand(string cmdStr)
            {
                Match match = CmdStrRegEx.Match(cmdStr);
                if (match.Success)
                {
                    return CmdDelegate(cmdStr, match);
                }
                else
                {
                    return null;
                }
            }
        }

        public delegate CmdResponse CmdHandlerDelegate(string cmdStr, Match match);

        /// <summary>
        /// The various states the modem can be in.
        /// </summary>
        public enum StateEnum
        {
            AwaitingA,
            AwaitingT,
            AwaitingCommand,
            Online,
        }

        /// <summary>
        /// All the S registers we support.
        /// </summary>
        public enum SRegEnum
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
        bool echo, petscii;
        StringBuilder cmdStrBuilder;
        int escapeCharCount;
        bool zap;
        IDTE iDTE;
        IDiagMsg iDiagMsg;
        IModem iModem;
        StateEnum state;
        Stopwatch escapeSw = new Stopwatch();
        object escapeLock = new object();
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

        List<CommandHandler> cmdList = new List<CommandHandler>();

        /*************************************************************************************************************/
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="iDTE">The DTE instance to use.</param>
        /// <param name="iModem">The modem instance to use.</param>
        /// <param name="iDiagMsg">The diagnostics message instance to use.</param>
        /*************************************************************************************************************/
        public ModemCore(IDTE iDTE, IModem iModem, IDiagMsg iDiagMsg)
        {
            this.iDTE = iDTE;
            this.iModem = iModem;
            this.iDiagMsg = iDiagMsg;

            EscapeSequenceTimer = new Timer(OnEscapeSequenceTimeout);

            // Install our command handlers.
            cmdList.Add(new CommandHandler("^E[01]?$",                              CmdEcho));
            cmdList.Add(new CommandHandler("^T$",                                   CmdToneDialing));
            cmdList.Add(new CommandHandler("^P$",                                   CmdPulseDialing));
            cmdList.Add(new CommandHandler("^Z$",                                   CmdZap));
            cmdList.Add(new CommandHandler("^D.*$",                                 CmdDial));
            cmdList.Add(new CommandHandler("^S(?<reg>\\d+)\\?$",                    CmdSRegQuery));
            cmdList.Add(new CommandHandler("^S(?<reg>\\d+)=(?<val>\\d+)$",          CmdSRegSet));
        }

        /*************************************************************************************************************/
        /// <summary>
        /// ATE, enable/disable echo.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdEcho(string cmdStr, Match match)
        {
            if ((cmdStr.Length > 1) && (cmdStr[1] == '0'))
            {
                echo = false;
                iDiagMsg.WriteLine("Echo Off");
                return CmdRsp.Ok;
            }
            else
            {
                echo = true;
                iDiagMsg.WriteLine("Echo On");
                return CmdRsp.Ok;
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Called when no other command handler matches, and it processes a simple "AT".
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdAt(string cmdStr, Match match)
        {
            if (cmdStr == "")
            {
                // Handle AT
                return CmdRsp.Ok;
            }
            else
            {
                // Anything else is an unrecognized command.
                return CmdRsp.Error;
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// ATP, enable pulsedialing.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdPulseDialing(string cmdStr, Match match)
        {
            iDiagMsg.WriteLine("Pulse Dialing");
            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// ATS{#}?, query s-register values.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdSRegQuery(string cmdStr, Match match)
        {
            int reg = int.Parse(match.Groups["reg"].Value);

            if (reg >= sReg.Length)
            {
                return CmdRsp.Error;
            }

            TxLine(sReg[reg].ToString());
            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// ATS{#}={value}, query s-register values.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdSRegSet(string cmdStr, Match match)
        {
            int reg = int.Parse(match.Groups["reg"].Value);
            int value = int.Parse(match.Groups["val"].Value);

            if (reg >= sReg.Length)
            {
                return CmdRsp.Error;
            }

            sReg[reg] = value;
            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// ATT, enable tone dialing.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdToneDialing(string cmdStr, Match match)
        {
            iDiagMsg.WriteLine("Tone Dialing");
            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// ATZ, Zap, resets all settings to their defaults and resets the modem.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdZap(string cmdStr, Match match)
        {
            iDiagMsg.WriteLine("Reset All Settings to Default");
            iModem.HangUp();
            zap = true;

            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Dials a remote host.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdDial(string cmdStr, Match match)
        {
            bool enterOnlineMode = true;
            int subStrLen = cmdStr.Length - 1;

            // If the last character is a ';', then dial, but remain in command mode.
            if (cmdStr.EndsWith(";"))
            {
                // Remove the semicolon before giving the string to the modem for dialing.
                subStrLen--;
                enterOnlineMode = false;
            }

            // Remove the beginning D, and the ';' if necessary.
            cmdStr = cmdStr.Substring(1, subStrLen);

            // Use our modem instance to dial the remote destination.
            CmdResponse cmdRsp = iModem.Dial(cmdStr);

            // See if the modem was able to connect to the destination.
            if (cmdRsp == CmdRsp.Connect)
            {
                iDiagMsg.WriteLine($"Connected to \"{cmdStr}\"");

                // Inform the DTE that we're now connected (the data carrier is detected).
                iDTE.SetDCD(true);

                // Move into online mode if requested.
                if (enterOnlineMode)
                {
                    iDiagMsg.WriteLine($"Online Data Mode");

                    state = StateEnum.Online;
                    escapeSw.Restart();
                }
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
            if (rsp != CmdRsp.None)
            {
                TxLine(rsp.ResponseStr);
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
                        cmdStrBuilder.Remove(cmdStrBuilder.Length - 1, 1);
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
        /// Called in an arbitrary thread when the guardband timer fires.
        /// </summary>
        /*************************************************************************************************************/
        void OnEscapeSequenceTimeout(object unused)
        {
            lock (escapeLock)
            {
                if (escapeCharCount == 3)
                {
                    iDiagMsg.WriteLine("Command Mode");
                    escapeCharCount = 0;
                    state = StateEnum.AwaitingA;
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
            lock (escapeLock)
            {
                // Stop the guardband timer.
                EscapeSequenceTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

                // Send any escape codes that we have buffered.
                while (escapeCharCount > 0)
                {
                    iModem.TxByteToRemoteHost(sReg[(int)SRegEnum.EscapeCode]);
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
            lock (escapeLock)
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
                    iModem.TxByteToRemoteHost(dataFromDte);
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
            // Install the generic AT command handler last because it will match any command. Do this here instead of
            // in the constructor to allow the user to install custom commands.
            cmdList.Add(new CommandHandler("", CmdAt));

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