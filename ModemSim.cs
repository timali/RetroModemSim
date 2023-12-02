using System.Text.RegularExpressions;
using System.Text;

namespace RetroModemSim
{
    /*************************************************************************************************************/
    /// <summary>
    /// Generic modem simulator.
    /// </summary>
    /*************************************************************************************************************/
    public class ModemSim
    {
        /*************************************************************************************************************/
        /// <summary>
        /// Encapsulates a response to an AT command.
        /// </summary>
        /*************************************************************************************************************/
        public class Response
        {
            public string ResponseStr { get; private set; }

            public int Code { get; private set; }

            public Response(int code, string str)
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

            public Response ExecuteCommand(string cmdStr)
            {
                if (CmdStrRegEx.IsMatch(cmdStr))
                {
                    return CmdDelegate(cmdStr);
                }
                else
                {
                    return null;
                }
            }
        }

        public delegate Response CmdHandlerDelegate(string cmdStr);

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

        Response respNone           = new Response(-1,  "");
        Response respOk             = new Response(0,   "OK");
        Response respConnect        = new Response(1,   "CONNECT");
        Response respRing           = new Response(2,   "RING");
        Response respNoCarrier      = new Response(3,   "NO CARRIER");
        Response respError          = new Response(4,   "ERROR");

        const int PETSCII_START = 0xC1, PETSCII_END = 0xDA, PETSCII_SHIFT = 0x80;
        bool echo, petscii;
        StringBuilder cmdStrBuilder;
        int escapeCharCount;
        bool toneDialing;
        bool zap;
        IDTE iDTE;
        IDiagMsg iDiagMsg;
        StateEnum state;
        List<CommandHandler> cmdList = new List<CommandHandler>();

        /*************************************************************************************************************/
        /// <summary>
        /// ATE, enable/disable echo.
        /// </summary>
        /*************************************************************************************************************/
        Response CmdEcho(string cmdStr)
        {
            if ((cmdStr.Length > 1) && (cmdStr[1] == '0'))
            {
                echo = false;
                iDiagMsg.WriteLine("Echo Off");
                return respOk;
            }
            else
            {
                echo = true;
                iDiagMsg.WriteLine("Echo On");
                return respOk;
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Called when no other command handler matches, and it processes a simple "AT".
        /// </summary>
        /*************************************************************************************************************/
        Response CmdAt(string cmdStr)
        {
            if (cmdStr == "")
            {
                // Handle AT
                return respOk;
            }
            else
            {
                // Anything else is an unrecognized command.
                return respError;
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// ATP, enable pulsedialing.
        /// </summary>
        /*************************************************************************************************************/
        Response CmdPulseDialing(string cmdStr)
        {
            iDiagMsg.WriteLine("Pulse Dialing");
            toneDialing = false;

            return respOk;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// ATT, enable tone dialing.
        /// </summary>
        /*************************************************************************************************************/
        Response CmdToneDialing(string cmdStr)
        {
            iDiagMsg.WriteLine("Tone Dialing");
            toneDialing = true;

            return respOk;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// ATZ, Zap, resets all settings to their defaults and resets the modem.
        /// </summary>
        /*************************************************************************************************************/
        Response CmdZap(string cmdStr)
        {
            iDiagMsg.WriteLine("Reset All Settings to Default");
            zap = true;

            return respOk;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="iDTE"></param>
        /// <param name="iDiagMsg"></param>
        /*************************************************************************************************************/
        public ModemSim(IDTE iDTE, IDiagMsg iDiagMsg)
        {
            this.iDTE = iDTE;
            this.iDiagMsg = iDiagMsg;
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
        /// Sends a command response to the DTE.
        /// </summary>
        /// <param name="rsp">The response to send.</param>
        /*************************************************************************************************************/
        void SendResponse(Response rsp)
        {
            if (rsp != respNone)
            {
                TxStr($"\r\n{rsp.ResponseStr}\r\n");
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Executes the command current stored in cmdStrBuilder.
        /// </summary>
        /*************************************************************************************************************/
        void ExecuteCmd()
        {
            string cmdStr = cmdStrBuilder.ToString();
            Response rsp = null;

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
                SendResponse(respError);
            }

            // The command is now complete.
            state = StateEnum.AwaitingA;
            petscii = false;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Process a character in offline mode (command mode).
        /// </summary>
        /*************************************************************************************************************/
        void ProcessCommandModeCharacter()
        {
            // Read a character from the DTE, and translate it from PETSCII to ASCII if necessary.
            int inChar = TranslateFromPetscii(iDTE.RxByte());

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
                    if (inChar == '\r')
                    {
                        ExecuteCmd();
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
        /// Process a character in online data mode.
        /// </summary>
        /*************************************************************************************************************/
        void ProcessOnlineCharacter()
        {
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
            // Install our command handlers. Do this here instead of the constructor to
            // allow the user to install more specific command handlers, which will go
            // in the list before these generic ones.
            cmdList.Add(new CommandHandler("^E[01]?$", CmdEcho));
            cmdList.Add(new CommandHandler("^T$", CmdToneDialing));
            cmdList.Add(new CommandHandler("^P$", CmdPulseDialing));
            cmdList.Add(new CommandHandler("^Z$", CmdZap));

            // This one must be last because it will match any command.
            cmdList.Add(new CommandHandler("", CmdAt));

            // Process data until the ZAP command is executed, which resets everything to the default.
            while (!zap)
            {
                if (state == StateEnum.Online)
                {
                    ProcessOnlineCharacter();
                }
                else
                {
                    ProcessCommandModeCharacter();
                }
            }
        }
    }
}