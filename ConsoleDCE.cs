namespace RetroModemSim
{
    /*************************************************************************************************************/
    /// <summary>
    /// Simple implementation of a DCE hardware interface using the console.
    /// </summary>
    /// <remarks>
    /// This is primarily for debugging and testing so that you do not need to use a real UART.
    /// </remarks>
    /*************************************************************************************************************/
    public class ConsoleDCE: BaseDCE, IDCE
    {
        /*************************************************************************************************************/
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="iDiagMsg">Object to use for diagnostic messages.</param>
        /*************************************************************************************************************/
        public ConsoleDCE(IDiagMsg iDiagMsg): base(iDiagMsg)
        {
        }

        /*************************************************************************************************************/
        /// <summary>
        /// The current baud rate.
        /// </summary>
        /*************************************************************************************************************/
        public int Baud { get; set; }

        /*************************************************************************************************************/
        /// <summary>
        /// Transmit the given byte from the DCE (modem) to the DTE (computer).
        /// </summary>
        /// <param name="b">The byte to transmit.</param>
        /*************************************************************************************************************/
        public void TxByte(int b)
        {
            Console.Write((char)b);
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Receive a byte from the DTE (computer), blocking until one is read.
        /// </summary>
        /*************************************************************************************************************/
        public int RxByte()
        {
            return Console.ReadKey(true).KeyChar;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Gets/sets Data Set Ready (DSR) on the DCE (modem).
        /// </summary>
        /// <remarks>
        /// DSR indicates that the modem is ready and powered on. It typically stays on when the modem is on.
        /// </remarks>
        /*************************************************************************************************************/
        override public bool DSR
        {
            set
            {
                base.DSR = value;
                ProcessOutput("DSR", value, DSRCfg);
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Gets/sets Data Carrier Detect (DCD) on the DCE (modem).
        /// </summary>
        /// <remarks>
        /// DCD is an output on the modem, and it indicates when a connection is established to the remote
        /// host (the carrier is detected).
        /// </remarks>
        /*************************************************************************************************************/
        override public bool DCD
        {
            set
            {
                base.DCD = value;
                ProcessOutput("DCD", value, DCDCfg);
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Gets/sets Ring (RING) on the DCE (modem).
        /// </summary>
        /// <remarks>
        /// The ring indicator pulses while there is an incoming call.
        /// </remarks>
        /*************************************************************************************************************/
        override public bool RING
        {
            set
            {
                base.RING = value;
                ProcessOutput("RING", value, RINGCfg);
            }
        }
    }
}