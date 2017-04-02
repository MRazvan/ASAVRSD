using System;

namespace SoftwareDebuggerExtension
{
    internal static class GuidList
    {
        public const string guidSoftwareDebuggerPkgString = "258c68ca-99da-4d20-ad47-55601b22a5d3";
        public const string guidSoftwareDebuggerCmdSetString = "7ac80e00-2e20-40be-b83d-f11bf8cc7ee8";
        public const string guidSoftwareDebuggerCmdSetToolbarString = "5D16BDA5-6B8A-45D9-B5D5-161F93CD12F8";

        public static readonly Guid guidSoftwareDebuggerCmdSet = new Guid(guidSoftwareDebuggerCmdSetString);
        public static readonly Guid guidSoftwareDebuggerCmdToolbarSet = new Guid(guidSoftwareDebuggerCmdSetToolbarString);
        public static readonly Guid guidStep = new Guid("{50ABD393-FE5E-4238-990A-FAF4A1BD0C63}");
    }
}