#This repository is now obsolete!

Switch to ACLs instead of using this package.

More information: http://blogs.msdn.com/b/walterm/archive/2014/04/22/windows-azure-paas-acls-are-here.aspx


#Windows Azure Cloud Services - IP Address Restrictions

There are times that you might need restrict access to one or more endpoints of a Web/Worker Role. The **WindowsAzure.IPAddressRestriction** library allows you to do just that based on an IP address, IP address range or even a  hostname. It does this by making changes to the Windows Firewall on each instance.

Each time an instance is provisioned or after a reboot the Fabric Controller will configure firewall rules on each instance. This means, if you configured an input endpoint on port 80, the Fabric Controller configure the firewall on all instances of that role in order to allow traffic to that port. This library allows you to:

 - Disable these rules (based on the port number)
 - Read settings from the ServiceConfiguration.cscfg (allowing you to manage the settings without having to redeploy)
 - Apply these settings to the firewall by creating new rules
 - Undo changes by re-enabling the original rules and deleting the new rules

### Requirements

Since we make changes to the Windows Firewall we'll need to run under **elevated** context.

    <?xml version="1.0" encoding="utf-8"?>
    <ServiceDefinition name="Hostnames" xmlns="http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceDefinition" schemaVersion="2012-10.1.8">
      <WebRole name="HostnamesWeb" vmsize="ExtraSmall">
        <Runtime executionContext="elevated" />
        ...
      </WebRole>
    </ServiceDefinition>


Examples
- 

###Configuration

The library supports the following settings in the ServiceConfiguration.cscfg:

      <Setting name="IPAddressRestriction.Enabled" value="true" />
      <Setting name="IPAddressRestriction.Settings" value="80=123.4.5.6" />

 - The ``IPAddressRestriction.Enabled`` setting allows you to enable or disable the library. You typically use this when you want to IP Address restrictions in the staging 
environment but you don't want these in production. 
 - The ``IPAddressRestriction.Settings`` allows you to configure the ports and IP ranges. Here are a few examples:

   - 80=1.1.1.1
   - 80=1.1.1.1;81=2.2.2.2
   - 80=123.45.67.1-123.45.67.254

### Setting up rules based on the ServiceConfiguration.cscfg

The following code shows how you would typically use the library:

- First you'll need to attach to the RoleEnvironment.Changing event.
- Then we setup the ``IPAddressRestrictionManager``
- We cleanup old rules which could still be on the instance by calling ``ResetDisabledRules`` and ``DeleteRules``
- Finally, if the library is enabled we read the settings and call the ApplySettings method to configure the firewall rules.

<pre><code>public class WebRole : RoleEntryPoint
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

		// Reset everything.
		restrictionManager.ResetDisabledRules();
		restrictionManager.DeleteRules();

		// Apply settings.
		if (IPAddressRestrictionConfiguration.IsEnabled())
			restrictionManager.ApplySettings(IPAddressRestrictionConfiguration.GetSettings());
	}

	void OnRoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
	{
		// Force restart of the instance.
		if (e.Changes.Any(o => o is RoleEnvironmentChange))
			e.Cancel = true;
	}
}
</code></pre>
