namespace RetroModemSim
{
    public class ConsoleDiagMsg:IDiagMsg
    {
        public void WriteLine(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}