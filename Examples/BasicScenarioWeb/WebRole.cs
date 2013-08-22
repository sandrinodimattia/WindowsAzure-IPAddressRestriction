using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

using WindowsAzure.IPAddressRules;

namespace BasicScenarioWeb
{
    public class WebRole : RoleEntryPoint
    {
        private IPAddressRulesManager ruleManager;

        public override bool OnStart()
        {
            if (RoleEnvironment.IsAvailable && !RoleEnvironment.IsEmulated)
            {
                RoleEnvironment.Changing += OnRoleEnvironmentChanging;
                ConfigureIPAddressRestrictions();
            }

            return base.OnStart();
        }

        private void ConfigureIPAddressRestrictions()
        {
            if (ruleManager == null)
                ruleManager = new IPAddressRulesManager();

            // Reset everything.
            ruleManager.ResetDisabledRules();
            ruleManager.DeleteRules();

            // Apply settings.
            if (IPAddressRulesConfiguration.IsEnabled())
                ruleManager.ApplySettings(IPAddressRulesConfiguration.GetSettings());
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