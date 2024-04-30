using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Queues;

namespace ConvergePro2DspPlugin
{
	/// <summary>
	/// DSP Device 
	/// </summary>
	/// <remarks>
	/// </remarks>
	public class ConvergePro2Dsp : EssentialsBridgeableDevice
	{
		private readonly IBasicCommunication _comm;
		public CommunicationGather CommGather { get; private set; }
		public readonly GenericCommunicationMonitor CommMonitor;
		private readonly GenericQueue _commRxQueue;

		private const string CommCommandDelimter = "\r\n";
		private const string CommGatherDelimiter = "\r\n";

		public BoolFeedback IsOnlineFeedback { get { return CommMonitor.IsOnlineFeedback; } }
		public IntFeedback CommMonitorFeedback { get; private set; }
		public IntFeedback SocketStatusFeedback { get; private set; }

		private readonly ConvergePro2DspConfig _config;
		private uint _heartbeatTracker;
		private bool _loggedIn;
		private bool _initializeComplete;

		public string BoxName { get; set; }
		public Dictionary<string, ConvergePro2DspLevelControl> LevelControlPoints { get; private set; }
		public Dictionary<string, ConvergePro2DspPresetConfig> Presets { get; set; }
		public Dictionary<string, ConvergePro2Dialer> Dialers { get; set; }

		public bool ShowHexResponse { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="key">String</param>
		/// <param name="name">String</param>
		/// <param name="comm">IBasicCommunication</param>		
		/// <param name="config"></param>
		public ConvergePro2Dsp(string key, string name, IBasicCommunication comm, ConvergePro2DspConfig config)
			: base(key, name)
		{
			try
			{
				Debug.Console(_debugVerbose, this, new string('*', 50));
				Debug.Console(_debugVerbose, this, new string('*', 50));
				Debug.Console(_debugVerbose, this, "Creating ClearOne Converge Pro 2 DSP Instance");

				_config = config;
				BoxName = _config.Boxname;

				_loggedIn = false;

				_comm = comm;
				_comm.TextReceived += OnTextReceived;
				_commRxQueue = new GenericQueue(key + "-queue");
				CommGather = new CommunicationGather(_comm, CommGatherDelimiter);
				CommGather.LineReceived += OnLineRecieved;


				var pollTime = 30000;
				var timeToWarning = 180000;
				var timeToError = 300000;

				if (_config.CommunicationMonitor != null)
				{
					pollTime = _config.CommunicationMonitor.PollInterval;
					timeToWarning = _config.CommunicationMonitor.TimeToWarning;
					timeToError = _config.CommunicationMonitor.TimeToError;
				}

				CommMonitor = new GenericCommunicationMonitor(this, _comm, pollTime, timeToWarning, timeToError, HeartbeatPoll);
				CommMonitor.StatusChange += OnCommMonitorStatusChange;

				var socket = _comm as ISocketStatus;
				if (socket != null)
				{
					socket.ConnectionChange += OnSocketConnectionChange;
					SocketStatusFeedback = new IntFeedback(() => (int)socket.ClientStatus);
				}

				LevelControlPoints = new Dictionary<string, ConvergePro2DspLevelControl>();
				Debug.Console(_debugVerbose, this, "CreateDspObjects: _config.LevelControlPoints.Count-'{0}'",
				_config.LevelControlBlocks.Count);

				LevelControlPoints.Clear();
				if (_config.LevelControlBlocks != null)
				{
					foreach (var levelControlBlock in _config.LevelControlBlocks)
					{
						LevelControlPoints.Add(levelControlBlock.Key, new ConvergePro2DspLevelControl(levelControlBlock.Key, levelControlBlock.Value, this));

						Debug.Console(_debugVerbose, this, "CreateDspObjects: Added LevelControl {0}-'{1}' (ChannelName:'{2}', BlockName:'{3}', MuteParameter:'{4}')",
							levelControlBlock.Key, levelControlBlock.Value.Label, levelControlBlock.Value.ChannelName, levelControlBlock.Value.BlockName, levelControlBlock.Value.MuteParameter);
					}
				}

				Debug.Console(_debugVerbose, this, "CreateDspObjects: LevelControlPoints.Count-'{0}'",
					LevelControlPoints.Count);

				Presets = new Dictionary<string, ConvergePro2DspPresetConfig>();
				Debug.Console(_debugVerbose, this, "CreateDspObjects: _config.Presets.Count-'{0}'",
				_config.Presets.Count);

				Presets.Clear();
				if (_config.Presets != null)
				{
					foreach (var item in _config.Presets)
					{
						var k = item.Key;
						var preset = item.Value;
						Presets.Add(k, preset);

						Debug.Console(_debugVerbose, this, "CreateDspObjects: Added Preset {0}-'{1}' '{2}'",
							k, preset.Label, preset.Preset);
					}
				}

				Debug.Console(_debugVerbose, this, "CreateDspObjects: Presets.Count-'{0}'",
					Presets.Count);

				Dialers = new Dictionary<string, ConvergePro2Dialer>();
				Debug.Console(_debugVerbose, this, "CreateDspObjects: _config.Dialers.Count-'{0}'",
				_config.Dialers.Count);

				Dialers.Clear();
				if (_config.Dialers != null)
				{
					foreach (var dialerConfig in _config.Dialers)
					{
						Dialers.Add(dialerConfig.Key, new ConvergePro2Dialer(dialerConfig.Key, dialerConfig.Value, this));

						Debug.Console(_debugVerbose, this, "CreateDspObjects: Added Dialer {0}-'{1}' (ChannelName:'{2}')",
							dialerConfig.Key, dialerConfig.Value.Label, dialerConfig.Value.ChannelName);
					}
				}

				Debug.Console(_debugVerbose, this, "CreateDspObjects: Dialers.Count-'{0}'",
					Dialers.Count);

				Debug.Console(_debugVerbose, this, new string('*', 50));
				Debug.Console(_debugVerbose, this, new string('*', 50));

				//CreateDspObjects();

				AddPostActivationAction(() =>
				{
					if (Dialers != null)
					{
						foreach (var dialer in Dialers)
						{
							dialer.Value.SubscribeToNotifications();
						}
					}
				});
			}
			catch (Exception ex)
			{
				Debug.Console(_debugNotice, this, "Exception Message: '{0}'", ex.Message);
				Debug.Console(_debugVerbose, this, Debug.ErrorLogLevel.Error, "** StackTrace:\n{0}", ex.StackTrace);
				if (ex.InnerException != null) Debug.Console(_debugNotice, this, Debug.ErrorLogLevel.Error, "** InnerException:\n{0}", ex.InnerException);
			}
		}

		/// <summary>
		/// Initializes plugin
		/// </summary>
		public override void Initialize()
		{
			_comm.Connect();
			CommMonitor.Start();

			base.Initialize();

			_initializeComplete = true;
		}

		#region IBridgeAdvanced Members

		/// <summary>
		/// Link to API
		/// </summary>
		/// <param name="trilist"></param>
		/// <param name="joinStart"></param>
		/// <param name="joinMapKey"></param>
		/// <param name="bridge"></param>
		public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
		{
			var joinMap = new ConvergePro2DspJoinMap(joinStart);

			// This adds the join map to the collection on the bridge
			if (bridge != null)
			{
				bridge.AddJoinMap(Key, joinMap);
			}

			var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);
			if (customJoins != null)
			{
				joinMap.SetCustomJoinData(customJoins);
			}

			Debug.Console(_debugTrace, this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
			Debug.Console(_debugTrace, this, "Linking to Bridge Type {0}", GetType().Name);

			IsOnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);

			if (CommMonitorFeedback != null)
				CommMonitorFeedback.LinkInputSig(trilist.UShortInput[joinMap.CommunicationMonitorStatus.JoinNumber]);

			if (SocketStatusFeedback != null)
				SocketStatusFeedback.LinkInputSig(trilist.UShortInput[joinMap.SocketStatus.JoinNumber]);

			// device name
			trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

			LinkLevelControlToApi(trilist, joinMap);
			LinkPresetsToApi(trilist, joinMap);
			LinkDialersToApi(trilist, joinMap);

			trilist.OnlineStatusChange += (sender, args) =>
			{
				if (!args.DeviceOnLine) return;

				trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
				IsOnlineFeedback.FireUpdate();
				CommMonitorFeedback.FireUpdate();
				SocketStatusFeedback.FireUpdate();
			};
		}

