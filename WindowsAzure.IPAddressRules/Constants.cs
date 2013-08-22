namespace WindowsAzure.IPAddressRules
{
    internal static class Constants
    {
        public const string TraceSource = "WindowsAzure.IPAddressRules";

        /// <summary>
        /// ServiceConfiguration: Enabled key.
        /// </summary>
        public const string EnabledKey = "IPAddressRules.Enabled";

        /// <summary>
        /// ServiceConfiguration: Settings key.
        /// </summary>
        public const string SettingsKey = "IPAddressRules.Settings";

        /// <summary>
        /// ServiceConfiguration: DNS refresh interval key.
        /// </summary>
        public const string DnsRefreshIntervalKey = "IPAddressRules.DnsRefreshInterval";

        /// <summary>
        /// Restriction formatting.
        /// </summary>
        public const string RuleNameFormatting = "WindowsAzure.IPAddressRules Action {0} IP/Host {1} on Port {2}";
    }
}
