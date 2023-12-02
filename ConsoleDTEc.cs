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

        public void SetRts(bool asserted)
        {
            Console.WriteLine($"<RTS {asserted}>");
        }

        public void SetDtr(bool asserted)
        {
            Console.WriteLine($"<DTR {asserted}>");
        }
    }
}