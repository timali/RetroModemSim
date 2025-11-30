namespace RetroModemSim
{
    /*************************************************************************************************************/
    /// <summary>
    /// Interface (abstraction) for a DCE (modem) hardware interface.
    /// </summary>
    /// <remarks>
    /// Abstracts the functions of the DCE (modem), for example transmitting and receiving data.
    /// 
    /// In addition, A DCE has several outputs which are abstracted: DCD, DSR, CTS, and RING (RI).
    /// 
    /// DSR, DCD, and RING are supported, but CTS is currently not (so no hardware flow control support).
    /// </remarks>
    /*************************************************************************************************************/
    public interface IDCE
    {
        /// <summary>
        /// The possible DTE outputs that can be configured to act as a DCE output.
        /// </summary>
        public enum DTEOutputs
        {
            /// <summary>
            /// No DTE output is used to act as the DCE output (the DCE signal will not be output).
            /// </summary>
            NONE,

            /// <summary>
            /// The DTE DTR signal is used to act as the DCE signal.
            /// </summary>
            DTR,

            /// <summary>
            /// The DTE RTS signal is used to act as the DCE signal.
            /// </summary>
            RTS,
        }

        /// <summary>
        /// How a particual DCE signal is configured, in terms of how we activated it (if we do).
        /// </summary>
        public struct DCEOutputCfg
        {
            /// <summary>
            /// The DTE signal that we use to active the DCE signal.
            /// </summary>
            public DTEOutputs Output;

            /// <summary>
            /// Whether the DTE signal uses inverted logic, compared to the DCE signal it represents.
            /// </summary>
            public bool Invert;

            /// <summary>
            /// Returns a textual description of this DCE output configuration.
            /// </summary>
            /// <returns></returns>
            public readonly override string ToString()
            {
                return $"{(Invert ? "!" : "")}{Output}";
            }
        }

        /// <summary>
        /// The current baud rate.
        /// </summary>
        int Baud { get; set; }

        /// <summary>
        /// Transmit the given byte from the DCE (modem) to the DTE (computer).
        /// </summary>
        /// <param name="b">The byte to transmit.</param>
        void TxByte(int b);

        /// <summary>
        /// Receive a byte from the DTE (computer), blocking until one is read.
        /// </summary>
        int RxByte();

        /// <summary>
        /// Gets/sets Data Set Ready (DSR) on the DCE (modem).
        /// </summary>
        /// <remarks>
        /// DSR indicates that the modem is ready and powered on. It typically stays on when the modem is on.
        /// </remarks>
        bool DSR { get;  set; }

        /// <summary>
        /// How the DCE's DSR signal is configured.
        /// </summary>
        DCEOutputCfg DSRCfg { get;  set; }

        /// <summary>
        /// Gets/sets Data Carrier Detect (DCD) on the DCE (modem).
        /// </summary>
        /// <remarks>
        /// DCD is an output on the modem, and it indicates when a connection is established to the remote
        /// host (the carrier is detected).
        /// </remarks>
        bool DCD { get;  set; }

        /// <summary>
        /// How the DCE's DCD signal is configured.
        /// </summary>
        DCEOutputCfg DCDCfg { get;  set; }

        /// <summary>
        /// Gets/sets Ring (RING) on the DCE (modem).
        /// </summary>
        /// <remarks>
        /// The ring indicator pulses while there is an incoming call.
        /// </remarks>
        bool RING { get;  set; }

        /// <summary>
        /// How the DCE's RING signal is configured.
        /// </summary>
        DCEOutputCfg RINGCfg { get;  set; }

        /// <summary>
        /// Whether XON/XOFF flow control is enabled.
        /// </summary>
        /// <remarks>
        /// This setting is persistent across power cycles.
        /// </remarks>
        bool SoftwareFlowControl { get; set; }
    }
}