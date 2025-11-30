using System.IO.Ports;
using static RetroModemSim.IDCE;

namespace RetroModemSim
{
    /*************************************************************************************************************/
    /// <summary>
    /// Implementation of a DCE (modem) hardware interface using a UART (actually a DTE on a host computer).
    /// </summary>
    /// <remarks>
    /// Abstracts the functions of the DCE (modem), for example transmitting and receiving data.
    /// 
    /// In addition, A DCE has several outputs which are abstracted: DCD, DSR, CTS, and RING (RI).
    /// 
    /// DSR, DCD, and RING are supported, but CTS is currently not (so no hardware flow control support).
    /// 
    /// A UART on a PC only has two outputs that it can control -- DTR and RTS. Yet, we ideally need to emulate
    /// three outputs that a typical DCE would have (DCD, DSR, and RING). The user can control how the two
    /// available outputs are mapped to the three outputs, so the user can choose what is important for them.
    /// </remarks>
    /*************************************************************************************************************/
    public class UartDCE: BaseDCE, IDCE
    {
        SerialPort port;
        byte[] data = new byte[1];

        /*************************************************************************************************************/
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="iDiagMsg">Object to use for diagnostic messages.</param>
        /// <param name="portName">The name of the UART port to use.</param>
        /// <param name="baudRate">The initial UART baud rate.</param>
        /*************************************************************************************************************/
        public UartDCE(IDiagMsg iDiagMsg, string portName, int baudRate): base(iDiagMsg)
        {
            port = new SerialPort(portName, baudRate);
            port.Open();
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Handles output for the given signal, given its configuration, and writes the result to the log.
        /// </summary>
        /// <param name="output">A string description of the output, used only for logging.</param>
        /// <param name="value">The desired logic level of the DCE signal.</param>
        /// <param name="cfg">How the DCE signal is currently configured.</param>
        /// <returns>The actual logic level to set for the DCE signal, given the current confguration.</returns>
        /*************************************************************************************************************/
        new void ProcessOutput(string output, bool value, DCEOutputCfg cfg)
        {
            // Call the base implementation to perform the logging and compute the desired output value for the config.
            bool outVal = base.ProcessOutput(output, value, cfg);

            // Apply the change, depending on how the output is configured.
            switch (cfg.Output)
            {
                case DTEOutputs.DTR: port.DtrEnable = outVal; break;
                case DTEOutputs.RTS: port.RtsEnable = outVal; break;
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// The current baud rate.
        /// </summary>
        /*************************************************************************************************************/
        public int Baud
        {
            get => port.BaudRate;
            set => port.BaudRate = value;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Transmit the given byte from the DCE (modem) to the DTE (computer).
        /// </summary>
        /// <param name="b">The byte to transmit.</param>
        /*************************************************************************************************************/
        public void TxByte(int b)
        {
            data[0] = (byte)b;
            port.Write(data, 0, 1);
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Receive a byte from the DTE (computer), blocking until one is read.
        /// </summary>
        /*************************************************************************************************************/
        public int RxByte()
        {
            return port.ReadByte();
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Gets/sets Data Set Ready (DSR) on the DTE (modem).
        /// </summary>
        /// <remarks>
        /// DSR indicates that the modem is ready and powered on. It typically stays on when the modem is on.
        /// </remarks>
        /*************************************************************************************************************/
        override public bool DSR
        {
            set => ProcessOutput("DSR", base.DSR = value, DSRCfg);
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
            set => ProcessOutput("DCD", base.DCD = value, DCDCfg);
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
            set => ProcessOutput("RING", base.RING = value, RINGCfg);
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Whether XON/XOFF flow control is enabled.
        /// </summary>
        /// <remarks>
        /// This setting is persistent across power cycles.
        /// </remarks>
        /*************************************************************************************************************/
        override public bool SoftwareFlowControl
        {
            set
            {
                // Call the base class to save the new value to both RAM and AppSettings.
                base.SoftwareFlowControl = value;

                // Actually set the software flow control on the UART.
                port.Handshake = Handshake.XOnXOff;
            }
        }
    }
}