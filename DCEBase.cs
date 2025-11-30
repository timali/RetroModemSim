using static RetroModemSim.IDCE;

namespace RetroModemSim
{
    /*************************************************************************************************************/
    /// <summary>
    /// Base class for all IDEC implementations.
    /// </summary>
    /*************************************************************************************************************/
    public class BaseDCE
    {
        protected IDiagMsg diagMsg;

        DCEOutputCfg dsrCfg, dcdCfg, ringCfg;
        bool swFc;

        /*************************************************************************************************************/
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="iDiagMsg">Diagnostics interface to use.</param>
        /*************************************************************************************************************/
        public BaseDCE(IDiagMsg iDiagMsg)
        {
            diagMsg = iDiagMsg;

            // Load the DCE output configurations.
            dsrCfg  = AppSettings.LoadNoThrow<DCEOutputCfg>("CfgDSR");
            dcdCfg  = AppSettings.LoadNoThrow<DCEOutputCfg>("CfgDCD");
            ringCfg = AppSettings.LoadNoThrow<DCEOutputCfg>("CfgRING");

            // DONT CALL SoftwareFlowControl because the derived class's port is not yet set
            // Load the software flow control setting. Don't actually apply it yet because the derived class is not
            // constructed yet, so we cannot apply the settings at this point.
            swFc = AppSettings.LoadNoThrow<bool>("swFlowControl");
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Determines the output level for a signal given its configuration, and writes the result to the log.
        /// </summary>
        /// <param name="output">A string description of the output, used only for logging.</param>
        /// <param name="value">The desired logic level of the DCE signal.</param>
        /// <param name="cfg">How the DCE signal is currently configured.</param>
        /// <returns>The actual logic level to set for the DCE signal, given the current confguration.</returns>
        /*************************************************************************************************************/
        protected bool ProcessOutput(string output, bool value, DCEOutputCfg cfg)
        {
            bool outVal = cfg.Invert ? !value : value;

            if (cfg.Output == DTEOutputs.NONE)
            {
                diagMsg.WriteLine($"Setting {output} to {value} (but no output is assigned).");
            }
            else
            {
                diagMsg.WriteLine($"Setting {output} to {value} by {(outVal ? "asserting" : "de-asserting")} {cfg.Output}");
            }

            return outVal;
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Whether XON/XOFF flow control is enabled.
        /// </summary>
        /// <remarks>
        /// This setting is persistent across power cycles.
        /// </remarks>
        /*************************************************************************************************************/
        virtual public bool SoftwareFlowControl
        {
            get => swFc;

            set
            {
                // Record and save the new configuration. Derived classes should actually apply the new setting.
                AppSettings.SaveNoThrow("swFlowControl", swFc = value);
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Gets/sets Data Set Ready (DSR) on the DCE (modem).
        /// </summary>
        /// <remarks>
        /// DSR indicates that the modem is ready and powered on. It typically stays on when the modem is on.
        /// </remarks>
        /*************************************************************************************************************/
        virtual public bool DSR { get; set; }

        /*************************************************************************************************************/
        /// <summary>
        /// How the DCE's DSR signal is configured.
        /// </summary>
        /*************************************************************************************************************/
        public DCEOutputCfg DSRCfg
        {
            get => dsrCfg;

            set
            {
                // Record and save the new configuration.
                AppSettings.SaveNoThrow("CfgDSR", dsrCfg = value);

                // Set the output again with the current output's value, applying the new configuration.
                DSR = DSR;
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Gets/sets Data Carrier Detect (DCD) on the DCE (modem).
        /// </summary>
        /// <remarks>
        /// DCD is an output on the modem, and it indicates when a connection is established to the remote
        /// host (the carrier is detected).
        /// </remarks>
        /*************************************************************************************************************/
        virtual public bool DCD { get; set; }

        /*************************************************************************************************************/
        /// <summary>
        /// How the DCE's DCD signal is configured.
        /// </summary>
        /*************************************************************************************************************/
        public DCEOutputCfg DCDCfg
        {
            get => dcdCfg;

            set
            {
                // Record and save the new configuration.
                AppSettings.SaveNoThrow("CfgDCD", dcdCfg = value);

                // Set the output again with the current output's value, applying the new configuration.
                DCD = DCD;
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Gets/sets Ring (RING) on the DCE (modem).
        /// </summary>
        /// <remarks>
        /// The ring indicator pulses while there is an incoming call.
        /// </remarks>
        /*************************************************************************************************************/
        virtual public bool RING { get; set; }

        /*************************************************************************************************************/
        /// <summary>
        /// How the DCE's RING signal is configured.
        /// </summary>
        /*************************************************************************************************************/
        public DCEOutputCfg RINGCfg
        {
            get => ringCfg;

            set
            {
                // Record and save the new configuration.
                AppSettings.SaveNoThrow("CfgRING", ringCfg = value);

                // Set the output again with the current output's value, applying the new configuration.
                RING = RING;
            }
        }
    }
}