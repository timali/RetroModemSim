namespace RetroModemSim
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Retro Modem Simulator v1.0, Alicie, 2023.");

            IDTE io = new ConsoleDTE();
            IDiagMsg msg = new ConsoleDiagMsg();

            while (true)
            {
                ModemSim modemSim = new ModemSim(io, msg);
                modemSim.RunSimulation();
            }
        }
    }
}