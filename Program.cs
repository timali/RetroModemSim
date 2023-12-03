namespace RetroModemSim
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Retro Modem Simulator v1.0, Alicie, 2023.");

            IDTE iDTE;
            if (args.Length == 2)
            {
                try
                {
                    Console.WriteLine($"Opening COM port {args[0]} at {args[1]} baud.");
                    iDTE = new UartDTE(args[0], int.Parse(args[1]));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to open the UART {args[0]}: {ex.Message}");
                    return;
                }
            }
            else
            {
                iDTE = new ConsoleDTE();
            }

            IDiagMsg iMsg = new ConsoleDiagMsg();

            Console.WriteLine("Beginning modem simulation.");
            while (true)
            {
                TcpModem modem = new TcpModem(iDTE, iMsg);
                modem.RunSimulation();
            }
        }
    }
}