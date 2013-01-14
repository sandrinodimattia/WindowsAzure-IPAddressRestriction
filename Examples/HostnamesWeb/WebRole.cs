using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

using WindowsAzure.IPAddressRestriction;

namespace HostnamesWeb
{
    public class WebRole : RoleEntryPoint
    {
        private IPAddressRestrictionManager restrictionManager;
        private List<Restriction> hostnameRestrictions;

        public override bool OnStart()
        {
            if (RoleEnvironment.IsAvailable && !RoleEnvironment.IsEmulated)
            {
                RoleEnvironment.Changing += OnRoleEnvironmentChanging;
                AddAllowedHostnames();
            }

            return base.OnStart();
        }
        
        private void AddAllowedHostnames()
        {
            if (restrictionManager == null)
                restrictionManager = new IPAddressRestrictionManager();

            // First time we run the method.
            if (hostnameRestrictions == null)
            {
                // Create a list which holds the previously created restrictions.
                hostnameRestrictions = new List<Restriction>();

                // Schedule refresh.
                var hostnamesRefreshTimer = new System.Timers.Timer(5 * 60000);
                hostnamesRefreshTimer.Elapsed += (s, e) => { AddAllowedHostnames(); };
                hostnamesRefreshTimer.Start();
            }

            // Loop each configured hostname.
            var hostNames = RoleEnvironment.GetConfigurationSettingValue("HostnamesAllowedToAccessPort80");
            foreach (string hostName in hostNames.Contains(",") ? hostNames.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries) : new string[] { hostNames })
            {
                try
                {
                    System.Diagnostics.Trace.WriteLine("Processing: " + hostName);

                    // Get each IP Address from the hostname.
                    var ipAddresses = System.Net.Dns.GetHostAddresses(hostName);
                    foreach (var ipAddress in ipAddresses)
                    {
                        if (!hostnameRestrictions.Any(o => o.Port == "80" && o.RemoteAddress == ipAddress.ToString()))
                            hostnameRestrictions.Add(new Restriction() { Port = "80", RemoteAddress = ipAddress.ToString(), NameSuffix = hostName });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.ToString());
                }
            }

            // Clean up old rules.
            restrictionManager.DeleteRules();
            restrictionManager.DeleteRules(hostnameRestrictions);

            // Disable all standard rules on port 80.
            restrictionManager.DisableRules("80");

            // Create new rules mathing the hostname.
            restrictionManager.Apply(hostnameRestrictions.ToArray(), false);
        }

        /// <summary>
        /// Force restart of the instance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnRoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            if (e.Changes.Any(o => o is RoleEnvironmentChange))
                e.Cancel = true;
        }
    }
}