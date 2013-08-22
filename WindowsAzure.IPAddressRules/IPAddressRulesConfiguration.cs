using System;
using System.Globalization;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace WindowsAzure.IPAddressRules
{
    public static class IPAddressRulesConfiguration
    {
        /// <summary>
        /// Verify if the IP Address restriction is enabled.
        /// </summary>
        /// <returns></returns>
        public static bool IsEnabled()
        {
            try
            {
                return RoleEnvironment.GetConfigurationSettingValue(Constants.EnabledKey).ToUpper(CultureInfo.InvariantCulture).Equals("TRUE");
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
