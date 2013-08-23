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
                return String.Format(Constants.RuleNameFormatting, Action, RemoteAddress == "0.0.0.0" ? "(any)" : RemoteAddress, Port == "0" ? "(any)" : Port);
            else
                return String.Format(Constants.RuleNameFormatting, Action, RemoteAddress == "0.0.0.0" ? "(any)" : RemoteAddress, Port == "0" ? "(any)" : Port) + " (" + NameSuffix + ")";
        }
    }
}
