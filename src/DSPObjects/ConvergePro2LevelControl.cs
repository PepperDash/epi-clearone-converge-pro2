using System;
using System.Globalization;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace ConvergePro2DspPlugin
{
	public class ConvergePro2DspLevelControl : ConvergePro2DspControlPoint, IBasicVolumeWithFeedback, IKeyed
	{
		public bool Enabled { get; set; }
		public bool UseAbsoluteValue { get; set; }
		public bool AutomaticUnmuteOnVolumeUp { get; private set; }
		public bool HasMute { get; private set; }
		public bool HasLevel { get; private set; }

		public BoolFeedback MuteFeedback { get; private set; }
		public IntFeedback VolumeLevelFeedback { get; private set; }

		public EPdtLevelTypes Type;

		private readonly ConvergePro2Dsp _parent;
		private bool _isMuted;
		private ushort _volumeLevel;
		private float _minLevel;
		private float _maxLevel;
		private const float MinimumDb = -65;
		private const float MaximumDb = 20;

		/// <summary>
		/// Tracks mute state and fires feedback update
		/// </summary>
		public bool IsMuted
		{
			get { return _isMuted; }
			set
			{
				_isMuted = value;
				MuteFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Tracks voluem level and fires feedback update
		/// </summary>
		public ushort VolumeLevel
		{
			get { return _volumeLevel; }
			set
			{
				_volumeLevel = value;
				VolumeLevelFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <blockName name="key">instance key</blockName>
		/// <blockName name="config">level control block configuration object</blockName>
		/// <blockName name="parent">dsp parent isntance</blockName>
		public ConvergePro2DspLevelControl(string key, ConvergePro2DspLevelControlBlockConfig config, ConvergePro2Dsp parent)
			: base(config, parent)
		{
			_parent = parent;

			if (config.Disabled)
				return;

			Initialize(key, config);
		}

		/// <summary>
		/// Initializes this attribute based on config values and adds commands to the parent's queue.
		/// </summary>
		/// <blockName name="key">instance key</blockName>
		/// <blockName name="config">level control block configuration object</blockName>
		public void Initialize(string key, ConvergePro2DspLevelControlBlockConfig config)
		{
			Key = string.Format("{0}-{1}", Parent.Key, key);
			Enabled = true;

			DeviceManager.AddDevice(this);
			Type = config.IsMic ? EPdtLevelTypes.Microphone : EPdtLevelTypes.Speaker;

			Debug.Console(2, this, "Adding LevelControl '{0}'", Key);

			MuteFeedback = new BoolFeedback(() => IsMuted);
			VolumeLevelFeedback = new IntFeedback(() => VolumeLevel);

			Label = config.Label;
			HasMute = config.HasMute;
			HasLevel = config.HasLevel;
			UseAbsoluteValue = config.UseAbsoluteValue;
			_minLevel = MinimumDb;
			_maxLevel = MaximumDb;
		}

		/// <summary>
		/// Parses the response from the DSP. Command is "MUTE, GAIN, MINMAX, erc. Values[] is the returned values after the channel and group.
		/// </summary>
		/// <example>
		/// {CMD_TYPE} {ENDPOINT_TYPE (EPT)} {ENDPOINT_NUMBER (EPN)} {BLOCK_NUMBER (BN)} {PARAMETER_NAME (PN)} [{VALUE}]
		/// "EP MIC 103 LEVEL MUTE 0"
		/// "EP PROC 201 LEVEL GAIN -5"
		/// </example>
		/// <blockName name="command"></blockName>
		/// <blockName name="values"></blockName>
		public void ParseResponse(string command, string[] values)
		{
			Debug.Console(1, this, "Parsing response {0} values: '{1}'", command, string.Join(", ", values));
			switch (command)
			{
				case "MUTE":
					{
						_isMuted = values[0] == "1";
						MuteFeedback.FireUpdate();
						return;
					}
				case "GAIN":
					{
						var parsedValue = float.Parse(values[0], CultureInfo.InvariantCulture);

						if (UseAbsoluteValue)
						{
							if (parsedValue >= MaximumDb)
								_volumeLevel = ushort.MaxValue;
							else if (parsedValue <= MinimumDb)
								_volumeLevel = ushort.MinValue;
							else
								_volumeLevel = (ushort)(((parsedValue - MinimumDb) * ushort.MaxValue) / (MaximumDb - MinimumDb));
							Debug.Console(1, this, "Level {0} VolumeLevel: '{1}'", Label, _volumeLevel);
						}
						else if (_maxLevel > _minLevel)
						{
							if (parsedValue >= _maxLevel)
								_volumeLevel = ushort.MaxValue;
							else if (parsedValue <= _minLevel)
								_volumeLevel = ushort.MinValue;
							else
								_volumeLevel = (ushort)(((parsedValue - _minLevel) * ushort.MaxValue) / (_maxLevel - _minLevel));
							Debug.Console(1, this, "Level {0} VolumeLevel: '{1}'", Label, _volumeLevel);
						}
						else
						{
							Debug.Console(1, this, "Min and Max levels not valid for level {0}", Label);
							return;
						}

						VolumeLevelFeedback.FireUpdate();
						return;
					}
				case "MIN_GAIN":
					{
						_minLevel = float.Parse(values[0], CultureInfo.InvariantCulture);
						Debug.Console(1, this, "Level {0} new min: {1}", Label, _minLevel);
						break;
					}
				case "MAX_GAIN":
					{
						_maxLevel = float.Parse(values[1], CultureInfo.InvariantCulture);
						Debug.Console(1, this, "Level {0} new max: {1}", Label, _maxLevel);
						break;
					}
			}
		}

		public void SendText(string parameterName, string value)
		{
			var cmd = string.IsNullOrEmpty(value)
				? string.Format("EP {0} {1} {2}", ChannelName, BlockName, parameterName) // get
				: string.Format("EP {0} {1} {2} {3}", ChannelName, BlockName, parameterName, value); // set

			base.SendText(cmd);
		}

		/// <summary>
		/// Polls the DSP for the min and max levels for this object
		/// </summary>
		public void GetCurrentMinMax()
		{
			GetCurrentMin();
			GetCurrentMax();
		}

		public void GetCurrentMin()
		{
			SendText("MIN_GAIN", "");
		}

		public void GetCurrentMax()
		{
			SendText("MAX_GAIN", "");
		}

		/// <summary>
		/// Polls the DSP for the current gain for this object
		/// </summary>
		public void GetCurrentGain()
		{
			SendText(LevelParameter, "");
		}

		/// <summary>
		/// Polls the DSP for the current mute for this object
		/// </summary>
		public void GetCurrentMute()
		{
			SendText(MuteParameter, "");
		}

		/// <summary>
		/// Turns the mute off
		/// </summary>
		public void MuteOff()
		{
			SendText(MuteParameter, "0");
		}

		/// <summary>
		/// Turns the mute on
		/// </summary>
		public void MuteOn()
		{
			SendText(MuteParameter, "1");
		}

		/// <summary>
		/// Toggles mute status
		/// </summary>
		public void MuteToggle()
		{
			SendText(MuteParameter, "2");
		}

		/// <summary>
		/// Sets the volume to a specified level
		/// </summary>
		/// <blockName name="level"></blockName>
		public void SetVolume(ushort level)
		{
			Debug.Console(1, this, "SetVolume: {0}", level);
			if (AutomaticUnmuteOnVolumeUp && _isMuted)
			{
				MuteOff();
			}
			var tempLevel = UseAbsoluteValue 
				? ScaleFull(level) 
				: Scale(level);
			Debug.Console(1, this, "SetVolume Scaled: {0}", tempLevel);

			SendText(LevelParameter, string.Format("{0}", tempLevel));
		}

		

		/// <summary>
		/// Decrements volume level
		/// </summary>
		/// <blockName name="press"></blockName>
		public void VolumeDown(bool press)
		{
			SendText(LevelParameter, "-2 REL");
		}

		/// <summary>
		/// Increments volume level
		/// </summary>
		/// <blockName name="press"></blockName>
		public void VolumeUp(bool press)
		{
			if (AutomaticUnmuteOnVolumeUp && _isMuted)
			{
				MuteOff();
			}
			SendText(LevelParameter, "2 REL");
		}

		/// <summary>
		/// Scales the input provided based on the min/max values from the DSP
		/// </summary>
		/// <blockName name="input"></blockName>
		/// <returns></returns>
		private double Scale(ushort input)
		{
			var scaled = (ushort)(input * (_maxLevel - _minLevel) / ushort.MaxValue) + _minLevel;
			var output = Math.Round(scaled, 2);
			return output;
		}

		/// <summary>
		/// Scales the input provided based on the absolute min/max values
		/// </summary>
		/// <blockName name="input"></blockName>
		/// <returns></returns>
		private double ScaleFull(ushort input)
		{
			var scaled = (ushort)(input * (MaximumDb - MinimumDb) / ushort.MaxValue) + MinimumDb;
			var output = Math.Round(scaled, 2);
			return output;
		}
	}

	/// <summary>
	/// Level type enum
	/// </summary>
	public enum EPdtLevelTypes
	{
		Speaker = 0,
		Microphone = 1
	}
}