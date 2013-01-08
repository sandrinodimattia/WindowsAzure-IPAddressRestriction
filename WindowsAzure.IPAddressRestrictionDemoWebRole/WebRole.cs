using System;

using Microsoft.WindowsAzure.ServiceRuntime;

using WindowsAzure.IPAddressRestriction;

namespace WindowsAzure.IPAddressRestrictionDemoWebRole
{
    public class WebRole : RoleEntryPoint
    {
        private IPAddressRestrictionManager restrictionManager;

        public override bool OnStart()
        {
            RoleEnvironment.Changing += OnRoleEnvironmentChanging;
            ConfigureIPAddressRestrictions();
            return base.OnStart();
        }

        private void ConfigureIPAddressRestrictions()
        {
            if (restrictionManager == null)
                restrictionManager = new IPAddressRestrictionManager();

            restrictionManager.RemoveRestrictions();
            if (restrictionManager.IsEnabledInConfiguration())
                restrictionManager.ApplyFromConfiguration();
        }

        /// <summary>
        /// Force restart of the instance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnRoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            e.Cancel = true;
        }
    }
}
