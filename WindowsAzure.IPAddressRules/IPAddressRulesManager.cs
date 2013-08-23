using System;
using System.Globalization;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace WindowsAzure.IPAddressRules
{
    public class IPAddressRulesManager
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
        public IPAddressRulesManager()
        {
            _createdRules = new List<string>();
            _disabledRules = new List<string>();
        }

        /// <summary>
        /// Apply settings to the firewall and disable all other rules matching the same ports.
        /// </summary>
        /// <param name="settings">DENY 80 0.0.0.0; ALLOW 80 8.8.8.8-9.9.9.9; </param>
        /// <param name="deleteAllOtherRules">Delete all other rules which have been created before.</param>
        public void ApplySettings(string settings, bool deleteAllOtherRules = true)
        {
            // Trace.
            source.TraceEvent(TraceEventType.Verbose, 0, "Parsing configuration...");

            IList<Rule> rules = new List<Rule>();

            // Loop each rule definition in the settings.
            foreach (string definition in GetArray(settings, ';'))
            {
                // Parse the defintion. 0 => Action. 1 => Port. 2 => IP or hostname.
                var parsedDefinition = GetArray(definition, ' ');
                if (parsedDefinition.Length != 3)
                    throw new InvalidOperationException("Invalid format for Rule: " + definition);

                // Disable matching rules.
                DisableRules(parsedDefinition[1]);

                // Add rules.
                foreach (var ip in GetArray(parsedDefinition[2], ','))
                    rules.Add(new Rule { Action = parsedDefinition[0], Port = parsedDefinition[1], RemoteAddress = ip });
            }

            // Trace.
            foreach (var rule in rules)
                source.TraceEvent(TraceEventType.Verbose, 0, " > Rule: {0} - {1} - {2}", rule.Action, rule.RemoteAddress, rule.Port);

            Apply(rules);
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
        /// <param name="rules"></param>
        /// <param name="deleteAllOtherRules">Delete all other rules which have been created before.</param>
        public void Apply(IEnumerable<Rule> rules, bool deleteAllOtherRules = true)
        {
            var firewallRules = GetFirewallRules();
            var rulesToDelete = _createdRules.ToList();

            // Clear created rules.
            _createdRules.Clear();

            // Trace.
            source.TraceEvent(TraceEventType.Verbose, 0, "Applying {0} rules", rules.Count());

            // Add each restriction.
            foreach (var rule in rules)
            {
                string name = rule.ToString();

                // Check if the rule exists.
                bool ruleExists = false;
                foreach (var fRule in firewallRules)
                {
                    if (fRule.Name == name)
                    {
                        ruleExists = true;
                        break;
                    }
                }

                // Go ahead and create the rule.
                if (!ruleExists)
                {
                    AddRule(rule);

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

            // Add rules starting with WindowsAzure.IPAddressRule
            var rules = GetFirewallRules();
            foreach (var rule in rules)
            {
                var ruleName = rule.Name as string;
                if (ruleName != null && ruleName.StartsWith(Constants.TraceSource))
                    rulesToDelete.Add(ruleName);
            }

            // Delete.
            DeleteRules(rulesToDelete);
        }

        /// <summary>
        /// Delete rules.
        /// </summary>
        /// <param name="hostnameRules"></param>
        public void DeleteRules(List<Rule> hostnameRules)
        {
            DeleteRules(hostnameRules.Select(o => o.ToString()));
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
                if (ruleName != null && ruleNames.Contains(ruleName))
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
                    var action = rule.Action as int?;
                    source.TraceEvent(TraceEventType.Verbose, 0, "Removed rule '{0}' - Action: {1} - LocalPorts: {2} - RemoteAddresses: {3}", ruleName, GetActionString(action.HasValue ? action.Value : 99), localPorts, remoteAddresses);
                }
            }
        }

        /// <summary>
        /// Add a rule to the firewall.
        /// </summary>
        /// <param name="rule"></param>
        public void AddRule(Rule rule)
        {
            var ruleName = rule.ToString();

            dynamic fRule = Activator.CreateInstance(Type.GetTypeFromProgID("hnetcfg.fwrule"));
            fRule.Action = GetActionNumber(rule.Action);
            fRule.Direction = 1;
            fRule.Enabled = true;
            fRule.InterfaceTypes = "All";
            fRule.Name = ruleName;
            fRule.Protocol = 6;
            fRule.RemoteAddresses = rule.RemoteAddress.Equals("0.0.0.0") ? "*" : rule.RemoteAddress;
            if (!rule.Port.Equals("0"))
                fRule.LocalPorts = rule.Port;

            if (_firewall == null)
                _firewall = Activator.CreateInstance(Type.GetTypeFromProgID("hnetcfg.fwpolicy2"));
            _firewall.Rules.Add(fRule);

            // Log created rule.
            if (!_createdRules.Contains(ruleName))
                _createdRules.Add(ruleName);

            // Trace.
            source.TraceEvent(TraceEventType.Verbose, 0, "Created rule '{0}' - Action: {1} - LocalPorts: {2} - RemoteAddresses: {3}", ruleName, rule.Action, rule.Port, rule.RemoteAddress);
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
                if (ruleName != null && rule.Enabled == true && rule.LocalPorts == localPort && !ruleName.StartsWith(Constants.TraceSource))
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

        private int GetActionNumber(string action)
        {
            switch (action.ToUpper(CultureInfo.InvariantCulture))
            {
                case ActionConstants.ALLOW:
                    return 1;
                case ActionConstants.BLOCK:
                    return 0;
                default:
                    return 1;
            }
        }

        private string GetActionString(int action)
        {
            switch (action)
            {
                case 0:
                    return ActionConstants.BLOCK;
                case 1:
                    return ActionConstants.ALLOW;
                default:
                    return "UNKNOWN";
            }
        }
    }
}
