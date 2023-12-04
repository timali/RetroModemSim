namespace RetroModemSim
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Retro Modem Simulator v1.0, Alicie, 2023.");

            IDTE iDTE;
            IDiagMsg iDiagMsg;

            if (args.Length == 2)
            {
                try
                {
                    // Use a UART to talk to the DTE, and output diagnostic messages on the console.
                    Console.WriteLine($"Opening COM port {args[0]} at {args[1]} baud.");
                    iDTE = new UartDTE(args[0], int.Parse(args[1]));
                    iDiagMsg = new ConsoleDiagMsg();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to open the UART {args[0]}: {ex.Message}");
                    return;
                }
            }
            else
            {
                // Use the console for the DTE, and do not display diagnostic messages.
                iDTE = new ConsoleDTE();
                iDiagMsg = new NullDiagMsg();
            }

            Console.WriteLine("Beginning modem simulation.");
            while (true)
            {
                TcpModem modem = new TcpModem(iDTE, iDiagMsg);
                modem.RunSimulation();
            }
        }
    }
}