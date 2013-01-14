using System;

using Microsoft.WindowsAzure.ServiceRuntime;

namespace WindowsAzure.IPAddressRestriction
{
    public static class IPAddressRestrictionConfiguration
    {
        /// <summary>
        /// Verify if the IP Address restriction is enabled.
        /// </summary>
        /// <returns></returns>
        public static bool IsEnabled()
        {
            try
            {
                return RoleEnvironment.GetConfigurationSettingValue(Constants.EnabledKey) == "true";
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Get IP settings.
        /// </summary>
        /// <returns></returns>
        public static string GetSettings()
        {
            return RoleEnvironment.GetConfigurationSettingValue(Constants.SettingsKey);
        }
    }
}
