using static RetroModemSim.ModemCore;

namespace RetroModemSim
{
    public static class CmdRsp
    {
        public static CmdResponse None { get; } =       new CmdResponse(-1,  "");
        public static CmdResponse Ok { get; } =         new CmdResponse(0,   "OK");
        public static CmdResponse Connect { get; } =    new CmdResponse(1,   "CONNECT");
        public static CmdResponse Ring { get; } =       new CmdResponse(2,   "RING");
        public static CmdResponse NoCarrier { get; } =  new CmdResponse(3,   "NO CARRIER");
        public static CmdResponse Error { get; } =      new CmdResponse(4,   "ERROR");
    }
}