		private void LinkLevelControlToApi(BasicTriList trilist, ConvergePro2DspJoinMap joinMap)
		{
			var maxLevelControls = LevelControlPoints.Count;
			if (maxLevelControls > joinMap.ChannelVolume.JoinSpan) maxLevelControls = (int)joinMap.ChannelVolume.JoinSpan;

			for (var i = 1; i <= maxLevelControls; i++)
			{
				var index = i - 1;

				var key = LevelControlPoints.ElementAt(index).Key;
				var channel = LevelControlPoints.ElementAt(index).Value;
				Debug.Console(_debugVerbose, this, @"LinkPresetsToApi: channel == null... continuing.");
				if (channel == null) continue;

				if (channel.Enabled == false) continue;

				var nameJoin = joinMap.ChannelName.JoinNumber + (ushort)index;
				var typeJoin = joinMap.ChannelType.JoinNumber + (ushort)index;
				var visibleJoin = joinMap.ChannelVisible.JoinNumber + (ushort)index;

				trilist.SetString(nameJoin, channel.Label);
				trilist.SetUshort(typeJoin, (ushort)channel.Type);
				trilist.SetBool(visibleJoin, channel.Enabled);

				var volumeSetJoin = joinMap.ChannelVolume.JoinNumber + (ushort)index;
				var volumeUpJoin = joinMap.ChannelVolumeUp.JoinNumber + (ushort)index;
				var volumeDownJoin = joinMap.ChannelVolumeDown.JoinNumber + (ushort)index;

				var channelWithFeedback = channel as IBasicVolumeWithFeedback;

				trilist.SetUShortSigAction(volumeSetJoin, channelWithFeedback.SetVolume);
				trilist.SetBoolSigAction(volumeUpJoin, channelWithFeedback.VolumeUp);
				trilist.SetBoolSigAction(volumeDownJoin, channelWithFeedback.VolumeDown);

				var muteToggleJoin = joinMap.ChannelMuteToggle.JoinNumber + (ushort)index;
				var muteOnJoin = joinMap.ChannelMuteOn.JoinNumber + (ushort)index;
				var muteOffJoin = joinMap.ChannelMuteOff.JoinNumber + (ushort)index;

				Debug.Console(_debugVerbose, this, @"LinkLevelControlToApi: 
	{0}-'{1}' 
	nameJoin-'{2}', 
	typeJoin-'{3}', 
	visibleJoin-'{4}', 
	volumeSetJoin-'{5}', 
	volumeUpJoin-'{6}', 
	volumeDownJoin-'{7}', 
	muteToggleJoin-'{8}', 
	muteOnJoin-'{9}', 
	muteOffJoin-'{10}'",
					key, channel.Label, nameJoin, typeJoin, visibleJoin, volumeSetJoin, volumeUpJoin, volumeDownJoin, muteToggleJoin, muteOnJoin, muteOffJoin);

				trilist.SetSigTrueAction(muteToggleJoin, channelWithFeedback.MuteToggle);
				trilist.SetSigTrueAction(muteOnJoin, channelWithFeedback.MuteOn);
				trilist.SetSigTrueAction(muteOffJoin, channelWithFeedback.MuteOff);

				channelWithFeedback.VolumeLevelFeedback.LinkInputSig(trilist.UShortInput[(uint)(joinMap.ChannelVolume.JoinNumber + index)]);

				channelWithFeedback.MuteFeedback.LinkInputSig(trilist.BooleanInput[(uint)(joinMap.ChannelMuteToggle.JoinNumber + index)]);
				channelWithFeedback.MuteFeedback.LinkInputSig(trilist.BooleanInput[(uint)(joinMap.ChannelMuteOn.JoinNumber + index)]);
				channelWithFeedback.MuteFeedback.LinkComplementInputSig(trilist.BooleanInput[(uint)(joinMap.ChannelMuteOff.JoinNumber + index)]);
			}

			trilist.OnlineStatusChange += (sender, args) =>
			{
				if (!args.DeviceOnLine) return;

				for (var i = 1; i <= maxLevelControls; i++)
				{
					var index = i - 1;

					var channel = LevelControlPoints.ElementAt(index).Value;
					if (channel == null) continue;

					var nameJoin = joinMap.ChannelName.JoinNumber + (ushort)index;
					var typeJoin = joinMap.ChannelType.JoinNumber + (ushort)index;
					var visibleJoin = joinMap.ChannelVisible.JoinNumber + (ushort)index;

					trilist.SetString(nameJoin, channel.Label);
					trilist.SetUshort(typeJoin, (ushort)channel.Type);
					trilist.SetBool(visibleJoin, channel.Enabled);

					var channelWithFeedback = channel as IBasicVolumeWithFeedback;

					channelWithFeedback.VolumeLevelFeedback.FireUpdate();
					channelWithFeedback.MuteFeedback.FireUpdate();
				}
			};
		}

