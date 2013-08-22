#Windows Azure Cloud Services - IP Address Rules

This is a little modification I did from the great library Sandrino Di Mattia post here at GitHub.

I just add the posibility to manage incoming Firewall rules directly, so you can add a Block or Allow rule. That way scenarios were you want to allow any BUT some addresses can be easely achieved.

So I change a bit the syntax to: [Action] [Port] [IP/Host/IP Range]

Actions cloud be: 
 - BLOCK
 - ALLOW

Port is a TCP Port number.

IP/Host/Range is the specific address, host or address range (x.x.x.x-y.y.y.y). An important thing here is that I added the 0.0.0.0 address as any/asterisk value.
You can add multiple rules separated by semicolon.

An example:
 - ALLOW 80 0.0.0.0;DENY 80 10.10.10.20
This will tell the library to set up a rule to allow all traffic in for por 80 and add also a block rule for the ip 10.10.10.20. This will end up generating that you will block traffic to port 80 just for the IP 10.10.10.20

Just in case you are not so familiar with Windows Firewall, here is how the rules are managed by it (incoming rules!):

- If there is no matching rule it will block the connection (default incoming rule firewall behavior)
- Block rules will be processed first, so they have priority over allow rules.

With that in mind, when you enable the library it will set up the rules you configured in the Azure portal.
If your configuration is, for example, "ALLOW 80 1.2.3.4", the library will first disable rules using the same port (avoiding conflicts),
then will create an allow rule for IP 1.2.3.4 on TCP port 80. The result is simple: only that IP will be able to use that port. This is because any other IP will not match that address and will fall into the default block rule.
So, How you can accomplish the great rule: allow any BUT 1.2.3.4? You will need to create two rules like the above example: "ALLOW 80 0.0.0.0;DENY 80 1.2.3.4". Now every IP will match the allow rule and will have access BUT because the firewall will first process the block rules the IP 1.2.3.4, which match the rule, will be denied.

Hope you like it.

Juan

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

      <Setting name="IPAddressRules.Enabled" value="true" />
      <Setting name="IPAddressRules.Settings" value="ALLOW 80 0.0.0.0;BLOCK 80 2.3.4.5" />

 - The ``IPAddressRules.Enabled`` setting allows you to enable or disable the library. You typically use this when you want to IP Address restrictions in the staging 
environment but you don't want these in production. 
 - The ``IPAddressRules.Settings`` allows you to configure the action, ports and IP ranges. Here are a few examples:

   - ALLOW 80 1.1.1.1
   - ALLOW 80 1.1.1.1;ALLOW 81 2.2.2.2
   - ALLOW 80 123.45.67.1-123.45.67.254

### Setting up rules based on the ServiceConfiguration.cscfg

The following code shows how you would typically use the library:

- First you'll need to attach to the RoleEnvironment.Changing event.
- Then we setup the ``IPAddressRulesManager``
- We cleanup old rules which could still be on the instance by calling ``ResetDisabledRules`` and ``DeleteRules``
- Finally, if the library is enabled we read the settings and call the ApplySettings method to configure the firewall rules.

<pre><code>public class WebRole : RoleEntryPoint
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
</code></pre>
