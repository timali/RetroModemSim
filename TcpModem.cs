namespace RetroModemSim
{
    public class TcpModem: IModem
    {
        public ModemCore.CmdResponse Dial(string destination)
        {
            return CmdRsp.Connect;
        }

        public void TxByteToRemoteHost(int b)
        {
            Console.WriteLine($"TX({(char)b})");
        }

        public void HangUp()
        {
            Console.WriteLine($"Hangup");
        }
    }
}