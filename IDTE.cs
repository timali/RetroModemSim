namespace RetroModemSim
{
    public interface IDTE
    {
        void TxByte(int b);

        int RxByte();

        void SetRts(bool asserted);

        void SetDtr(bool asserted);
    }
}