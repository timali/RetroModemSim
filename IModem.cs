namespace RetroModemSim
{
    public interface IModem
    {
        ModemCore.CmdResponse Dial(string destination);

        void HangUp();

        void TxByteToRemoteHost(int b);
    }
}