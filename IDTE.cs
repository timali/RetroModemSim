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
        /// <param name="asserted"></param>
        void SetDCD(bool asserted);

        /// <summary>
        /// Set the Ring signal on the DTE.
        /// </summary>
        /// <param name="asserted"></param>
        void SetRING(bool asserted);

        /// <summary>
        /// Set the baud rate on the DTE.
        /// </summary>
        /// <param name="baud"></param>
        void SetBaud(int baud);
    }
}