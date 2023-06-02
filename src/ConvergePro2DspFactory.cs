using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace ConvergePro2DspPlugin
{
	public class ConvergePro2DspFactory : EssentialsPluginDeviceFactory<ConvergePro2Dsp>
	{
		public ConvergePro2DspFactory()
		{
			// Set the minimum Essentials Framework Version
			MinimumEssentialsFrameworkVersion = "1.13.4";

			// In the constructor we initialize the list with the typenames that will build an instance of this device
			TypeNames = new List<string>() { "convergepro2dsp" };
		}

		// Builds and returns an instance of EssentialsPluginDeviceTemplate
		public override EssentialsDevice BuildDevice(DeviceConfig dc)
		{
			Debug.Console(1, "Factory Attempting to create new device from type: {0}", dc.Type);

			var propertiesConfig = dc.Properties.ToObject<ConvergePro2DspConfig>();

			if (propertiesConfig == null)
			{
				Debug.Console(2, "[{0}] Factory failed to read properties config for {1}", dc.Key, dc.Name);
				return null;
			}
			
			var comms = CommFactory.CreateCommForDevice(dc);
			if (comms != null) return new ConvergePro2Dsp(dc.Key, dc.Name, comms, propertiesConfig);

			Debug.Console(2, "[{0}] Factory failed to create comms for {1}", dc.Key, dc.Name);
			return null;
		}
	}
}