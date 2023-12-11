﻿using static RetroModemSim.ModemCore;

namespace RetroModemSim
{
    public static class CmdRsp
    {
        public static CmdResponse Ok { get; } =         new CmdResponse(0,   "OK");
        public static CmdResponse Connect { get; } =    new CmdResponse(1,   "CONNECT");
        public static CmdResponse Ring { get; } =       new CmdResponse(2,   "RING");
        public static CmdResponse NoCarrier { get; } =  new CmdResponse(3,   "NO CARRIER");
        public static CmdResponse Error { get; } =      new CmdResponse(4,   "ERROR");

        /// <summary>
        /// Special command response indicating to not show the response.
        /// </summary>
        /// <remarks>The error code is set such that the response will never be displayed to the user.</remarks>
        public static CmdResponse None { get; } =       new CmdResponse(int.MaxValue, "");
    }
}