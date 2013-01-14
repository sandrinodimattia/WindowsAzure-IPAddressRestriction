using System;

namespace WindowsAzure.IPAddressRestriction
{
    internal static class Constants
    {
        public const string TraceSource = "WindowsAzure.IPAddressRestriction";

        /// <summary>
        /// ServiceConfiguration: Enabled key.
        /// </summary>
        public const string EnabledKey = "IPAddressRestriction.Enabled";

        /// <summary>
        /// ServiceConfiguration: Settings key.
        /// </summary>
        public const string SettingsKey = "IPAddressRestriction.Settings";

        /// <summary>
        /// ServiceConfiguration: DNS refresh interval key.
        /// </summary>
        public const string DnsRefreshIntervalKey = "IPAddressRestriction.DnsRefreshInterval";

        /// <summary>
        /// Restriction formatting.
        /// </summary>
        public const string RestrictionNameFormatting = "WindowsAzure.IPAddressRestriction Port {0}: {1}";
    }
}
