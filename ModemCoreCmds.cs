using System.Text.RegularExpressions;

namespace RetroModemSim
{
    /*************************************************************************************************************/
    /// <summary>
    /// Commands for the modem core.
    /// </summary>
    /*************************************************************************************************************/
    public abstract partial class ModemCore
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
        protected class CommandHandler
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

        /*************************************************************************************************************/
        /// <summary>
        /// ATX, Result Code Set Selection
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdResultCodeSet(string cmdStr, Match match)
        {
            char s = '2';

            if (cmdStr.Length > 1)
            {
                s = cmdStr[1];
            }

            switch (s)
            {
                case '0':
                    iDiagMsg.WriteLine("Displaying Result Codes 0-4");
                    resultCodeLimit = 4;
                    break;

                case '1':
                    iDiagMsg.WriteLine("Displaying Result Codes 0-5");
                    resultCodeLimit = 5;
                    break;

                case '2':
                    iDiagMsg.WriteLine("Displaying All Result Codes");
                    resultCodeLimit = RESULT_CODE_ALL;
                    break;
            }

            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// ATM, Monitor Speaker
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdMonitor(string cmdStr, Match match)
        {
            if (cmdStr.Length == 1)
            {
                iDiagMsg.WriteLine("Speaker On Command Mode");
            }
            else
            {
                switch (cmdStr[1])
                {
                    case '0':
                        iDiagMsg.WriteLine("Speaker Off");
                        break;

                    case '1':
                        iDiagMsg.WriteLine("Speaker On Command Mode");
                        break;

                    case '2':
                        iDiagMsg.WriteLine("Speaker On Always");
                        break;
                }
            }

            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// ATV, Verbal
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdVerbal(string cmdStr, Match match)
        {
            if ((cmdStr.Length > 1) && (cmdStr[1] == '0'))
            {
                iDiagMsg.WriteLine("Numeric Response Codes");
                numericResponses = true;
            }
            else
            {
                iDiagMsg.WriteLine("Verbal Response Codes");
                numericResponses = false;
            }
            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// ATQ, Quiet
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdQuiet(string cmdStr, Match match)
        {
            if ((cmdStr.Length > 1) && (cmdStr[1] == '1'))
            {
                iDiagMsg.WriteLine("Disabling Response Codes");
                hideResponses = true;
            }
            else
            {
                iDiagMsg.WriteLine("Enabling Response Codes");
                hideResponses = false;
            }
            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// ATF, Full/Half Duplex
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdDuplex(string cmdStr, Match match)
        {
            if ((cmdStr.Length > 1) && (cmdStr[1] == '0'))
            {
                iDiagMsg.WriteLine("Half Duplex");
                halfDuplex = true;
            }
            else
            {
                iDiagMsg.WriteLine("Full Duplex");
                halfDuplex = false;
            }
            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// ATC, Transmitter Carrier.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdCarrier(string cmdStr, Match match)
        {
            if ((cmdStr.Length > 1) && (cmdStr[1] == '0'))
            {
                iDiagMsg.WriteLine("Carrier Off");
            }
            else
            {
                iDiagMsg.WriteLine("Carrier On");
            }
            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// ATH, Hangup.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdHangup(string cmdStr, Match match)
        {
            if ((cmdStr.Length > 1) && (cmdStr[1] == '1'))
            {
                iDiagMsg.WriteLine("Hook Off");
            }
            else
            {
                iDiagMsg.WriteLine("Hook On");
                HangupNoLock();
            }
            return CmdRsp.Ok;
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
            }
            else
            {
                echo = true;
                iDiagMsg.WriteLine("Echo On");
            }
            return CmdRsp.Ok;
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

            TxLineNoLock(sReg[reg].ToString());
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
            HangupNoLock();
            zap = true;

            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// ATO, Online, returns to online data mode from command mode.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdOnline(string cmdStr, Match match)
        {
            if (connected)
            {
                EnterOnlineDataMode();
                return CmdRsp.Connect;
            }
            else
            {
                return CmdRsp.Ok;
            }
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
        /// AT+IPR={baud}, change the baud rate.
        /// </summary>
        /// <remarks>
        /// The baud rate is immediately set, and so the command response will be sent at the new baud rate.
        /// </remarks>
        /*************************************************************************************************************/
        CmdResponse CmdSetBaud(string cmdStr, Match match)
        {
            int baud = int.Parse(match.Groups["baud"].Value);

            try
            {
                iDiagMsg.WriteLine($"Setting baud rate to {baud}.");
                iDTE.SetBaud(baud);
            }
            catch(Exception ex)
            {
                iDiagMsg.WriteLine($"Failed to set baud rate to {baud}: {ex.Message}");
                return CmdRsp.Error;
            }

            return CmdRsp.Ok;
        }
    }
}