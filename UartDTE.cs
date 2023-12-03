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

        public UartDTE(string portName, int baudRate)
        {
            port = new SerialPort(portName, baudRate);
            port.Open();
        }

        public void TxByte(int b)
        {
            data[0] = (byte)b;
            port.Write(data, 0, 1);
        }

        public int RxByte()
        {
            return port.ReadByte();
        }

        public void SetDCD(bool asserted)
        {
            // Most NULL-modem cables wire DTR to the DCD pin on the other side.
            port.DtrEnable = asserted;
        }

        public void SetRING(bool asserted)
        {
            // The NULL modem cable I am using connects RTS to the RING signal on the DTE.
            port.RtsEnable = asserted;
        }
    }
}