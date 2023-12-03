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
        public void TxByte(int b)
        {
            Console.Write((char)b);
        }

        public int RxByte()
        {
            return Console.ReadKey(true).KeyChar;
        }

        public void SetDCD(bool asserted)
        {
            Console.WriteLine($"<DCD {asserted}>");
        }

        public void SetRING(bool asserted)
        {
            Console.WriteLine($"<RING {asserted}>");
        }
    }
}