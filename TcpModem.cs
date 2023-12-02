using System.Net.Sockets;

namespace RetroModemSim
{
    public class TcpModem: ModemCore
    {
        TcpClient tcpClient;
        NetworkStream nwkStream;

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
            string[] splitArr = destination.Split(':');
            if (splitArr.Length != 2 )
            {
                iDiagMsg.WriteLine("Invalid destination");
                return CmdRsp.Error;
            }

            int port;
            try
            {
                port = int.Parse(splitArr[1]);
            }
            catch
            {
                iDiagMsg.WriteLine("Invalid port");
                return CmdRsp.Error;
            }

            try
            {
                // Create the client, which automatically connects.
                tcpClient = new TcpClient(splitArr[0], port);
                nwkStream = tcpClient.GetStream();

                // Create and start a thread to receive data from the remote host.
                Thread rxThread = new Thread(RxThread);
                rxThread.Start();

                return CmdRsp.Connect;
            }
            catch (Exception ex)
            {
                iDiagMsg.WriteLine(ex.Message);
                return CmdRsp.NoCarrier;
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Receives data from the remote host.
        /// </summary>
        /*************************************************************************************************************/
        void RxThread()
        {
            int rxByte;
            while ((nwkStream != null) && tcpClient.Connected)
            {
                try
                {
                    rxByte = nwkStream.ReadByte();

                    if (rxByte == -1)
                    {
                        throw new Exception("Remote host closed the connection.");
                    }

                    OnRxData(rxByte);
                }
                catch(Exception ex)
                {
                    // Do not indicate an error for this because it happens normally when we close the connection.
                    if (ex.HResult != -2146232800)
                    {
                        iDiagMsg.WriteLine(ex.Message);
                    }
                    OnDisconnected();
                }
            }
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
            if ((nwkStream != null) && tcpClient.Connected)
            {
                try
                {
                    nwkStream.WriteByte((byte)b);
                }
                catch(Exception ex)
                {
                    iDiagMsg.WriteLine(ex.Message);
                    OnDisconnected();
                }
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Terminates the remote connection.
        /// </summary>
        /*************************************************************************************************************/
        protected override void HangUp()
        {
            iDiagMsg.WriteLine($"Hangup");

            try
            {
                nwkStream?.Dispose();
                tcpClient?.Dispose();
                nwkStream = null;
                tcpClient = null;
            }
            catch(Exception ex)
            {
                iDiagMsg.WriteLine(ex.Message);
            }
        }
    }
}