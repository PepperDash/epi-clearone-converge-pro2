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
using PepperDash.Essentials.Devices.Common.DSP;

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

		private const string CommCommandDelimter = "\x0D";
		private const string CommGatherDelimiter = "\x0A";

		public BoolFeedback IsOnlineFeedback { get { return CommMonitor.IsOnlineFeedback; } }
		public IntFeedback CommMonitorFeedback { get; private set; }
		public IntFeedback SocketStatusFeedback { get; private set; }

		private readonly ConvergePro2DspConfig _config;
		private uint _heartbeatTracker;
		private bool _initializeComplete;

		public string BoxName { get; set; }
		public Dictionary<string, ConvergePro2DspLevelControl> LevelControlPoints { get; private set; }
		public List<ConvergePro2DspPresetConfig> PresetList = new List<ConvergePro2DspPresetConfig>();
		public Dictionary<string, ConvergePro2DspDialer> Dialers { get; set; }

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

				_comm = comm;
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
				Dialers = new Dictionary<string, ConvergePro2DspDialer>();

				Debug.Console(_debugVerbose, this, new string('*', 50));
				Debug.Console(_debugVerbose, this, new string('*', 50));

				CreateDspObjects();
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
			InitializeDspObjects();

			_initializeComplete = true;

			base.Initialize();
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
			Debug.Console(_debugVerbose, this, "Socket state: {0}, IsConnected: {1}", args.Client.ClientStatus, args.Client.IsConnected ? "true" : "false");
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
			
			if(CommMonitorFeedback != null)
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
			var maxPresets = PresetList.Count;
			if (maxPresets > joinMap.PresetRecall.JoinSpan) maxPresets = (int)joinMap.PresetRecall.JoinSpan;

			trilist.SetStringSigAction(joinMap.PresetRecall.JoinNumber, RunPresetByString);
			trilist.SetUShortSigAction(joinMap.PresetRecall.JoinNumber, RunPreset);

			for (var i = 1; i <= maxPresets; i++)
			{
				var index = i - 1;

				var preset = PresetList.ElementAt(index);
				if (preset == null) continue;

				var nameJoin = joinMap.PresetName.JoinNumber + (ushort)index;
				var presetRecallJoin = joinMap.PresetRecall.JoinNumber + (ushort)index;

				trilist.SetString(nameJoin, preset.Label);
				trilist.SetSigTrueAction(presetRecallJoin, () => RunPreset(preset));
			}

			trilist.OnlineStatusChange += (sender, args) =>
			{
				if (!args.DeviceOnLine) return;

				for (var i = 1; i <= maxPresets; i++)
				{
					var index = i - 1;

					var preset = PresetList.ElementAt(index);
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
				Debug.Console(_debugTrace, "AddingDialerBridge {0}, Offset: {1}", dialer.Key, dialerLineOffset);

				// dialer label
				trilist.SetString(joinMap.Label.JoinNumber, dialer.Config.Label);

				// keypad commands
				for (var i = 0; i < joinMap.KeypadNumeric.JoinSpan; i++)
				{
					var keypadIndex = i;
					var joinOffset = keypadIndex + dialerLineOffset;
					trilist.SetSigTrueAction((uint)(joinMap.KeypadNumeric.JoinNumber + joinOffset), () => dialer.SendKeypad((ConvergePro2DspDialer.EKeypadKeys)(keypadIndex)));
				}
				trilist.SetSigTrueAction((joinMap.KeypadStar.JoinNumber + dialerLineOffset), () => dialer.SendKeypad(ConvergePro2DspDialer.EKeypadKeys.Star));
				trilist.SetSigTrueAction((joinMap.KeypadPound.JoinNumber + dialerLineOffset), () => dialer.SendKeypad(ConvergePro2DspDialer.EKeypadKeys.Pound));
				trilist.SetSigTrueAction((joinMap.KeypadClear.JoinNumber + dialerLineOffset), () => dialer.SendKeypad(ConvergePro2DspDialer.EKeypadKeys.Clear));
				trilist.SetSigTrueAction((joinMap.KeypadBackspace.JoinNumber + dialerLineOffset), () => dialer.SendKeypad(ConvergePro2DspDialer.EKeypadKeys.Backspace));
				trilist.SetSigTrueAction(joinMap.KeypadDial.JoinNumber + dialerLineOffset, dialer.Dial);

				// dial & call controls
				trilist.SetSigTrueAction(joinMap.EndCall.JoinNumber + dialerLineOffset, dialer.EndAllCalls);
				//trilist.SetSigTrueAction(joinMap.IncomingCallAccept.JoinNumber + dialerLineOffset, dialer.AcceptCall());
				//trilist.SetSigTrueAction(joinMap.IncomingCallReject.JoinNumber + dialerLineOffset, dialer.RejectCall());

				dialer.IncomingCallFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IncomingCall.JoinNumber + dialerLineOffset]);
				dialer.CallerIdNumberFeedback.LinkInputSig(trilist.StringInput[joinMap.CallerIdNumberFb.JoinNumber + dialerLineOffset]);
				//dialer.callerIdNameFeedback.LinkInputSig(trilist.StringInput[joinMap.CallerIdNameFb.JoinNumber + dialerLineOffset]);

				// hook state command
				dialer.OffHookFeedback.LinkInputSig(trilist.BooleanInput[joinMap.KeypadDial.JoinNumber + dialerLineOffset]);
				dialer.OffHookFeedback.LinkInputSig(trilist.BooleanInput[joinMap.OffHook.JoinNumber + dialerLineOffset]);
				dialer.OffHookFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.OnHook.JoinNumber + dialerLineOffset]);

				// dial string
				trilist.SetStringSigAction(joinMap.DialString.JoinNumber + dialerLineOffset, dialer.Dial);
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
					trilist.SetString(joinMap.Label.JoinNumber, dialer.Config.Label);

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
			Debug.Console(_debugVerbose, this, "Creating DSP Objects");

			LevelControlPoints.Clear();
			PresetList.Clear();
			Dialers.Clear();

			if (_config.LevelControlBlocks != null)
			{
				foreach (var block in _config.LevelControlBlocks)
				{
					LevelControlPoints.Add(block.Key, new ConvergePro2DspLevelControl(block.Key, block.Value, this));

					Debug.Console(_debugVerbose, this, "Added LevelControl {0}-'{1}' (EPT:'{2}', EPN:'{3}', BN:'{4}')",
						block.Key, block.Value.Label, block.Value.EndpointType, block.Value.EndpointNumber, block.Value.BlockNumber);
				}
			}

			if (_config.Presets != null)
			{
				foreach (var preset in _config.Presets)
				{
					AddPreset(preset.Value);

					Debug.Console(_debugVerbose, this, "Added Preset {0}-'{1}' '{2}' (EPT:'{3}', EPN:'{4}', BN:'{5}')",
						preset.Key, preset.Value.Label, preset.Value.Preset, preset.Value.EndpointType, preset.Value.EndpointNumber, preset.Value.BlockNumber);
				}
			}

			if (_config.Dialers != null)
			{
				foreach (var dialerConfig in _config.Dialers)
				{
					var value = dialerConfig.Value;
					var key = dialerConfig.Key;

					Dialers.Add(key, new ConvergePro2DspDialer(key, value, this));

					Debug.Console(_debugVerbose, this, "Added Dialer {0}-'{1}' (EPT:'{2}', EPN:'{3}', BN:'{4}', ChannelName:'{5}')",
						key, value.Label, value.EndpointType, value.EndpointNumber, value.BlockNumber, value.ChannelName);
				}
			}

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

		/// <summary>
		/// Handles a response message from the DSP
		/// </summary>
		/// <example>
		/// "{CMD_TYPE} {EPT} {EPN} {BN} {PN} [VALUE]\x0a"
		/// "EP MIC 103 LEVEL MUTE 0\x0a"
		/// "EP PROC 201 LEVEL GAIN -5\x0a"
		/// </example>
		/// <param name="dev"></param>
		/// <param name="args"></param>
		private void OnLineRecieved(object dev, GenericCommMethodReceiveTextArgs args)
		{
			Debug.Console(_debugVerbose, this, "OnLineRecieved args.Text: '{0}'", args.Text);
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
				// option 1
				/*
				var expression = new Regex(
						@"(?<commandtype>.*) (?<endpointType>.*) (?<endpointNumber>.*) (?<blockNumber>.*) (?<parameterName>.*) (?<value>.*)?$",
						RegexOptions.None);
				var matches = expression.Match(response);
				if (!matches.Success)
				{
					Debug.Console(_debugVerbose, this, "ProcessResponse: unknown response '{0}', regex match failed", response);
					return;
				}

				var commandType = matches.Groups["commandType"].Value;
				var endpointType = matches.Groups["endpointType"].Value;
				var endpointNumber = matches.Groups["endpointNumber"].Value;
				var blockNumber = matches.Groups["blockNumber"].Value;
				var parameterName = matches.Groups["parameterName"].Value;
				var value = matches.Groups["value"].Value;

				Debug.Console(_debugVerbose, this, "ProcessResponse: [{0}, {1}, {2}, {3}, {4}, {5}]",
					commandType, endpointType, endpointNumber, blockNumber, parameterName, value);
				*/

				// option 2				
				var data = response.Split(' ');
				if (data == null)
				{
					Debug.Console(_debugVerbose, this, "ProcessResponse: failed to process response");
					return;
				}

				var commandType = data[0] ?? "null";
				var endpointType = data[1] ?? "null";
				var endpointNumber = data[2] ?? "null";
				var blockNumber = data[3] ?? "null";
				var parameterName = data[4] ?? "null";
				var value = data[5] ?? "null";

				Debug.Console(_debugVerbose, this, "ProcessResponse: [{0}, {1}, {2}, {3}, {4}, {5}]",
					commandType, endpointType, endpointNumber, blockNumber, parameterName, value);

				switch (commandType)
				{
					case "EP":
						{
							switch (parameterName)
							{
								case "GAIN":
								case "MUTE":
								case "MIN_GIAN":
								case "MAX_GAIN":
									{
										Debug.Console(_debugNotice, this, "ProcessResponse: found parameter '{0}' response", parameterName);

										foreach (var controlPoint in LevelControlPoints)
										{
											if (endpointType != (controlPoint.Value).EndpointType && endpointNumber != (controlPoint.Value).EndpointNumber)
											{
												continue;
											}

											controlPoint.Value.ParseResponse(parameterName, new[] { value });
										}
										break;
									}
								case "AUTO_ANSWER_RINGS":
								case "AUTO_DISCONNECT_MODE":
								case "KEY_CALL":
								case "KEY_HOOK_FLASH":
								case "KEY_REDIAL":
								case "KEY_HOOK":
								case "INCOMING_CALL":
								case "CALLER_ID":
								case "HOOK":
								case "RING":
									{
										Debug.Console(_debugNotice, this, "ProcessResponse: found parameter '{0}' response", parameterName);

										foreach (var dialer in Dialers.Where(dialer => endpointType == dialer.Value.Config.EndpointType))
										{
											dialer.Value.ParseResponse(parameterName, new[] { value });
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
							Debug.Console(_debugNotice, this, "ProcessResponse: unhandled response '{0} {1} {2} {3} {4} {5}'",
								commandType, endpointType, endpointNumber, blockNumber, parameterName, value);
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
		public void SendLine(string s)
		{
			Debug.Console(_debugNotice, this, "TX: '{0}'", s);
			_comm.SendText(s + CommCommandDelimter);
		}

		// Checks the comm health, should be called by comm monitor only. If no heartbeat has been detected recently, will clear the queue and log an error.
		private void HeartbeatPoll()
		{
			_heartbeatTracker++;
			SendLine(string.Format("BOX {0} UNIT SN", BoxName));
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
		/// Adds a presst
		/// </summary>
		/// <param name="s">ConvergeProDspPresets</param>
		public void AddPreset(ConvergePro2DspPresetConfig s)
		{
			PresetList.Add(s);
		}

		/// <summary>
		/// Runs the presetConfig with the number provided
		/// </summary>
		/// <param name="preset"></param>
		public void RunPreset(ushort preset)
		{
			Debug.Console(_debugNotice, this, "RunPreset: '{0}'", preset);

			if (0 < preset && preset <= PresetList.Count && PresetList[preset - 1] != null)
			{
				RunPreset(PresetList[preset - 1]);
			}
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
			SendLine(string.Format("MCCF {1}", preset));
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
	}
}