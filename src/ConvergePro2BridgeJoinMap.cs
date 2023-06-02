using PepperDash.Essentials.Core;

namespace ConvergePro2DspPlugin
{
	/// <summary>
	/// Converge Pro DSP Join Map
	/// </summary>
	public class ConvergePro2DspJoinMap : JoinMapBaseAdvanced
	{
		#region Digital

		[JoinName("IsOnline")]
		public JoinDataComplete IsOnline = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "Is Online",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("PresetRecall")]
		public JoinDataComplete PresetRecall = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 101,
				JoinSpan = 100
			},
			new JoinMetadata()
			{
				Description = "Preset Recall",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("ChannelVisible")]
		public JoinDataComplete ChannelVisible = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 201,
				JoinSpan = 200
			},
			new JoinMetadata()
			{
				Description = "ControlTag Visible",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("ChannelMuteToggle")]
		public JoinDataComplete ChannelMuteToggle = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 401,
				JoinSpan = 200
			},
			new JoinMetadata()
			{
				Description = "ControlTag Mute Toggle",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("ChannelMuteOn")]
		public JoinDataComplete ChannelMuteOn = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 601,
				JoinSpan = 200
			},
			new JoinMetadata()
			{
				Description = "ControlTag Mute On",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("ChannelMuteOff")]
		public JoinDataComplete ChannelMuteOff = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 801,
				JoinSpan = 200
			},
			new JoinMetadata()
			{
				Description = "ControlTag Mute Off",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("ChannelVolumeUp")]
		public JoinDataComplete ChannelVolumeUp = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 1001,
				JoinSpan = 200
			},
			new JoinMetadata()
			{
				Description = "ControlTag Volume Up",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("ChannelVolumeDown")]
		public JoinDataComplete ChannelVolumeDown = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 1201,
				JoinSpan = 200
			},
			new JoinMetadata()
			{
				Description = "ControlTag Volume Down",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		#endregion

		#region Analog

		[JoinName("ChannelVolume")]
		public JoinDataComplete ChannelVolume = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 201,
				JoinSpan = 200
			},
			new JoinMetadata()
			{
				Description = "ControlTag Volume",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Analog
			});

		[JoinName("ChannelType")]
		public JoinDataComplete ChannelType = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 401,
				JoinSpan = 200
			},
			new JoinMetadata()
			{
				Description = "ControlTag Type",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Analog
			});

		#endregion

		#region Serial

		[JoinName("DeviceName")]
		public JoinDataComplete DeviceName = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "Device Name",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});

		[JoinName("PresetName")]
		public JoinDataComplete PresetName = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 101,
				JoinSpan = 100
			},
			new JoinMetadata()
			{
				Description = "Preset Name",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});

		[JoinName("ChannelName")]
		public JoinDataComplete ChannelName = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 201,
				JoinSpan = 200
			},
			new JoinMetadata()
			{
				Description = "Channel Name",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});

		#endregion

		#region Dialer
		[JoinName("IncomingCall")]
		public JoinDataComplete IncomingCall =
			new JoinDataComplete(new JoinData { JoinNumber = 3100, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Call Incoming",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("Answer")]
		public JoinDataComplete Answer =
			new JoinDataComplete(new JoinData { JoinNumber = 3106, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Answer Incoming Call",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("EndCall")]
		public JoinDataComplete EndCall =
			new JoinDataComplete(new JoinData { JoinNumber = 3107, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "End Call",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("KeyPadNumeric")]
		public JoinDataComplete KeypadNumeric =
			new JoinDataComplete(new JoinData { JoinNumber = 3110, JoinSpan = 10 },
			new JoinMetadata
			{
				Description = "Keypad Digits 0-9",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("KeypadStar")]
		public JoinDataComplete KeypadStar =
			new JoinDataComplete(new JoinData { JoinNumber = 3120, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Keypad *",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("KeypadPound")]
		public JoinDataComplete KeypadPound =
			new JoinDataComplete(new JoinData { JoinNumber = 3121, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Keypad #",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("KeypadClear")]
		public JoinDataComplete KeypadClear =
			new JoinDataComplete(new JoinData { JoinNumber = 3122, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Keypad Clear",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("KeypadBackspace")]
		public JoinDataComplete KeypadBackspace =
			new JoinDataComplete(new JoinData { JoinNumber = 3123, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Keypad Backspace",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("KeypadDial")]
		public JoinDataComplete KeypadDial =
			new JoinDataComplete(new JoinData { JoinNumber = 3124, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Keypad Dial and Feedback",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("AutoAnswerOn")]
		public JoinDataComplete AutoAnswerOn =
			new JoinDataComplete(new JoinData { JoinNumber = 3125, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Auto Answer On and Feedback",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("AutoAnswerOff")]
		public JoinDataComplete AutoAnswerOff =
			new JoinDataComplete(new JoinData { JoinNumber = 3126, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Auto Answer Off and Feedback",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("AutoAnswerToggle")]
		public JoinDataComplete AutoAnswerToggle =
			new JoinDataComplete(new JoinData { JoinNumber = 3127, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Auto Answer Toggle and On Feedback",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("OnHook")]
		public JoinDataComplete OnHook =
			new JoinDataComplete(new JoinData { JoinNumber = 3129, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "On Hook Set and Feedback",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("OffHook")]
		public JoinDataComplete OffHook =
			new JoinDataComplete(new JoinData { JoinNumber = 3130, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Off Hook Set and Feedback",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("DoNotDisturbToggle")]
		public JoinDataComplete DoNotDisturbToggle =
			new JoinDataComplete(new JoinData { JoinNumber = 3132, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Do Not Disturb Toggle and Feedback",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("DoNotDisturbOn")]
		public JoinDataComplete DoNotDisturbOn =
			new JoinDataComplete(new JoinData { JoinNumber = 3133, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Do Not Disturb On Set and Feedback",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("DoNotDisturbOff")]
		public JoinDataComplete DoNotDisturbOff =
			new JoinDataComplete(new JoinData { JoinNumber = 3134, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Do Not Disturb Of Set and Feedback",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("CallState")]
		public JoinDataComplete CallState =
			new JoinDataComplete(new JoinData { JoinNumber = 3100, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Call State Feedback",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Analog
			});

		[JoinName("DialString")]
		public JoinDataComplete DialString =
			new JoinDataComplete(new JoinData { JoinNumber = 3100, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Dial String Send and Feedback",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Serial
			});

		[JoinName("Label")]
		public JoinDataComplete Label =
			new JoinDataComplete(new JoinData { JoinNumber = 3101, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Dialer Label",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});

		[JoinName("LastNumberDialerFb")]
		public JoinDataComplete LastNumberDialerFb =
			new JoinDataComplete(new JoinData { JoinNumber = 3102, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Last Number Dialed Feedback",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});

		[JoinName("CallerIdNumberFb")]
		public JoinDataComplete CallerIdNumberFb =
			new JoinDataComplete(new JoinData { JoinNumber = 3104, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Caller ID Number",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});

		[JoinName("CallerIdNameFb")]
		public JoinDataComplete CallerIdNameFb =
			new JoinDataComplete(new JoinData { JoinNumber = 3105, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Caller ID Name",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});

		[JoinName("DisplayNumber")]
		public JoinDataComplete DisplayNumber =
			new JoinDataComplete(new JoinData { JoinNumber = 3106, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "This Line's Number",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});
		#endregion

		public ConvergePro2DspJoinMap(uint joinStart)
			: base(joinStart, typeof(ConvergePro2DspJoinMap))
		{
		}
	}
}