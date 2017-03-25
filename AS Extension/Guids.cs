// Guids.cs
// MUST match guids.h

using System;

namespace Microsoft.WPFWizardExample
{
    internal static class GuidList
    {
        public const string guidWPFWizardExamplePkgString = "258c68ca-99da-4d20-ad47-55601b22a5d3";
        public const string guidWPFWizardExampleCmdSetString = "7ac80e00-2e20-40be-b83d-f11bf8cc7ee8";

        public static readonly Guid guidWPFWizardExampleCmdSet = new Guid(guidWPFWizardExampleCmdSetString);
        public static readonly Guid guidStep = new Guid("{50ABD393-FE5E-4238-990A-FAF4A1BD0C63}");
    }
}