		private void LinkPresetsToApi(BasicTriList trilist, ConvergePro2DspJoinMap joinMap)
		{
			var maxPresets = Presets.Count;
			Debug.Console(_debugVerbose, this, @"LinkPresetsToApi: maxPresets-'{0}'", maxPresets);

			if (maxPresets > joinMap.PresetRecall.JoinSpan) maxPresets = (int)joinMap.PresetRecall.JoinSpan;

			trilist.SetStringSigAction(joinMap.PresetRecall.JoinNumber, RunPresetByString);
			trilist.SetUShortSigAction(joinMap.PresetRecall.JoinNumber, RunPreset);

			for (var i = 1; i <= maxPresets; i++)
			{
				var index = i - 1;

				var presetKey = Presets.ElementAt(index).Key;
				var preset = Presets.ElementAt(index).Value;
				Debug.Console(_debugVerbose, this, @"LinkPresetsToApi: preset == null... continuing.");
				if (preset == null) continue;

				var nameJoin = joinMap.PresetName.JoinNumber + (ushort)index;
				var presetRecallJoin = joinMap.PresetRecall.JoinNumber + (ushort)index;

				Debug.Console(_debugVerbose, this, @"LinkPresetsToApi:
	{0}-'{1}'
	nameJoin-'{2}'
	presetRecallJoin-{3}",
					presetKey, preset.Label, nameJoin, presetRecallJoin);

				trilist.SetString(nameJoin, preset.Label);
				trilist.SetSigTrueAction(presetRecallJoin, () =>
				{
					Debug.Console(_debugVerbose, this, @"LinkPresetsToApi: trilist.SetSigTrueAction(presetRecallJoin) => {0} {1}",
						preset.Label, preset.Preset);
					RunPreset(preset);
				});
			}

			trilist.OnlineStatusChange += (sender, args) =>
			{
				if (!args.DeviceOnLine) return;

				for (var i = 1; i <= maxPresets; i++)
				{
					var index = i - 1;

					var preset = Presets.ElementAt(index).Value;
					if (preset == null) continue;

					var nameJoin = joinMap.PresetName.JoinNumber + (ushort)index;

					trilist.SetString(nameJoin, preset.Label);
				}
			};
		}

