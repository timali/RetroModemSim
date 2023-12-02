namespace RetroModemSim
{
    public interface IDTE
    {
        void TxByte(int b);

        int RxByte();

        void SetDCD(bool asserted);

        void SetRING(bool asserted);
    }
}