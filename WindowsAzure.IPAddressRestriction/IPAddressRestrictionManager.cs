using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.WindowsAzure.ServiceRuntime;

namespace WindowsAzure.IPAddressRestriction
{
    public class IPAddressRestrictionManager
    {
        private const string EnabledKey = "IPAddressRestriction.Enabled";
        private const string SettingsKey = "IPAddressRestriction.Settings";

        private dynamic _firewall;
        private List<string> _rulesChanged;

        public IPAddressRestrictionManager()
        {
            _rulesChanged = new List<string>();
        }

        /// <summary>
        /// Verify if the IP Address restriction is enabled.
        /// </summary>
        /// <returns></returns>
        public bool IsEnabledInConfiguration()
        {
            try
            {
                return RoleEnvironment.GetConfigurationSettingValue(EnabledKey) == "true";
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Read settings from configuration and apply them to firewall.
        /// Format should be as follows: 80=8.8.8.8,9.9.9.9;81=8.8.8.8
        /// </summary>
        public void ApplyFromConfiguration()
        {
            IList<Restriction> restrictions = new List<Restriction>();
            string settings = RoleEnvironment.GetConfigurationSettingValue(SettingsKey);
            string[] portSettings = settings.Contains(";") ? settings.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries) : new string[] { settings };
            foreach (string portDefinition in portSettings)
            {
                var portAndIp = portDefinition.Split('=');
                restrictions.Add(new Restriction { Port = portAndIp[0], IPAddresses = portAndIp[1] });
            }

            Apply(restrictions.ToArray());
        }

        /// <summary>
        /// Apply IP Address restrictions to the firewall.
        /// </summary>
        /// <param name="restrictions"></param>
        public void Apply(params Restriction[] restrictions)
        {
            foreach (dynamic rule in GetFirewallRules())
            {
                if (rule.LocalPorts != null)
                {
                    string ruleName = rule.Name.ToString();
                    string localPorts = rule.LocalPorts.ToString();
                    if (localPorts != "*" && restrictions.Any(o => o.Port == localPorts))
                    {
                        // Apply new IP Address to rule.
                        Restriction restriction = restrictions.FirstOrDefault(o => o.Port == localPorts);
                        rule.RemoteAddresses = restriction.IPAddresses;

                        // Track changes.
                        if (!_rulesChanged.Contains(ruleName))
                            _rulesChanged.Add(ruleName);
                    }
                }
            }
        }

        /// <summary>
        /// Remove all restrictions which were applied.
        /// </summary>
        public void RemoveRestrictions()
        {
            if (_rulesChanged.Any())
            {
                foreach (dynamic rule in GetFirewallRules())
                {
                    if (_rulesChanged.Contains(rule.Name.ToString()))
                        rule.RemoteAddresses = "*";
                }

                _rulesChanged.Clear();
            }
        }

        /// <summary>
        /// Get the current firewall rules.
        /// </summary>
        /// <returns></returns>
        private dynamic GetFirewallRules()
        {
            if (_firewall == null)
                _firewall = Activator.CreateInstance(Type.GetTypeFromProgID("hnetcfg.fwpolicy2"));
            return _firewall.Rules;
        }
    }
}
