using System;
using System.Collections.Generic;

namespace WindowsAzure.IPAddressRestriction
{
    public class Restriction
    {
        public string Port { get; set; }

        public string RemoteAddress { get; set; }

        public string NameSuffix { get; set; }

        public override string ToString()
        {
            if (String.IsNullOrEmpty(NameSuffix))
                return String.Format(Constants.RestrictionNameFormatting, Port, RemoteAddress);
            else
                return String.Format(Constants.RestrictionNameFormatting, Port, RemoteAddress) + " (" + NameSuffix + ")";
        }
    }
}
