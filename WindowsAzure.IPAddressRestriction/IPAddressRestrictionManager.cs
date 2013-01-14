using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WindowsAzure.IPAddressRestriction
{
    public class IPAddressRestrictionManager
    {
        private static readonly TraceSource source = new TraceSource(Constants.TraceSource);

        /// <summary>
        /// COM firewall.
        /// </summary>
        private dynamic _firewall;

        /// <summary>
        /// Keep track of all rules which have been created.
        /// </summary>
        private List<string> _createdRules;

        /// <summary>
        /// Keep track of all rules which have been disabled.
        /// </summary>
        private List<string> _disabledRules;

        /// <summary>
        /// Initialize the manager.
        /// </summary>
        public IPAddressRestrictionManager()
        {
            _createdRules = new List<string>();
            _disabledRules = new List<string>();
        }

        /// <summary>
        /// Apply settings to the firewall and disable all other rules matching the same ports.
        /// </summary>
        /// <param name="settings">80=8.8.8.8,9.9.9.9;81=8.8.8.8</param>
        /// <param name="deleteAllOtherRules">Delete all other rules which have been created before.</param>
        public void ApplySettings(string settings, bool deleteAllOtherRules = true)
        {
            // Trace.
            source.TraceEvent(TraceEventType.Verbose, 0, "Parsing configuration...");

            IList<Restriction> restrictions = new List<Restriction>();

            // Loop each port in the settings.
            foreach (string portDefinition in GetArray(settings, ';'))
            {
                // Parse the port defintion. 0 => Port. 1 => IP or hostname.
                var portAndIp = portDefinition.Split('=');
                if (portAndIp.Length != 2)
                    throw new InvalidOperationException("Invalid format for Restriction: " + portDefinition);

                // Disable matching rules.
                DisableRules(portAndIp[0]);

                // Add restrictions.
                foreach (var ip in GetArray(portAndIp[1], ','))
                    restrictions.Add(new Restriction { Port = portAndIp[0], RemoteAddress = ip });
            }

            // Trace.
            foreach (var restriction in restrictions)
                source.TraceEvent(TraceEventType.Verbose, 0, " > Restriction: {0} - {1}", restriction.RemoteAddress, restriction.Port);

            Apply(restrictions);
        }

        /// <summary>
        /// Convert a string to an array.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        private string[] GetArray(string text, char separator)
        {
            return text.Contains(separator) ? text.Split(new char[] { separator }, StringSplitOptions.RemoveEmptyEntries) : new string[] { text };
        }

        /// <summary>
        /// Apply IP Address restrictions to the firewall and remove all rules which were previously added.
        /// </summary>
        /// <param name="restrictions"></param>
        /// <param name="deleteAllOtherRules">Delete all other rules which have been created before.</param>
        public void Apply(IEnumerable<Restriction> restrictions, bool deleteAllOtherRules = true)
        {
            var rules = GetFirewallRules();
            var rulesToDelete = _createdRules.ToList();

            // Clear created rules.
            _createdRules.Clear();

            // Trace.
            source.TraceEvent(TraceEventType.Verbose, 0, "Applying {0} restrictions", restrictions.Count());

            // Add each restriction.
            foreach (var restriction in restrictions)
            {
                string name = restriction.ToString();

                // Check if the rule exists.
                bool ruleExists = false;
                foreach (var rule in rules)
                {
                    if (rule.Name == name)
                        ruleExists = true;
                }

                // Go ahead and create the rule.
                if (!ruleExists)
                {
                    AddRule(restriction);

                    // Remove the rule from the delete rules list. 
                    if (rulesToDelete.Contains(name))
                        rulesToDelete.Remove(name);
                }
            }

            // Delete all other rules.
            if (deleteAllOtherRules)
                DeleteRules(rulesToDelete);
        }

        /// <summary>
        /// Delete rules which have been created before.
        /// </summary>
        public void DeleteRules()
        {
            var rulesToDelete = new List<string>();
            rulesToDelete.AddRange(_createdRules);

            // Add rules starting with WindowsAzure.IPAddressRestriction
            var rules = GetFirewallRules();
            foreach (var rule in rules)
            {
                var ruleName = rule.Name as string;
                if (ruleName.StartsWith("WindowsAzure.IPAddressRestriction"))
                    rulesToDelete.Add(ruleName);
            }

            // Delete.
            DeleteRules(rulesToDelete);
        }

        /// <summary>
        /// Delete rules.
        /// </summary>
        /// <param name="hostnameRestrictions"></param>
        public void DeleteRules(List<Restriction> hostnameRestrictions)
        {
            DeleteRules(hostnameRestrictions.Select(o => o.ToString()));
        }

        /// <summary>
        /// Delete rules.
        /// </summary>
        /// <param name="ruleNames"></param>
        public void DeleteRules(IEnumerable<string> ruleNames)
        {
            var rules = GetFirewallRules();
            foreach (var rule in rules)
            {
                var ruleName = rule.Name as string;
                if (ruleNames.Contains(ruleName))
                {
                    if (_firewall == null)
                        _firewall = Activator.CreateInstance(Type.GetTypeFromProgID("hnetcfg.fwpolicy2"));
                    _firewall.Rules.Remove(ruleName);

                    // Remove rule.
                    if (_createdRules.Contains(ruleName))
                        _createdRules.Remove(ruleName);

                    // Trace.
                    var localPorts = rule.LocalPorts as string;
                    var remoteAddresses = rule.RemoteAddresses as string;
                    source.TraceEvent(TraceEventType.Verbose, 0, "Removed rule '{0}' - LocalPorts: {1} - RemoteAddresses: {2}", ruleName, localPorts, remoteAddresses);
                }
            }
        }

        /// <summary>
        /// Add a rule to the firewall.
        /// </summary>
        /// <param name="restriction"></param>
        public void AddRule(Restriction restriction)
        {
            var ruleName = restriction.ToString();

            dynamic rule = Activator.CreateInstance(Type.GetTypeFromProgID("hnetcfg.fwrule"));
            rule.Action = 1;
            rule.Direction = 1;
            rule.Enabled = true;
            rule.InterfaceTypes = "All";
            rule.Name = ruleName;
            rule.Protocol = 6;
            rule.RemoteAddresses = restriction.RemoteAddress;
            rule.LocalPorts = restriction.Port;

            if (_firewall == null)
                _firewall = Activator.CreateInstance(Type.GetTypeFromProgID("hnetcfg.fwpolicy2"));
            _firewall.Rules.Add(rule);

            // Log created rule.
            if (!_createdRules.Contains(ruleName))
                _createdRules.Add(ruleName);

            // Trace.
            source.TraceEvent(TraceEventType.Verbose, 0, "Created rule '{0}' - LocalPorts: {1} - RemoteAddresses: {2}", ruleName, restriction.Port, restriction.RemoteAddress);
        }

        /// <summary>
        /// Disabled all rules related to a specific port.
        /// </summary>
        /// <param name="localPort"></param>
        public void DisableRules(string localPort)
        {
            foreach (dynamic rule in GetFirewallRules())
            {
                string ruleName = rule.Name as string;
                if (rule.Enabled == true && rule.LocalPorts == localPort && !ruleName.StartsWith("WindowsAzure.IPAddressRestriction"))
                {
                    rule.Enabled = false;

                    // Add to disabled rules.
                    if (!_disabledRules.Contains(ruleName))
                        _disabledRules.Add(ruleName);

                    // Trace.
                    var localPorts = rule.LocalPorts as string;
                    var remoteAddresses = rule.RemoteAddresses as string;
                    source.TraceEvent(TraceEventType.Verbose, 0, "Disabled rule '{0}' - LocalPorts: {1} - RemoteAddresses: {2}", ruleName, localPorts, remoteAddresses);
                }
            }
        }

        /// <summary>
        /// Reset all disabled rules.
        /// </summary>
        public void ResetDisabledRules()
        {
            if (_disabledRules.Any())
            {
                foreach (dynamic rule in GetFirewallRules())
                {
                    string ruleName = rule.Name as string;
                    if (_disabledRules.Contains(ruleName))
                    {
                        rule.Enabled = true;

                        // Trace.
                        var localPorts = rule.LocalPorts as string;
                        var remoteAddresses = rule.RemoteAddresses as string;
                        source.TraceEvent(TraceEventType.Verbose, 0, "Re-enabled rule '{0}' - LocalPorts: {1} - RemoteAddresses: {2}", ruleName, localPorts, remoteAddresses);
                    }
                }

                _disabledRules.Clear();
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
