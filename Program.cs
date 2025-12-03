using System.Reflection;

namespace RetroModemSim
{
    internal class Program
    {
        static string comPort;
        static int baud = 1200;
        static int incomingPort = 60000;

        /*************************************************************************************************************/
        /// <summary>
        /// Returns a description of the application, along with version information.
        /// </summary>
        /*************************************************************************************************************/
        public static string AppDescription
        {
            get
            {
                return $"RetroModemSim v{Assembly.GetEntryAssembly().GetName().Version}, .NET {Environment.Version}";
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Parses the arguments for the application.
        /// </summary>
        /// <param name="args">The application arguments.</param>
        /// <returns>True if the application should immediately terminate (based on the arguments).</returns>
        /*************************************************************************************************************/
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
                    Console.WriteLine("Usage: RetroModemSim comport=<COM_port> baud=<baud_rate> incomingport=<TCP_port>");
                    Console.WriteLine();
                    Console.WriteLine("Baud rate defaults to 1200 if unspecified.");
                    Console.WriteLine();
                    Console.WriteLine("Incomingport defaults to 60000 if unspecified. If incomingport is 0, incoming calls are disabled.");
                    Console.WriteLine();
                    Console.WriteLine("If comport is unspecified, then the console will be used to interact with the modem.");
                    return true;
                }
            }

            return false;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// The main application entry point.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /*************************************************************************************************************/
        static void Main(string[] args)
        {
            Console.WriteLine($"{AppDescription}, Alicie, 2023-2025.");

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

            // Initialize the AppSettings subsystem, which allows us to save/restore our settings.
            AppSettings.Initialize(".");

            IDCE iDCE;
            IDiagMsg iDiagMsg;

            if (comPort == null)
            {
                // Use the console for the DCE hardware interface, and do not display diagnostic messages.
                iDiagMsg = new NullDiagMsg();
                iDCE = new ConsoleDCE(iDiagMsg);
            }
            else
            {
                try
                {
                    // Use a UART to talk to the DTE, and output diagnostic messages on the console.
                    Console.WriteLine($"Opening COM port {comPort} at {baud} baud.");
                    iDiagMsg = new ConsoleDiagMsg();
                    iDCE = new UartDCE(iDiagMsg, comPort, baud);
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
                TcpModem modem = new TcpModem(iDCE, iDiagMsg, incomingPort);
                modem.RunSimulation();
            }
        }
    }
}