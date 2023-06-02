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
		/// <param name="key">instance key</param>
		/// <param name="config">level control block configuration object</param>
		/// <param name="parent">dsp parent isntance</param>
		public ConvergePro2DspLevelControl(string key, ConvergePro2DspLevelControlBlockConfig config, ConvergePro2Dsp parent)
			: base(config.Label, config.EndpointType, config.EndpointNumber, config.BlockNumber, parent)
		{
			_parent = parent;

			if (config.Disabled)
				return;

			Initialize(key, config);
		}

		/// <summary>
		/// Initializes this attribute based on config values and adds commands to the parent's queue.
		/// </summary>
		/// <param name="key">instance key</param>
		/// <param name="config">level control block configuration object</param>
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
		/// <param name="command"></param>
		/// <param name="values"></param>
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
				case "MINMAX":
					{
						_minLevel = float.Parse(values[0], CultureInfo.InvariantCulture);
						_maxLevel = float.Parse(values[1], CultureInfo.InvariantCulture);
						Debug.Console(1, this, "Level {0} new min: {1}, new max: {2}", Label, _minLevel, _maxLevel);
						break;
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

		public void SimpleCommand(string cmd, string value)
		{
			SendFullCommand("EP", EndpointType, EndpointNumber, BlockNumber, cmd, value);
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
			SendFullCommand("EP", new[] { EndpointType, EndpointNumber, BlockNumber, "MIN_GAIN" });
		}

		public void GetCurrentMax()
		{
			SendFullCommand("EP", new[] { EndpointType, EndpointNumber, BlockNumber, "MAX_GAIN" });
		}

		/// <summary>
		/// Polls the DSP for the current gain for this object
		/// </summary>
		public void GetCurrentGain()
		{
			SendFullCommand("EP", new[] { EndpointType, EndpointNumber, BlockNumber, "GAIN" });
		}

		/// <summary>
		/// Polls the DSP for the current mute for this object
		/// </summary>
		public void GetCurrentMute()
		{
			SendFullCommand("EP", new[] { EndpointType, EndpointNumber, BlockNumber, "MUTE" });
		}

		/// <summary>
		/// Turns the mute off
		/// </summary>
		public void MuteOff()
		{
			SimpleCommand("MUTE", "0");
		}

		/// <summary>
		/// Turns the mute on
		/// </summary>
		public void MuteOn()
		{
			SimpleCommand("MUTE", "1");
		}

		/// <summary>
		/// Sets the volume to a specified level
		/// </summary>
		/// <param name="level"></param>
		public void SetVolume(ushort level)
		{
			Debug.Console(1, this, "Set Volume: {0}", level);
			if (AutomaticUnmuteOnVolumeUp && _isMuted)
			{
				MuteOff();
			}
			var tempLevel = UseAbsoluteValue ? ScaleFull(level) : Scale(level);
			Debug.Console(1, this, "Set Scaled Volume: {0}", tempLevel);

			SimpleCommand("GAIN", tempLevel.ToString("N2"));
		}

		/// <summary>
		/// Toggles mute status
		/// </summary>
		public void MuteToggle()
		{
			SimpleCommand("MUTE", _isMuted ? "0" : "1");
		}

		/// <summary>
		/// Decrements volume level
		/// </summary>
		/// <param name="press"></param>
		public void VolumeDown(bool press)
		{
			SendFullCommand("RAMP", new[] { EndpointType, EndpointNumber, _minLevel.ToString("N"), "2" });
		}

		/// <summary>
		/// Increments volume level
		/// </summary>
		/// <param name="press"></param>
		public void VolumeUp(bool press)
		{
			if (AutomaticUnmuteOnVolumeUp && _isMuted)
			{
				MuteOff();
			}
			SendFullCommand("RAMP", new[] { EndpointType, EndpointNumber, _maxLevel.ToString("N"), "2" });
		}

		/// <summary>
		/// Scales the input provided based on the min/max values from the DSP
		/// </summary>
		/// <param name="input"></param>
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
		/// <param name="input"></param>
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