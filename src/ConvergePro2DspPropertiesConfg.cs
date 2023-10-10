using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Essentials.Core;


namespace ConvergePro2DspPlugin
{
	/// <summary>
	/// Converge Pro 2 DSP properties configuration
	/// </summary>
	public class ConvergePro2DspConfig
	{
		[JsonProperty("communicationMonitor")]
		public CommunicationMonitorConfig CommunicationMonitor { get; set; }

		[JsonProperty("control")]
		public EssentialsControlPropertiesConfig Control { get; set; }

		[JsonProperty("boxName")]
		public string Boxname { get; set; }

		[JsonProperty("levelControlBlocks")]
		public Dictionary<string, ConvergePro2DspLevelControlBlockConfig> LevelControlBlocks { get; set; }

		[JsonProperty("presets")]
		public Dictionary<string, ConvergePro2DspPresetConfig> Presets { get; set; }

		[JsonProperty("dialers")]
		public Dictionary<string, ConvergePro2DspDialerConfig> Dialers { get; set; }

		public ConvergePro2DspConfig()
		{
			LevelControlBlocks = new Dictionary<string, ConvergePro2DspLevelControlBlockConfig>();
			Presets = new Dictionary<string, ConvergePro2DspPresetConfig>();
			Dialers = new Dictionary<string, ConvergePro2DspDialerConfig>();
		}
	}

	/// <summary>
	/// Converge Pro 2 Presets Configurations
	/// </summary>
	public class ConvergePro2DspPresetConfig
	{
		[JsonProperty("label")]
		public string Label { get; set; }

		[JsonProperty("preset")]
		public string Preset { get; set; }
	}

	/// <summary>
	/// Converge Pro 2 Level Control Block Configuration 
	/// </summary>
	public class ConvergePro2DspLevelControlBlockConfig
	{
		[JsonProperty("label")]
		public string Label { get; set; }

		[JsonProperty("channelName")]
		public string ChannelName { get; set; }

		[JsonProperty("blockName")]
		public string BlockName { get; set; }

		[JsonProperty("levelParameter")]
		public string LevelParameter { get; set; }

		[JsonProperty("muteParamter")]
		public string MuteParameter { get; set; }
		
		[JsonProperty("disabled")]
		public bool Disabled { get; set; }

		[JsonProperty("hasLevel")]
		public bool HasLevel { get; set; }

		[JsonProperty("hasMute")]
		public bool HasMute { get; set; }

		[JsonProperty("isMic")]
		public bool IsMic { get; set; }

		[JsonProperty("useAbsoluteValue")]
		public bool UseAbsoluteValue { get; set; }

		[JsonProperty("unmuteOnVolChange")]
		public bool UnmuteOnVolChange { get; set; }
	}

	/// <summary>
	/// Converge Pro 2 Dialer configuration
	/// </summary>
	public class ConvergePro2DspDialerConfig
	{
		[JsonProperty("label")]
		public string Label { get; set; }

		[JsonProperty("channelName")]
		public string ChannelName { get; set; }

		[JsonProperty("blockName")]
		public string BlockName { get; set; }

		[JsonProperty("levelParameter")]
		public string LevelParameter { get; set; }

		[JsonProperty("muteParamter")]
		public string MuteParameter { get; set; }

		[JsonProperty("ClearOnHangup")]
		public bool ClearOnHangup { get; set; }
	}

	/// <summary>
	/// Converge Pro2 Base Config Properties
	/// </summary>
	public class ConvergePro2BaseConfigProperties
	{
		[JsonProperty("label")]
		public string Label { get; set; }

		[JsonProperty("channelName")]
		public string ChannelName { get; set; }
		
		[JsonProperty("blockName")]
		public string BlockName { get; set; }

		[JsonProperty("levelParameter")]
		public string LevelParameter { get; set; }

		[JsonProperty("muteParamter")]
		public string MuteParameter { get; set; }
	}
}