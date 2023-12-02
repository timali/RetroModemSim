namespace RetroModemSim
{
    public class TcpModem: ModemCore
    {
        /*************************************************************************************************************/
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="iDTE">The DTE instance to use.</param>
        /// <param name="iDiagMsg">The diagnostics message instance to use.</param>
        /*************************************************************************************************************/
        public TcpModem(IDTE iDTE, IDiagMsg iDiagMsg) : base(iDTE, iDiagMsg)
        {
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Creates a connection to the remote destination.
        /// </summary>
        /// <param name="destination">A string describing the remote destination.</param>
        /// <returns>The connect command upon success, or any other response upon error.</returns>
        /*************************************************************************************************************/
        protected override CmdResponse Dial(string destination)
        {
            return CmdRsp.Connect;
        }

        /*************************************************************************************************************/
        // Abstract classes to be implemented in derived classes.
        /// <summary>
        /// Sends a byte to the remote host.
        /// </summary>
        /// <param name="b">The byte to send.</param>
        /*************************************************************************************************************/
        protected override void TxByteToRemoteHost(int b)
        {
            iDiagMsg.WriteLine($"TX({(char)b})");
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Terminates the remote connection.
        /// </summary>
        /*************************************************************************************************************/
        protected override void HangUp()
        {
            iDiagMsg.WriteLine($"Hangup");
        }
    }
}