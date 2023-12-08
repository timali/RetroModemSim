using System.Net;
using System.Net.Sockets;

namespace RetroModemSim
{
    public class TcpModem: ModemCore
    {
        bool acceptedIncomingCall;
        TcpListener tcpListener;
        TcpClient tcpClient, incomingClient;
        NetworkStream nwkStream;

        /// <summary>
        /// Parameters passed to the RX thread.
        /// </summary>
        struct RxThreadParams
        {
            public TcpClient client;
            public bool incomingCall;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="iDTE">The DTE instance to use.</param>
        /// <param name="iDiagMsg">The diagnostics message instance to use.</param>
        /// <param name="listenPort">
        /// The port to listen on for incoming connections, or 0 to disable incoming connectsions.
        /// </param>
        /*************************************************************************************************************/
        public TcpModem(IDTE iDTE, IDiagMsg iDiagMsg, int listenPort) : base(iDTE, iDiagMsg)
        {
            try
            {
                if (listenPort > 0)
                {
                    // Create a TCP listener and begin accepting incoming connections.
                    tcpListener = new TcpListener(IPAddress.Any, listenPort);
                    tcpListener.Start();

                    // Begin asynchronously accepting an incoming connection, but do not wait on it.
                    _ = AcceptListenerAsync();
                }
            }
            catch (Exception ex)
            {
                iDiagMsg.WriteLine("Unable to start the listening socket: " + ex.Message);
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Sends the given client a busy message and then disposes of the TCP stream.
        /// </summary>
        /// <param name="client">The TCP client to terminate.</param>
        /*************************************************************************************************************/
        void TerminateIncomingClient(TcpClient client)
        {
            iDiagMsg.WriteLine("Terminating an incoming connection.");

            using (StreamWriter sw = new StreamWriter(client.GetStream()))
            {
                sw.WriteLine("The remote host is busy with another call.");
            }

            client.Dispose();
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Accepts an incoming socket connection, starts a new RX thread for it, and notifies the modem core.
        /// </summary>
        /*************************************************************************************************************/
        async Task AcceptListenerAsync()
        {
            try
            {
                TcpClient newIncomingClient = await tcpListener.AcceptTcpClientAsync();
                lock (stateLock)
                {
                    // See if we're already in a call or if we already have an incoming call.
                    if (connected || (tcpClient != null) || (incomingClient != null))
                    {
                        TerminateIncomingClient(newIncomingClient);
                        return;
                    }

                    // We now have an incoming connection, so inform the modem core.
                    incomingClient = newIncomingClient;
                    StartRxThread(incomingClient, true);
                    OnIncomingConnectionStateChange(true);
                }
            }
            catch (Exception ex)
            {
                iDiagMsg.WriteLine("Error accepting an incoming connection: " + ex.Message);
            }
            finally
            {
                // Begin accepting another incoming connection.
                _ = AcceptListenerAsync();
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Creates a connection to the remote destination.
        /// </summary>
        /// <param name="destination">A string describing the remote destination.</param>
        /// <returns>The connect command upon success, or any other response upon error.</returns>
        /// <remarks>
        /// If the destination starts with an '@', the '@' is ignored. This is useful when connecting to hosts that
        /// begin with a T or a P, as the T will be interpreted as the touch-tone or pulse indicator.
        /// </remarks>
        /// <remarks>The state stock is already acquired when calling this method.</remarks>
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

            lock (stateLock)
            {
                try
                {
                    // Remove the '@' from the beginning of the string if present.
                    string destStr = splitArr[0];
                    if (destStr.StartsWith('@'))
                    {
                        destStr = destStr.Substring(1);
                    }

                    // Create the client, which automatically connects.
                    tcpClient = new TcpClient(destStr, port);
                    StartRxThread(tcpClient, false);

                    return CmdRsp.Connect;
                }
                catch (Exception ex)
                {
                    iDiagMsg.WriteLine(ex.Message);
                    return CmdRsp.NoCarrier;
                }
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Starts a thread to receive data for the given TCP client.
        /// </summary>
        /// <param name="client">The client to service.</param>
        /// <param name="incomingCall">Whether this RX thread is servicing an incoming call.</param>
        /*************************************************************************************************************/
        void StartRxThread(TcpClient client, bool incomingCall)
        {
            // Create and start a thread to receive data from the remote host.
            Thread rxThread = new Thread(RxThread);

            // Start the RX thread with the required parameters.
            rxThread.Start(new RxThreadParams() { client = client, incomingCall = incomingCall});
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Receives data from the remote host.
        /// </summary>
        /*************************************************************************************************************/
        void RxThread(object clientObj)
        {
            RxThreadParams parms = (RxThreadParams)clientObj;
            int rxByte;

            using (NetworkStream rxStream = parms.client.GetStream())
            {
                while (true)
                {
                    try
                    {
                        // Read a byte from the network stream.
                        rxByte = rxStream.ReadByte();

                        // -1 is returned when the stream is closed.
                        if (rxByte == -1)
                        {
                            throw new Exception("Remote host closed the connection");
                        }

                        // Only pass data to the modem core for outgoing calls or accepted incoming calls.
                        if (!(parms.incomingCall && !acceptedIncomingCall))
                        {
                            OnRxData(rxByte);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (stateLock)
                        {
                            if (!(parms.incomingCall && !acceptedIncomingCall))
                            {
                                // Only inform the modem core we were disconnected if we did not initiate the
                                // disconnect. Look at the HResult instead of checking tcpClient for null because
                                // a nwe client could have connected before the RX thread runs from the last one.
                                if (ex.HResult != -2146232800)
                                {
                                    iDiagMsg.WriteLine(ex.Message);
                                    OnDisconnected();
                                }
                            }
                            else
                            {
                                // Inform the core that the incoming connection is no longer available.
                                OnIncomingConnectionStateChange(false);

                                // There is no longer an incoming connection.
                                incomingClient = null;

                                // This is an incoming call which was not accepted, so we must dispose of it ourselves.
                                parms.client?.Dispose();
                            }

                            // In any case, we have no longer accepted the current incoming call.
                            acceptedIncomingCall = false;
                        }

                        return;
                    }
                }
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Called by the modem core when the user answers the current incoming connection.
        /// </summary>
        /// <returns>
        /// True if the incoming call (if there was one) was answered, or false if not.
        /// </returns>
        /// <remarks>The state stock is already acquired when calling this method.</remarks>
        /*************************************************************************************************************/
        protected override bool AnswerIncomingCall()
        {
            if (incomingClient == null)
            {
                return false;
            }

            // The incoming connection becomes our current connection, and there is no longer an incoming connection.
            tcpClient = incomingClient;
            acceptedIncomingCall = true;
            incomingClient = null;

            return true;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Called by the modem core when there is an incoming call, and then the user dials an outgoing connection.
        /// </summary>
        /// <remarks>The state stock is already acquired when calling this method.</remarks>
        /*************************************************************************************************************/
        protected override void TerminateIncomingCall()
        {
            if (incomingClient != null)
            {
                TerminateIncomingClient(incomingClient);
            }
        }

        /*************************************************************************************************************/
        // Abstract classes to be implemented in derived classes.
        /// <summary>
        /// Sends a byte to the remote host.
        /// </summary>
        /// <param name="b">The byte to send.</param>
        /// <remarks>The state stock is already acquired when calling this method.</remarks>
        /*************************************************************************************************************/
        protected override void TxByteToRemoteHost(int b)
        {
            // Get the network stream from the TCP client if we have not already.
            if (nwkStream == null)
            {
                nwkStream = tcpClient.GetStream();
            }

            if (tcpClient.Connected)
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
        /// <remarks>The state stock is already acquired when calling this method.</remarks>
        /*************************************************************************************************************/
        protected override void HangUpModem()
        {
            iDiagMsg.WriteLine($"Disconnecting from remote host");

            try
            {
                nwkStream?.Dispose();
                tcpClient?.Dispose();
                nwkStream = null;
                tcpClient = null;
            }
            catch (Exception ex)
            {
                iDiagMsg.WriteLine(ex.Message);
            }
        }
    }
}