namespace RetroModemSim
{
    internal class Program
    {
        static string comPort;
        static int baud = 2400;
        static int incomingPort = 60000;

        static bool ParseArgs(string[] args)
        {
            foreach (string arg in args)
            {
                if (arg.StartsWith("comport=", StringComparison.OrdinalIgnoreCase))
                {
                    comPort = arg.Substring(8);
                }
                else if (arg.StartsWith("baud=", StringComparison.OrdinalIgnoreCase))
                {
                    baud = int.Parse(arg.Substring(5));
                }
                else if (arg.StartsWith("incomingport=", StringComparison.OrdinalIgnoreCase))
                {
                    incomingPort = int.Parse(arg.Substring(13));
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("Usage: RetroModemSim comport=<COM port> baud=<baud rate> incomingport=<TCP port>");
                    Console.WriteLine();
                    Console.WriteLine("Baud rate defaults to 2400 if unspecified.");
                    Console.WriteLine();
                    Console.WriteLine("Incomingport defaults to 60000 if unspecified. If incomingport is 0, incoming calls are disabled.");
                    Console.WriteLine();
                    Console.WriteLine("If comport is unspecified, then the console will be used to interact with the modem.");
                    return true;
                }
            }

            return false;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Retro Modem Simulator v1.0, Alicie, 2023.");

            try
            {
                if (ParseArgs(args))
                {
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error processing arguments:" + e.Message);
                return;
            }

            IDTE iDTE;
            IDiagMsg iDiagMsg;

            if (comPort == null)
            {
                // Use the console for the DTE, and do not display diagnostic messages.
                iDTE = new ConsoleDTE();
                iDiagMsg = new NullDiagMsg();
            }
            else
            {
                try
                {
                    // Use a UART to talk to the DTE, and output diagnostic messages on the console.
                    Console.WriteLine($"Opening COM port {comPort} at {baud} baud.");
                    iDTE = new UartDTE(comPort, baud);
                    iDiagMsg = new ConsoleDiagMsg();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to open the UART {comPort}: {ex.Message}");
                    return;
                }
            }

            Console.WriteLine($"Beginning modem simulation with incoming TCP port {incomingPort}.");
            while (true)
            {
                TcpModem modem = new TcpModem(iDTE, iDiagMsg, incomingPort);
                modem.RunSimulation();
            }
        }
    }
}