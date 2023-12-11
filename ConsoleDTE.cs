namespace RetroModemSim
{
    /*************************************************************************************************************/
    /// <summary>
    /// Simple implementation of a DTE using the console.
    /// </summary>
    /// <remarks>
    /// This is primarily for debugging and testing so that you do not need to use a real UART.
    /// </remarks>
    /*************************************************************************************************************/
    public class ConsoleDTE: IDTE
    {
        /// <summary>
        /// Transmit the given byte to the DTE.
        /// </summary>
        /// <param name="b"></param>
        public void TxByte(int b)
        {
            Console.Write((char)b);
        }

        /// <summary>
        /// Receive a byte from the DTE, blocking until one is read.
        /// </summary>
        public int RxByte()
        {
            return Console.ReadKey(true).KeyChar;
        }

        /// <summary>
        /// Set the Data Carrier Detect signal on the DTE.
        /// </summary>
        public bool DCD { set { } }

        /// <summary>
        /// Set the Ring signal on the DTE.
        /// </summary>
        public bool RING { set { } }

        /// <summary>
        /// The current baud rate.
        /// </summary>
        public int Baud { get; set; }

        /// <summary>
        /// Whether XON/XOFF flow control is enabled.
        /// </summary>
        public bool SoftwareFlowControl { get; set; }
    }
}