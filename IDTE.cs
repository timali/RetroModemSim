namespace RetroModemSim
{
    public interface IDTE
    {
        /// <summary>
        /// Transmit the given byte to the DTE.
        /// </summary>
        /// <param name="b"></param>
        void TxByte(int b);

        /// <summary>
        /// Receive a byte from the DTE, blocking until one is read.
        /// </summary>
        int RxByte();

        /// <summary>
        /// Set the Data Carrier Detect signal on the DTE.
        /// </summary>
        bool DCD { set; }

        /// <summary>
        /// Set the Ring signal on the DTE.
        /// </summary>
        bool RING { set; }

        /// <summary>
        /// The current baud rate.
        /// </summary>
        int Baud { get; set; }

        /// <summary>
        /// Whether XON/XOFF flow control is enabled.
        /// </summary>
        bool SoftwareFlowControl { get; set; }
    }
}