using System;

namespace WindowsAzure.IPAddressRules
{
    public class Rule
    {
        public string Action { get; set; }

        public string Port { get; set; }

        public string RemoteAddress { get; set; }

        public string NameSuffix { get; set; }

        public override string ToString()
        {
            if (String.IsNullOrEmpty(NameSuffix))
                return String.Format(Constants.RuleNameFormatting, Action, RemoteAddress, Port);
            else
                return String.Format(Constants.RuleNameFormatting, Action, RemoteAddress, Port) + " (" + NameSuffix + ")";
        }
    }
}
