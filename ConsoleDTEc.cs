namespace RetroModemSim
{
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