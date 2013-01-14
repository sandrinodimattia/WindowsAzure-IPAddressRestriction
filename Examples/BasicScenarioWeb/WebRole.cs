using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

using WindowsAzure.IPAddressRestriction;

namespace BasicScenarioWeb
{
    public class WebRole : RoleEntryPoint
    {
        private IPAddressRestrictionManager restrictionManager;

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
            if (restrictionManager == null)
                restrictionManager = new IPAddressRestrictionManager();

            // Reset everything.
            restrictionManager.ResetDisabledRules();
            restrictionManager.DeleteRules();

            // Apply settings.
            if (IPAddressRestrictionConfiguration.IsEnabled())
                restrictionManager.ApplySettings(IPAddressRestrictionConfiguration.GetSettings());
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