		private void LinkDialersToApi(BasicTriList trilist, ConvergePro2DspJoinMap joinMap)
		{
			// VoIP Dialer
			uint lineOffset = 0;
			foreach (var line in Dialers)
			{
				var dialer = line.Value;
				var dialerLineOffset = lineOffset;
				Debug.Console(_debugTrace, this, "AddingDialerBridge {0}, Offset: {1}", dialer.Key, dialerLineOffset);

				// dialer label
				trilist.SetString(joinMap.Label.JoinNumber, dialer.Label);
				dialer.LocalNumberFeedback.LinkInputSig(trilist.StringInput[joinMap.DisplayNumber.JoinNumber + dialerLineOffset]);

				// keypad commands
				for (var i = 0; i < joinMap.KeypadNumeric.JoinSpan; i++)
				{
					var keypadIndex = i;
					var joinOffset = keypadIndex + dialerLineOffset;
					trilist.SetSigTrueAction((uint)(joinMap.KeypadNumeric.JoinNumber + joinOffset), () => dialer.SendKeypad((ConvergePro2Dialer.EKeypadKeys)(keypadIndex)));
				}
				trilist.SetSigTrueAction((joinMap.KeypadStar.JoinNumber + dialerLineOffset), () => dialer.SendKeypad(ConvergePro2Dialer.EKeypadKeys.Star));
				trilist.SetSigTrueAction((joinMap.KeypadPound.JoinNumber + dialerLineOffset), () => dialer.SendKeypad(ConvergePro2Dialer.EKeypadKeys.Pound));
				trilist.SetSigTrueAction((joinMap.KeypadClear.JoinNumber + dialerLineOffset), () => dialer.SendKeypad(ConvergePro2Dialer.EKeypadKeys.Clear));
				trilist.SetSigTrueAction((joinMap.KeypadBackspace.JoinNumber + dialerLineOffset), () => dialer.SendKeypad(ConvergePro2Dialer.EKeypadKeys.Backspace));
				trilist.SetSigTrueAction(joinMap.KeypadDial.JoinNumber + dialerLineOffset, dialer.Dial);

				// dial & call controls
				trilist.SetSigTrueAction(joinMap.EndCall.JoinNumber + dialerLineOffset, dialer.EndAllCalls);
				trilist.SetSigTrueAction(joinMap.IncomingCallAnswer.JoinNumber + dialerLineOffset, dialer.AcceptCall);
				trilist.SetSigTrueAction(joinMap.IncomingCallReject.JoinNumber + dialerLineOffset, dialer.RejectCall);

				dialer.IncomingCallFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IncomingCall.JoinNumber + dialerLineOffset]);
				dialer.CallerIdNumberFeedback.LinkInputSig(trilist.StringInput[joinMap.CallerIdNumberFb.JoinNumber + dialerLineOffset]);
				//dialer.callerIdNameFeedback.LinkInputSig(trilist.StringInput[joinMap.CallerIdNameFb.JoinNumber + dialerLineOffset]);

				// hook state command
				trilist.SetSigTrueAction(joinMap.OnHook.JoinNumber + dialerLineOffset, dialer.EndAllCalls);
				trilist.SetSigTrueAction(joinMap.OffHook.JoinNumber + dialerLineOffset, () => dialer.SetHookState(1));

