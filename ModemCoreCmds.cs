using System.Text.RegularExpressions;

namespace RetroModemSim
{
    /*************************************************************************************************************/
    /// <summary>
    /// Commands for the modem core.
    /// </summary>
    /// <remarks>The state stock is already acquired when calling any command.</remarks>
    /*************************************************************************************************************/
    public abstract partial class ModemCore
    {
        /*************************************************************************************************************/
        /// <summary>
        /// Parses a DCE output configuration AT command.
        /// </summary>
        /*************************************************************************************************************/
        IDCE.DCEOutputCfg ParseOutputCfg(string cmdStr, Match match)
        {
            return new IDCE.DCEOutputCfg()
            {
                Invert = !string.IsNullOrEmpty(match.Groups["inv"].Value),
                Output = IDCE.DTEOutputs.Parse<IDCE.DTEOutputs>(match.Groups["sig"].Value),
            };
        }

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
        /// Installs all the commands supported in this file.
        /// </summary>
        /*************************************************************************************************************/
        void InstallCoreCommands()
        {
            // Install our Hayes-compatible AT command handlers.
            cmdList.Add(new CommandHandler("^A$",                                       CmdAnswer));
            cmdList.Add(new CommandHandler("^T$",                                       CmdToneDialing));
            cmdList.Add(new CommandHandler("^P$",                                       CmdPulseDialing));
            cmdList.Add(new CommandHandler("^Z$",                                       CmdZap));
            cmdList.Add(new CommandHandler("^O$",                                       CmdOnline));
            cmdList.Add(new CommandHandler("^C[01]?$",                                  CmdCarrier));
            cmdList.Add(new CommandHandler("^E[01]?$",                                  CmdEcho));
            cmdList.Add(new CommandHandler("^F[01]?$",                                  CmdDuplex));
            cmdList.Add(new CommandHandler("^H[01]?$",                                  CmdHangup));
            cmdList.Add(new CommandHandler("^Q[01]?$",                                  CmdQuiet));
            cmdList.Add(new CommandHandler("^V[01]?$",                                  CmdVerbal));
            cmdList.Add(new CommandHandler("^M[012]?$",                                 CmdMonitor));
            cmdList.Add(new CommandHandler("^X[012]?$",                                 CmdResultCodeSet));
            cmdList.Add(new CommandHandler("^D.*$",                                     CmdDial));
            cmdList.Add(new CommandHandler("^S(?<reg>\\d+)\\?$",                        CmdSRegQuery));
            cmdList.Add(new CommandHandler("^S(?<reg>\\d+)=(?<val>\\d+)$",              CmdSRegSet));

            // Install our extended AT command handlers.
            cmdList.Add(new CommandHandler("^\\+IPR=(?<baud>\\d+)$",                    CmdSetBaud));
            cmdList.Add(new CommandHandler("^\\+IPR\\?$",                               CmdBaudQuery));

            // Install our custom AT command handlers.
            cmdList.Add(new CommandHandler("^\\$B[01]?$",                               CmdBufferOnline));

            // Flow control commands.
            cmdList.Add(new CommandHandler("^\\$SWFC[01]?$",                            CmdSoftwareFlowControl));
            cmdList.Add(new CommandHandler("^\\$SWFC\\?$",                              CmdSoftwareFlowControlQuery));

            // DSR output configuration commands.
            cmdList.Add(new CommandHandler("^\\$DSR=(?<inv>!?)(?<sig>(?:DTR|RTS|))$",   CmdDSRCfg));
            cmdList.Add(new CommandHandler("^\\$DSR\\?$",                               CmdDSRCfgQuery));

            // DCD output configuration commands.
            cmdList.Add(new CommandHandler("^\\$DCD=(?<inv>!?)(?<sig>(?:DTR|RTS|))$",   CmdDCDCfg));
            cmdList.Add(new CommandHandler("^\\$DCD\\?$",                               CmdDCDCfgQuery));

            // RING output configuration commands.
            cmdList.Add(new CommandHandler("^\\$RING=(?<inv>!?)(?<sig>(?:DTR|RTS|))$",  CmdRINGCfg));
            cmdList.Add(new CommandHandler("^\\$RING\\?$",                              CmdRINGCfgQuery));

            // Phonebook commands.
            cmdList.Add(new CommandHandler("^\\$PB=(?<key>.+),(?<value>.+)$",           CmdPhoneBookAdd));
            cmdList.Add(new CommandHandler("^\\$PB\\?$",                                CmdPhoneBookQuery));
            cmdList.Add(new CommandHandler("^\\$PB=(?<key>.+)$",                        CmdPhoneBookDelete));

            // Install the generic AT command handler last because it will match any command. Do this here instead of
            // in the constructor to allow the user to install custom commands.
            cmdList.Add(new CommandHandler("", CmdAt));
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

            SendIntermediateResponseNoLock(sReg[reg].ToString());
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
        /// ATA, answer incoming call.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdAnswer(string cmdStr, Match match)
        {
            iDiagMsg.WriteLine("Answering Incoming Call");
            if (!AnswerIncomingCall())
            {
                return CmdRsp.Error;
            }

            CompleteConnectionNoLock(true);
            TerminateRingNoLock();

            return CmdRsp.Connect;
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
                return CmdRsp.NoCarrier;
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

            // If there is an incoming call, then terminate it.
            if (ringing)
            {
                TerminateIncomingCall();
            }

            // If we're already connected, hang up first.
            if (connected)
            {
                HangupNoLock();
            }

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

            // If the destination starts with an '@', the '@' is ignored. This is useful when connecting to hosts that
            // begin with a T or a P, as the T will be interpreted as the touch-tone or pulse indicator.
            if (cmdStr.StartsWith('@'))
            {
                cmdStr = cmdStr.Substring(1);
            }

            // See if the entry is in our phonebook.
            string phoneBookValue = phoneBook.GetEntry(cmdStr);
            if (phoneBookValue != null)
            {
                cmdStr = phoneBookValue;
            }

            // Use our modem instance to dial the remote destination.
            iDiagMsg.WriteLine($"Dialing \"{cmdStr}\"...");
            CmdResponse cmdRsp = Dial(cmdStr);

            // See if the modem was able to connect to the destination.
            if (cmdRsp == CmdRsp.Connect)
            {
                iDiagMsg.WriteLine($"Connected to \"{cmdStr}\"");
                CompleteConnectionNoLock(enterOnlineMode);
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
        /// The response is sent at the original baud rate.
        /// </remarks>
        /*************************************************************************************************************/
        CmdResponse CmdSetBaud(string cmdStr, Match match)
        {
            int baud = int.Parse(match.Groups["baud"].Value);
            int currentBaud = iDCE.Baud;

            try
            {
                iDiagMsg.WriteLine($"Setting baud rate to {baud}.");

                // Try to set the new baud rate to see if the baud rate is supported or not.
                iDCE.Baud = baud;

                // Now switch back to the original baud rate and send the OK response at the original rate.
                iDCE.Baud = currentBaud;
                SendFinalResponseNoLock(CmdRsp.Ok);

                // Wait for a while for the response to be sent in full at the original baud rate.
                Thread.Sleep(BaudChangeDelay);

                // Finally, switch to the new baud rate.
                iDCE.Baud = baud;

                // Return a special response that indicates that the modem core will not send any response.
                return CmdRsp.None;
            }
            catch(Exception ex)
            {
                // Restore the baud rate to the original value.
                iDCE.Baud = currentBaud;

                // Indicate that the given baud rate cannot be set.
                iDiagMsg.WriteLine($"Failed to set baud rate to {baud}: {ex.Message}");
                return CmdRsp.Error;
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// AT+IPR?, Query the current baud rate.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdBaudQuery(string cmdStr, Match match)
        {
            SendIntermediateResponseNoLock($"+IPR: {iDCE.Baud}");
            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// AT$PB={key}, delete the entry in the phone book with the specified key.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdPhoneBookDelete(string cmdStr, Match match)
        {
            string key = match.Groups["key"].Value;

            try
            {
                phoneBook.DeleteEntry(key);
            }
            catch(Exception ex)
            {
                iDiagMsg.WriteLine($"Failed to delete entry {key} from the phone book: {ex.Message}");
                return CmdRsp.Error;
            }

            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// AT$PB={key},{value} add the entry to the phone book with the specified key.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdPhoneBookAdd(string cmdStr, Match match)
        {
            string key = match.Groups["key"].Value;
            string value = match.Groups["value"].Value;

            try
            {
                phoneBook.AddEntry(key, value);
            }
            catch(Exception ex)
            {
                iDiagMsg.WriteLine($"Failed to add entry {key} to the phone book: {ex.Message}");
                return CmdRsp.Error;
            }

            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// AT$PB? Query the contents of the phone book.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdPhoneBookQuery(string cmdStr, Match match)
        {
            try
            {
                foreach(KeyValuePair<string, string> kvp in phoneBook.GetContents())
                {
                    SendIntermediateResponseNoLock($"$PB: \"{kvp.Key}\",\"{kvp.Value}\"");
                }
            }
            catch(Exception ex)
            {
                iDiagMsg.WriteLine($"Failed to query phone book contents: {ex.Message}");
                return CmdRsp.Error;
            }

            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// AT$B, Enable/disable buffering of data in online data mode.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdBufferOnline(string cmdStr, Match match)
        {
            if ((cmdStr.Length > 2) && (cmdStr[2] == '0'))
            {
                iDiagMsg.WriteLine("Online Data Mode Buffering Disabled");
                bufferOnline = false;
                onlineDataBuffer.Clear();
            }
            else
            {
                iDiagMsg.WriteLine("Online Data Mode Buffering Enabled");
                bufferOnline = true;
            }

            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// AT$SWFC, Enable/disable XON/XOFF flow control.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdSoftwareFlowControl(string cmdStr, Match match)
        {
            if ((cmdStr.Length > 5) && (cmdStr[5] == '1'))
            {
                iDCE.SoftwareFlowControl = true;
                iDiagMsg.WriteLine("XON/XOFF Flow Control Enabled");
            }
            else
            {
                iDCE.SoftwareFlowControl = false;
                iDiagMsg.WriteLine("XON/XOFF Flow Control Disabled");
            }

            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// AT$SWFC?, Query the value of XON/XOFF flow control.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdSoftwareFlowControlQuery(string cmdStr, Match match)
        {
            SendIntermediateResponseNoLock($"$SWFC: {(iDCE.SoftwareFlowControl ? "1" : "0")}");
            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// AT$DSR, Configuration of the DSR output.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdDSRCfg(string cmdStr, Match match)
        {
            iDiagMsg.WriteLine($"DSR={iDCE.DSRCfg}");
            iDCE.DSRCfg = ParseOutputCfg(cmdStr, match);

            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// AT$DSR?, Query the configuration of the DSR output.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdDSRCfgQuery(string cmdStr, Match match)
        {
            SendIntermediateResponseNoLock($"$DSR: {iDCE.DSRCfg}");
            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// AT$DCD, Configuration of the DCD output.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdDCDCfg(string cmdStr, Match match)
        {
            iDiagMsg.WriteLine($"DCD={iDCE.DCDCfg}");
            iDCE.DCDCfg = ParseOutputCfg(cmdStr, match);

            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// AT$DCD?, Query the configuration of the DCD output.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdDCDCfgQuery(string cmdStr, Match match)
        {
            SendIntermediateResponseNoLock($"$DCD: {iDCE.DCDCfg}");
            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// AT$RING, Configuration of the RING output.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdRINGCfg(string cmdStr, Match match)
        {
            iDiagMsg.WriteLine($"RING={iDCE.RINGCfg}");
            iDCE.RINGCfg = ParseOutputCfg(cmdStr, match);

            return CmdRsp.Ok;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// AT$RING?, Query the configuration of the RING output.
        /// </summary>
        /*************************************************************************************************************/
        CmdResponse CmdRINGCfgQuery(string cmdStr, Match match)
        {
            SendIntermediateResponseNoLock($"$RING: {iDCE.RINGCfg}");
            return CmdRsp.Ok;
        }
    }
}