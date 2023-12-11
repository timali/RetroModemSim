using System.IO.Ports;

namespace RetroModemSim
{
    /*************************************************************************************************************/
    /// <summary>
    /// Implementation of IDTE using a UART to connect to the DTE.
    /// </summary>
    /*************************************************************************************************************/
    public class UartDTE: IDTE
    {
        SerialPort port;
        byte[] data = new byte[1];

        /// <summary>
        /// Constructor.
        /// </summary>
        public UartDTE(string portName, int baudRate)
        {
            port = new SerialPort(portName, baudRate);
            port.Open();
        }

        /// <summary>
        /// Transmit the given byte to the DTE.
        /// </summary>
        /// <param name="b"></param>
        public void TxByte(int b)
        {
            data[0] = (byte)b;
            port.Write(data, 0, 1);
        }

        /// <summary>
        /// Receive a byte from the DTE, blocking until one is read.
        /// </summary>
        public int RxByte()
        {
            return port.ReadByte();
        }

        /// <summary>
        /// Set the Data Carrier Detect signal on the DTE.
        /// </summary>
        public bool DCD
        {
            set
            {
                // Most NULL-modem cables wire DTR to the DCD pin on the other side. The C64/128 terminal programs I
                // use (AA term) seem to have inverted DCD logic, so invert the signal before setting it.
                port.DtrEnable = !value;
            }
        }

        /// <summary>
        /// Set the Ring signal on the DTE.
        /// </summary>
        public bool RING
        {
            set
            {
                // The NULL modem cable I am using connects RTS to the RING signal on the DTE.
                port.RtsEnable = value;
            }
        }

        /// <summary>
        /// The current baud rate.
        /// </summary>
        public int Baud
        {
            get
            {
                return port.BaudRate;
            }

            set
            {
                port.BaudRate = value;
            }
        }

        /// <summary>
        /// Whether XON/XOFF flow control is enabled.
        /// </summary>
        public bool SoftwareFlowControl
        {
            get
            {
                return port.Handshake == Handshake.XOnXOff;
            }

            set
            {
                port.Handshake = Handshake.XOnXOff;
            }
        }
    }
}