				dialer.OffHookFeedback.LinkInputSig(trilist.BooleanInput[joinMap.KeypadDial.JoinNumber + dialerLineOffset]);
				dialer.OffHookFeedback.LinkInputSig(trilist.BooleanInput[joinMap.OffHook.JoinNumber + dialerLineOffset]);
				dialer.OffHookFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.OnHook.JoinNumber + dialerLineOffset]);

				// dial string
				trilist.SetStringSigAction(joinMap.DialString.JoinNumber + dialerLineOffset, dialer.Dial);				
				dialer.DialStringFeedback.LinkInputSig(trilist.StringInput[joinMap.DialString.JoinNumber + dialerLineOffset]);

				// do not disturb controls
				trilist.SetSigTrueAction(joinMap.DoNotDisturbToggle.JoinNumber + dialerLineOffset, dialer.DoNotDisturbToggle);
				trilist.SetSigTrueAction(joinMap.DoNotDisturbOn.JoinNumber + dialerLineOffset, dialer.DoNotDisturbOn);
				trilist.SetSigTrueAction(joinMap.DoNotDisturbOff.JoinNumber + dialerLineOffset, dialer.DoNotDisturbOff);

				dialer.DoNotDisturbFeedback.LinkInputSig(trilist.BooleanInput[joinMap.DoNotDisturbToggle.JoinNumber + dialerLineOffset]);
				dialer.DoNotDisturbFeedback.LinkInputSig(trilist.BooleanInput[joinMap.DoNotDisturbOn.JoinNumber + dialerLineOffset]);
				dialer.DoNotDisturbFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.DoNotDisturbOff.JoinNumber + dialerLineOffset]);

				// auto answer controls
				trilist.SetSigTrueAction(joinMap.AutoAnswerToggle.JoinNumber + dialerLineOffset, dialer.AutoAnswerToggle);
				trilist.SetSigTrueAction(joinMap.AutoAnswerOn.JoinNumber + dialerLineOffset, dialer.AutoAnswerOn);
				trilist.SetSigTrueAction(joinMap.AutoAnswerOff.JoinNumber + dialerLineOffset, dialer.AutoAnswerOff);

				dialer.AutoAnswerFeedback.LinkInputSig(trilist.BooleanInput[joinMap.AutoAnswerToggle.JoinNumber + dialerLineOffset]);
				dialer.AutoAnswerFeedback.LinkInputSig(trilist.BooleanInput[joinMap.AutoAnswerOn.JoinNumber + dialerLineOffset]);
				dialer.AutoAnswerFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.AutoAnswerOff.JoinNumber + dialerLineOffset]);

				lineOffset = lineOffset + 50;
			}

			trilist.OnlineStatusChange += (sender, args) =>
			{
				if (!args.DeviceOnLine) return;

				foreach (var dialer in Dialers.Select(line => line.Value))
				{
					// dialer label
					trilist.SetString(joinMap.Label.JoinNumber, dialer.Label);

					dialer.AutoAnswerFeedback.FireUpdate();
					dialer.DoNotDisturbFeedback.FireUpdate();
					dialer.IncomingCallFeedback.FireUpdate();
					dialer.OffHookFeedback.FireUpdate();
					dialer.CallerIdNumberFeedback.FireUpdate();
					dialer.DialStringFeedback.FireUpdate();
				}
			};
		}

		#endregion

		/// <summary>
		/// Creates DSP objects
		/// </summary>
		public void CreateDspObjects()
		{
			Debug.Console(_debugVerbose, this, new string('*', 50));
			Debug.Console(_debugVerbose, this, new string('*', 50));
			
			// levelControls
			Debug.Console(_debugVerbose, this, "CreateDspObjects: _config.LevelControlPoints.Count-'{0}'",
				_config.LevelControlBlocks.Count);

			LevelControlPoints.Clear();
			if (_config.LevelControlBlocks != null)
			{
				foreach (var levelControlBlock in _config.LevelControlBlocks)
				{
					LevelControlPoints.Add(levelControlBlock.Key, new ConvergePro2DspLevelControl(levelControlBlock.Key, levelControlBlock.Value, this));

					Debug.Console(_debugVerbose, this, "CreateDspObjects: Added LevelControl {0}-'{1}' (ChannelName:'{2}', BlockName:'{3}', MuteParameter:'{4}')",
						levelControlBlock.Key, levelControlBlock.Value.Label, levelControlBlock.Value.ChannelName, levelControlBlock.Value.BlockName, levelControlBlock.Value.MuteParameter);
				}
			}

			Debug.Console(_debugVerbose, this, "CreateDspObjects: LevelControlPoints.Count-'{0}'",
				LevelControlPoints.Count);

			// presets			
			Debug.Console(_debugVerbose, this, "CreateDspObjects: _config.Presets.Count-'{0}'", 
				_config.Presets.Count);

			Presets.Clear();
			if (_config.Presets != null)
			{
				foreach (var preset in _config.Presets)
				{
					Presets.Add(preset.Key, preset.Value);

					Debug.Console(_debugVerbose, this, "CreateDspObjects: Added Preset {0}-'{1}' '{2}'",
						preset.Key, preset.Value.Label, preset.Value.Preset);
				}
			}

			Debug.Console(_debugVerbose, this, "CreateDspObjects: Presets.Count-'{0}'",
				Presets.Count);

			// dialers
			Debug.Console(_debugVerbose, this, "CreateDspObjects: _config.Dialers.Count-'{0}'",
				_config.Dialers.Count);

			Dialers.Clear();
			if (_config.Dialers != null)
			{
				foreach (var dialerConfig in _config.Dialers)
				{
					Dialers.Add(dialerConfig.Key, new ConvergePro2Dialer(dialerConfig.Key, dialerConfig.Value, this));

					Debug.Console(_debugVerbose, this, "CreateDspObjects: Added Dialer {0}-'{1}' (ChannelName:'{2}')",
						dialerConfig.Key, dialerConfig.Value.Label, dialerConfig.Value.ChannelName);
				}
			}

			Debug.Console(_debugVerbose, this, "CreateDspObjects: Dialers.Count-'{0}'",
				Dialers.Count);

			Debug.Console(_debugVerbose, this, new string('*', 50));
			Debug.Console(_debugVerbose, this, new string('*', 50));
		}

		/// <summary>
		/// Initiates the subscription process to the DSP
		/// </summary>
		void InitializeDspObjects()
		{
			foreach (var channel in LevelControlPoints.Where(channel => channel.Value.HasLevel))
			{
				channel.Value.GetCurrentMinMax();
				CrestronEnvironment.Sleep(250);
			}

			foreach (var channel in LevelControlPoints.Where(channel => channel.Value.HasLevel))
			{
				channel.Value.GetCurrentGain();
				CrestronEnvironment.Sleep(250);
			}

			foreach (var channel in LevelControlPoints.Where(channel => channel.Value.HasMute))
			{
				channel.Value.GetCurrentMute();
				CrestronEnvironment.Sleep(250);
			}

			foreach (var line in Dialers)
			{
				line.Value.GetHookState();
			}
		}

		private void OnCommMonitorStatusChange(object sender, MonitorStatusChangeEventArgs args)
		{
			Debug.Console(_debugVerbose, this, "Communication monitor state: {0}", args.Status);
			if (args.Status == MonitorStatus.IsOk && _initializeComplete)
			{
				InitializeDspObjects();
			}
		}

		private void OnSocketConnectionChange(object sender, GenericSocketStatusChageEventArgs args)
		{
			Debug.Console(_debugVerbose, this, "Socket state: {0}", args.Client.ClientStatus);

			if (args.Client.IsConnected)
			{
				if (Dialers == null) return;

				foreach (var dialer in Dialers)
				{
					dialer.Value.SubscribeToNotifications();
				}

				return;
			}

			_loggedIn = false;
			_comm.TextReceived += OnTextReceived;

			//var telnetNegotation = new byte[] { 0xFF, 0xFE, 0x01, 0xFF, 0xFE, 0x21, 0xFF, 0xFC, 0x01, 0xFF, 0xFC, 0x03 };
			//args.Client.SendBytes(telnetNegotation);
		}

		private void OnTextReceived(object dev, GenericCommMethodReceiveTextArgs args)
		{
			//Debug.Console(_debugVerbose, this, "OnTextReceived args.Text: '{0}'", args.Text);
			if (string.IsNullOrEmpty(args.Text))
			{
				Debug.Console(_debugVerbose, this, "OnTextReceived: args.Text '{0}' is null or empty", args.Text);
				return;
			}

			try
			{
				Debug.Console(_debugVerbose, this, "OnTextReceived args.Text: '{0}'", args.Text);
				if (args.Text.Contains("Username:"))
				{
					SendText(_config.Control.TcpSshProperties.Username);
					return;
				}

				if (args.Text.Contains("Password:"))
				{
					SendText(_config.Control.TcpSshProperties.Password);
					return;
				}

				_loggedIn = args.Text.Contains("=>");

				if (!_loggedIn) return;

				_comm.TextReceived -= OnTextReceived;
				InitializeDspObjects();
			}
			catch (Exception ex)
			{
				Debug.Console(_debugNotice, this, Debug.ErrorLogLevel.Error, "OnTextReceived Exception {0}", ex.Message);
				Debug.Console(_debugVerbose, this, Debug.ErrorLogLevel.Error, "** Stack Trace:\n{0}",
					ex.StackTrace);
				if (ex.InnerException != null)
					Debug.Console(_debugVerbose, this, Debug.ErrorLogLevel.Error, "** Inner Exception:\n'{0}'",
						ex.InnerException);
			}
		}

		/// <summary>
		/// Handles a response message from the DSP
		/// </summary>
		/// <example>
		/// "{CommandType} {ChannelName} {BlockName} {ParameterName} {Value}\x0a"
		/// "EP MyChannel LEVEL GAIN -5.0\x0a\x0d"
		/// </example>
		/// <param name="dev"></param>
		/// <param name="args"></param>
		private void OnLineRecieved(object dev, GenericCommMethodReceiveTextArgs args)
		{
			_heartbeatTracker = 0;

			if (string.IsNullOrEmpty(args.Text))
			{
				Debug.Console(_debugVerbose, this, "OnLineRecieved: args.Text '{0}' is null or empty", args.Text);
				return;
			}

			try
			{
				Debug.Console(_debugVerbose, this, "OnLineRecieved args.Text: '{0}'", args.Text);
				_commRxQueue.Enqueue(new ProcessStringMessage(args.Text, ProcessResponse));
			}
			catch (Exception ex)
			{
				Debug.Console(_debugNotice, this, Debug.ErrorLogLevel.Error, "OnLineRecieved Exception {0}", ex.Message);
				Debug.Console(_debugVerbose, this, Debug.ErrorLogLevel.Error, "** Stack Trace:\n{0}",
					ex.StackTrace);
				if (ex.InnerException != null)
					Debug.Console(_debugVerbose, this, Debug.ErrorLogLevel.Error, "** Inner Exception:\n'{0}'",
						ex.InnerException);
			}
		}

		private void ProcessResponse(string response)
		{
			try
			{
				if (string.IsNullOrEmpty(response)) return;

				Debug.Console(_debugVerbose, this, "ProcessResponse: '{0}'", response);

				// => BOX MAIN_DSP UNIT SN 0000-0000-00<LF><CR>
				// => EP MyChannel LEVEL GAIN -5.0<LF><CR>
				// => EP MyCHannel LEVEL MUTE 2<LF><CR>
				// => Error Invalid Paramter(s)<LF><CR>
				// => EP UA 101 NOTIFICATION INDICATION PL 1;PARTY_LINE:ON
				// => EP UA 101 NOTIFICATION STATE_CHANGE PL 1;DIALTONE
				// => EP UA 101 NOTIFICATION INDICATION PL 1;INPROCESS;{PHONE_NUMBER_DIALED}
				// => EP UA 101 NOTIFICATION INDICATION PL NA;RINGBACK:ON
				// => EP UA 101 NOTIFICATION STATE_CHANGE PL 1;NA;RINGBACK:OFF
				var expression =
					new Regex(
					//@"^=>\s*(?<CommandType>\w+)\s+(?<ChannelName>\w+)\s+(?<BlockName>\w+)\s+(?<ParameterName>\w+)(?:\s+(?<Value>[\w\-\.\s]+))?",
						@"\W+(?<CommandType>BOX|EP UA|EP)\s+(?<ChannelName>\w+)\s+(?<BlockName>\w+)\s+(?<ParameterName>\w+)\s+(?<Value>.*)",
						RegexOptions.None);
				var results = expression.Match(response);
				if (!results.Success)
				{
					//Debug.Console(_debugVerbose, this, "ProcessResponse: regex failed to find a matching pattern");
					return;
				}

				Debug.Console(_debugVerbose, this, "ProcessRsponse: results.Groups.Count == {0}", results.Groups.Count);

				var commandType = results.Groups["CommandType"].Success
					? results.Groups["CommandType"].Value
					: string.Empty;

				var channelName = results.Groups["ChannelName"].Success
					? results.Groups["ChannelName"].Value
					: string.Empty;

				var blockName = results.Groups["BlockName"].Success
					? results.Groups["BlockName"].Value
					: string.Empty;

				var parameterName = results.Groups["ParameterName"].Success
					? results.Groups["ParameterName"].Value
					: string.Empty;

				var value = results.Groups["Value"].Success
					? results.Groups["Value"].Value.Trim()
					: string.Empty;

				if (string.IsNullOrEmpty(commandType) || commandType.Equals("Error"))
				{
					Debug.Console(_debugNotice, this, "ProcessResponse: {0}", response.Replace("=>", "").Trim());
					return;
				}

				Debug.Console(_debugVerbose, this, "ProcessResponse: CommandType-'{0}', ChannelName-'{1}', BlockName-'{2}', ParameterName-'{3}', Value-'{4}'",
					commandType, channelName, blockName, parameterName, value);

				if (string.IsNullOrEmpty(channelName) || string.IsNullOrEmpty(parameterName))
				{
					Debug.Console(_debugVerbose, this, "ProcessResponse: channelName-'{0}' || parameterName-'{1}' is empty or null", channelName, parameterName);
					return;
				}

				switch (commandType)
				{
					case "EP":
					case "EP UA":
						{
							switch (parameterName)
							{
								case "GAIN":
								case "MUTE":
								case "MIN_GIAN":
								case "MAX_GAIN":
									{
										Debug.Console(_debugNotice, this, "ProcessResponse: found parameter '{0}' response", parameterName);

										foreach (var controlPoint in LevelControlPoints.Where(controlPoint => channelName == controlPoint.Value.ChannelName))
										{
											controlPoint.Value.ParseResponse(parameterName, new[] { value });
										}
										break;
									}
								// "EP '<EPT> <EPN>' INQUIRE AUTO_ANSWER_RINGS [VALUE]"
								// "EP '<EPT> <EPN>' NOTIFICATON INCOMING_CALL [VALUE]"
								//case "RING":
								case "AUTO_ANSWER":
								case "AUTO_ANSWER_RINGS":
								case "AUTO_DISCONNECT_MODE":
								case "KEY_CALL":
								case "KEY_HOOK_FLASH":
								case "KEY_REDIAL":
								case "KEY_HOOK":
								case "KEY_DO_NOT_DISTURB":
								case "CALLER_ID":
								case "HOOK":								
								case "INCOMING_CALL":
									{
										Debug.Console(_debugNotice, this, "ProcessResponse: found parameter '{0}' response", parameterName);

										foreach (var dialer in Dialers.Where(dialer => channelName == dialer.Value.ChannelName))
										{
											dialer.Value.ParseResponse(parameterName, value.Split(';'));
											return;
										}

										break;
									}
								case "ACTIVE_PARTIES":
									foreach (var dialer in Dialers.Where(dialer => channelName == dialer.Value.ChannelName))
									{
										var responses = value.Split(';');
										if (responses.Any(r => r.ToUpper().Contains("INCOMING")))
										{
											dialer.Value.IncomingCallHandler(responses);
											return;
										}

										dialer.Value.ActivePartiesHandler(responses);

										return;
									}

									break;
								case "INDICATION":
									{
										foreach (var dialer in Dialers.Where(dialer => channelName == dialer.Value.ChannelName))
										{
											var responses = value.Split(';');
											if (responses.Any(r => r.ToUpper().Contains("INCOMING")))
											{
												dialer.Value.IncomingCallHandler(responses);
												return;
											}

											dialer.Value.IndicationHandler(responses);

											return;
										}

										break;
									}
								case "STATE_CHANGE":
									{
										foreach (var dialer in Dialers.Where(dialer => channelName == dialer.Value.ChannelName))
										{
											var responses = value.Split(';');
											if (responses.Any(r => r.ToUpper().Contains("INCOMING")))
											{
												dialer.Value.IncomingCallHandler(responses);
												return;
											}

											dialer.Value.StateChangeHandler(responses);

											return;
										}

										break;
									}								
								default:
									{
										Debug.Console(_debugNotice, this, "ProcessResponse: unhandled parameter '{0}'", parameterName);
										break;
									}
							}

							break;
						}
					default:
						{
							Debug.Console(_debugVerbose, this, "ProcessResponse: Unhandled Response\r\tCommandType-'{0}', ChannelName-'{1}', BlockName-'{2}', ParameterName-'{3}', Value-'{4}'",
								commandType, channelName, blockName, parameterName, value);
							break;
						}
				}
			}
			catch (Exception e)
			{
				Debug.Console(_debugNotice, this, "ProcessResponse Exception {1} parsing response: '{0}'", response, e.Message);
				Debug.Console(_debugVerbose, this, Debug.ErrorLogLevel.Error, "** StackTrace:\n{0}", e.StackTrace);
				if (e.InnerException != null) Debug.Console(_debugNotice, this, Debug.ErrorLogLevel.Error, "** InnerException:\n{0}", e.InnerException);
			}

		}

		/// <summary>
		/// Sends a command to the DSP (with delimiter appended)
		/// </summary>
		/// <param name="s">Command to send</param>
		public void SendText(string s)
		{
			if (s == null) return;

			Debug.Console(_debugNotice, this, "TX: '{0}'", s);
			var text = string.Format("{0}{1}", s, CommCommandDelimter);
			_comm.SendText(text);
		}

		// Checks the comm health, should be called by comm monitor only. If no heartbeat has been detected recently, will clear the queue and log an error.
		private void HeartbeatPoll()
		{
			_heartbeatTracker++;
			SendText(string.Format("BOX {0} UNIT SN", BoxName));
			CrestronEnvironment.Sleep(1000);

			if (_heartbeatTracker > 0)
			{
				Debug.Console(_debugNotice, this, "Heartbeat missed, count {0}", _heartbeatTracker);

				if (_heartbeatTracker == 5)
					Debug.Console(_debugNotice, this, Debug.ErrorLogLevel.Warning, "Heartbeat missed 5 times");
			}
			else
			{
				Debug.Console(_debugVerbose, this, "Heartbeat okay");
			}
		}

		/// <summary>
		/// Runs the presetConfig with the number provided
		/// </summary>
		/// <param name="preset"></param>
		public void RunPreset(ushort preset)
		{
			Debug.Console(_debugNotice, this, "RunPreset: '{0}'", preset);

			var p = Presets.ElementAt(preset).Value;
			if (p == null) return;

			RunPreset(p);
		}

		/// <summary>
		/// Runs the presetConfig object provided
		/// </summary>
		/// <param name="presetConfig">ConvergePro2DspPresetConfig</param>
		public void RunPreset(ConvergePro2DspPresetConfig presetConfig)
		{
			RunPresetByString(presetConfig.Preset);
		}

		/// <summary>
		/// Sends a command to execute a presetConfig
		/// </summary>
		/// <param name="preset">Preset Name</param>
		public void RunPresetByString(string preset)
		{
			SendText(string.Format("MCCF {1}", preset));
		}

		#region DebugLevels

		private uint _debugTrace;
		private uint _debugNotice;
		private uint _debugVerbose;

		public void ResetDebugLevels()
		{
			_debugTrace = 0;
			_debugNotice = 1;
			_debugVerbose = 2;
		}

		public void SetDebugLevels(uint level)
		{
			_debugTrace = level;
			_debugNotice = level;
			_debugVerbose = level;
		}

		#endregion


		#region EmulateEvents

		/// <summary>
		/// Emulates the incoming call responses, provided the `channelName`
		/// </summary>
		/// <param name="channelName">string</param>
		public void EmulateIncomingCall(string channelName)
		{
			var incomingCallResponses = new List<string>
			{
				"=> EP UA {0} NOTIFICATION INDICATION PL;RINING:ON{1}",
				"=> EP UA {0} NOTIFICATION INDICATION PL 1;PARTY_LINE:BLINK{1}",
				"=> EP UA {0} NOTIFICATION STATE_CHANGE PL 1;INCOMING:\"77897 S7/B1020/3 North\" <SIP:77897@154.70.4.100>{1}",
				"=> EP UA {0} NOTIFICATION INDICATION PL NA;RINGING:OFF{1}",
				"=> EP UA {0} NOTIFICATION STATE_CHANGE PL 1:IDLE{1}",
				"=> EP UA {0} NOTIFICATION INDICATION PL 1;PARTY_LINE:OFF{1}"
			};

			foreach (var response in incomingCallResponses)
			{
				var virtualResponse = string.Format(response, channelName, CommGatherDelimiter);

				OnLineRecieved(this, new GenericCommMethodReceiveTextArgs(virtualResponse));
			}
		}

		#endregion
	}
}