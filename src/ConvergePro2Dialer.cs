using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.Codec;

namespace ConvergePro2DspPlugin
{
	/// <summary>
	/// Telco Dialer
	/// </summary>
	/// <remarks>
	/// API 
	/// 2.4.22 TELCO_RX - PDF Page 251
	/// 2.4.23 TELCO_TX - PDF Page 267
	/// 2.4.24 UA - PDF Page 269
	/// </remarks>
	public class ConvergePro2Dialer : IHasDialer, IKeyed
	{
		/// <summary>
		/// Dialer Key
		/// </summary>
		public string Key { get; protected set; }

		/// <summary>
		/// Parent DSP
		/// </summary>
		public ConvergePro2Dsp Parent { get; private set; }

		public string Label { get; private set; }
		public string ChannelName { get; private set; }
		public string BlockName { get; private set; }
		public string LevelParameter { get; private set; }
		public string MuteParameter { get; private set; }
		public bool ClearOnHangup { get; private set; }

		/// <summary>
		/// Tracks if the dialer is VoIP or TELCO
		/// </summary>
		public bool IsVoipDialer { get; private set; }

		/// <summary>
		/// Tracks in call state
		/// </summary>
		public bool IsInCall { get; private set; }

		/// <summary>
		/// Dialer local phone number feedback
		/// </summary>
		public StringFeedback LocalNumberFeedback;
		// local phone number backer field
		private string _localNumber;
		/// <summary>
		/// Dialer local phone number property
		/// </summary>
		public String LocalNumber
		{
			get { return _localNumber; }
			private set
			{
				_localNumber = value;
				LocalNumberFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Dial string feedback 
		/// </summary>
		public StringFeedback DialStringFeedback;
		// Dial string backer field
		private string _dialString;
		/// <summary>
		/// Dial string property
		/// </summary>
		public string DialString
		{
			get { return _dialString; }
			private set
			{
				_dialString = value;
				DialStringFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Off hook feedback
		/// </summary>
		public BoolFeedback OffHookFeedback;
		// Off hook backer field
		private bool _offHook;
		/// <summary>
		/// Off Hook property
		/// </summary>
		public bool OffHook
		{
			get { return _offHook; }
			set
			{
				_offHook = value;
				OffHookFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Auto answer feedback
		/// </summary>
		public BoolFeedback AutoAnswerFeedback;
		// Auto answer backer field
		private bool _autoAnswerState;
		/// <summary>
		/// Auto answer property
		/// </summary>
		public bool AutoAnswerState
		{
			get { return _autoAnswerState; }
			private set
			{
				_autoAnswerState = value;
				AutoAnswerFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Do not disturb feedback
		/// </summary>
		public BoolFeedback DoNotDisturbFeedback;
		// Do not disturb backer field
		private bool _doNotDisturbState;
		/// <summary>
		/// Do not disturb property
		/// </summary>
		public bool DoNotDisturbState
		{
			get { return _doNotDisturbState; }
			private set
			{
				_doNotDisturbState = value;
				DoNotDisturbFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Caller ID number feedback
		/// </summary>
		public StringFeedback CallerIdNumberFeedback;
		// Caller ID number backer field
		private string _callerIdNumber;
		/// <summary>
		///  Caller ID Number property
		/// </summary>
		public string CallerIdNumber
		{
			get { return _callerIdNumber; }
			set
			{
				_callerIdNumber = value;
				CallerIdNumberFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Incoming call feedback
		/// </summary>
		public BoolFeedback IncomingCallFeedback;
		// Incoming call backer field
		private bool _incomingCall;
		/// <summary>
		/// Incoming call property
		/// </summary>
		public bool IncomingCall
		{
			get { return _incomingCall; }
			set
			{
				_incomingCall = value;
				IncomingCallFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="key"></param>
		/// <param name="config">configuration object</param>
		/// <param name="parent">parent dsp instance</param>
		public ConvergePro2Dialer(string key, ConvergePro2DspDialerConfig config, ConvergePro2Dsp parent)
		{			
			Key = key;
			Parent = parent;
			IsVoipDialer = config.IsVoip;
			Label = config.Label;
			ChannelName = config.ChannelName;

			ClearOnHangup = config.ClearOnHangup;

			LocalNumberFeedback = new StringFeedback(() => LocalNumber);
			IncomingCallFeedback = new BoolFeedback(() => IncomingCall);
			DialStringFeedback = new StringFeedback(() => DialString);
			OffHookFeedback = new BoolFeedback(() => OffHook);
			AutoAnswerFeedback = new BoolFeedback(() => AutoAnswerState);
			DoNotDisturbFeedback = new BoolFeedback(() => DoNotDisturbState);
			CallerIdNumberFeedback = new StringFeedback(() => CallerIdNumber);

			Initialize(key, config);			
		}

		private Dictionary<string, Action<string[]>> _handlers;

		// Converge Pro 2 Serial Commands, PDF: 276
		// => EP UA 101 NOTIFICATIONS STATE_CHANGE PL 1;DIALTONE
		// => EP UA 101 NOTIFICATIONS STATE_CHANGE PL 1;NA;RINGBACK:OFF
		// => EP UA 101 NOTIFICATIONS INDICATION PL 1;PARTY_LINE:ON
		// => EP UA 101 NOTIFICATIONS INDICATION PL 1;INPROCESS;{PHONE_NUMBER_DIALED}
		// => EP UA 101 NOTIFICATIONS INDICATION PL NA;RINGBACK:ON
		private readonly List<string> _offHookValues = new List<string>
		{
            "ACTIVE",
			"DIAL_TONE",
			"DIALING",
            "INPROCESS",
            "RINGING"
		};

		private readonly List<string> _incomingCallValues = new List<string>
		{
			"INCOMING"
		};

		/// <summary>
		/// Initializes dialer
		/// </summary>
		/// <param name="key"></param>
		/// <param name="config"></param>
		public void Initialize(string key, ConvergePro2DspDialerConfig config)
		{
			Key = string.Format("{0}-{1}", Parent.Key, key);

			DeviceManager.AddDevice(this);

			if (IsVoipDialer) SubscribeToNotifications();

			_handlers = new Dictionary<string, Action<string[]>>
			{
				{
					"STATE_CHANGE", v => OffHook = _offHookValues.Any(s=>s.Contains(v[0]))
				},
				{
					"INDICATION", null	
				},
				{
					"INCOMING_CALL", v => IncomingCall = _incomingCallValues.Any(s=>s.Contains(v[0]))
				},				
				{
					"HOOK", v => OffHook = v[0] == "1"
				},
				{
					"LOCAL_NUMBER", v=> LocalNumber = v[0]
				},
				{
					"RING", null	
				},
				{
					"AUTO_ANSWER_RINGS", null
				},
				{
					"AUTO_DISCONNECT_MODE", null
				},
				{
					"KEY_CALL", null
				},
				{
					"KEY_HOOK_FLASH", null
				},
				{
					"KEY_REDIAL", null
				},
				{
					"CALLER_ID", null
				},
				{
					"ERROR", v => Debug.Console(2, this, "ERROR: {0}", v.ToString())
				}
			};
		}

		/// <summary>
		/// Subscribe to VoIP notifications
		/// </summary>
		public void SubscribeToNotifications()
		{
			if (!IsVoipDialer) return;

			var notifications = new List<string>
			{
                "STATE_CHANGE IDLE",
				"STATE_CHANGE DIAL_TONE",
				"STATE_CHANGE DIALING",
				"STATE_CHANGE RINGING",
				"INDICATION PL NA;HOLD;ON;RINGINING",
                "ERROR ERROR_CALL_ACTIVE",
                "REG_FAILED",
                "REG_SUCCEED"                
			};

			foreach (var notification in notifications)
			{
				var cmd = string.Format("EP UA {0} NOTIFCATION {1}", ChannelName, notification);
				SendText(cmd);
			}
		}

		/// <summary>
		/// Call status change event
		/// Interface requires this
		/// </summary>
		public event EventHandler<CodecCallStatusItemChangeEventArgs> CallStatusChange;

		/// <summary>
		/// Call status event handler
		/// </summary>
		/// <param name="args"></param>
		public void OnCallStatusChange(CodecCallStatusItemChangeEventArgs args)
		{
			var handler = CallStatusChange;
			if (handler == null) return;
			CallStatusChange(this, args);
		}		

		/// <summary>
		/// Parses the response from the DSP. Command is "MUTE, GAIN, MINMAX, erc. Values[] is the returned values after the channel and group.
		/// </summary>
		/// <example>
		/// {CMD_TYPE} {ENDPOINT_TYPE (EPT)} {ENDPOINT_NUMBER (EPN)} {BLOCK_NUMBER (BN)} {PARAMETER_NAME (PN)} [{VALUE}]
		/// "EP MIC 103 LEVEL MUTE 0"
		/// "EP PROC 201 LEVEL GAIN -5"
		/// </example>
		/// <param name="parameterName"></param>
		/// <param name="values"></param>
		public void ParseResponse(string parameterName, string[] values)
		{
			Debug.Console(1, this, "ProceseResponse: parameterName-'{0}' values-'{1}'", parameterName, string.Join(", ", values));

			Action<string[]> handler;
			if (!_handlers.TryGetValue(parameterName, out handler))
			{
				Debug.Console(2, this, "ResponseRecieved: unhandled response '{0} {1}'", parameterName, values.ToString());
				return;
			}

			handler(values);		
		}


		private void SendText(string cmd)
		{
			if (string.IsNullOrEmpty(cmd))
			{
				Debug.Console(1, this, "SendText: cmd is null or empty");
				return;
			}

			Debug.Console(2, this, "SendText: {0}", cmd);

			Parent.SendText(cmd);
		}

		/// <summary>
		/// Toggles the do not disturb state
		/// </summary>
		public void DoNotDisturbToggle()
		{
			var dndStateInt = !DoNotDisturbState ? 1 : 0;

			var cmd = IsVoipDialer
				? null
				: string.Format("EP {0} SETTINGS RING_ENABLE {1}", ChannelName, dndStateInt);

			if (cmd == null)
			{
				Debug.Console(1, this, "DoNotDisturbToggle: Not implemented for VoIP Dialer");
				return;
			}

			SendText(cmd);
		}

		/// <summary>
		/// Sets the do not disturb state on
		/// </summary>
		public void DoNotDisturbOn()
		{
			var cmd = IsVoipDialer
				? null
				: string.Format("EP {0} SETTINGS RING_ENABLE 0", ChannelName);

			if (cmd == null)
			{
				Debug.Console(1, this, "DoNotDisturbOn: Not implemented for VoIP Dialer");
				return;
			}

			SendText(cmd);
		}

		/// <summary>
		/// Sets the do not disturb state off
		/// </summary>
		public void DoNotDisturbOff()
		{
			var cmd = IsVoipDialer
				? null
				: string.Format("EP {0} SETTINGS RING_ENABLE 1", ChannelName);

			if (cmd == null)
			{
				Debug.Console(1, this, "DoNotDisturbOff: Not implemented for VoIP Dialer");
				return;
			}

			SendText(cmd);
		}

		/// <summary>
		/// Toggles the auto answer state
		/// </summary>
		public void AutoAnswerToggle()
		{
			var autoAnswerStateInt = !AutoAnswerState ? 1 : 0;

			var cmd = IsVoipDialer
				? null
				: string.Format("EP {0} SETTINGS AUTO_ANSWER_RINGS {1}", ChannelName, autoAnswerStateInt);

			if (cmd == null)
			{
				Debug.Console(1, this, "AutoAnswerToggle: Not implemented for VoIP Dialer");
				return;
			}

			SendText(cmd);
		}

		/// <summary>
		/// Sets the auto answer state on
		/// </summary>
		public void AutoAnswerOn()
		{
			var cmd = IsVoipDialer
				? null
				: string.Format("EP {0} SETTINGS AUTO_ANSWER_RINGS 1", ChannelName);

			if (cmd == null)
			{
				Debug.Console(1, this, "AutoAnswerOn: Not implemented for VoIP Dialer");
				return;
			}

			SendText(cmd);
		}

		/// <summary>
		/// Sets the auto answer state off
		/// </summary>
		public void AutoAnswerOff()
		{
			var cmd = IsVoipDialer
				? null
				: string.Format("EP {0} SETTINGS AUTO_ANSWER_RINGS 0", ChannelName);

			if (cmd == null)
			{
				Debug.Console(1, this, "AutoAnswerOff: Not implemented for VoIP Dialer");
				return;
			}

			SendText(cmd);
		}

		/// <summary>
		/// Toggles the hook state
		/// </summary>
		public void Dial()
		{
			if (OffHook)
			{
				EndAllCalls();
				return;
			}

			var cmd = IsVoipDialer
				? string.Format("EP UA {0} KEY KEY_CALL {1}", ChannelName, DialString)
				: string.Format("EP {0} KEY KEY_CALL {1}", ChannelName, DialString);

			SendText(cmd);
		}

		/// <summary>
		/// Dial overload
		/// Dials the number provided
		/// </summary>
		/// <param name="number">Number to dial</param>
		public void Dial(string number)
		{
			if (string.IsNullOrEmpty(number))
				return;

			if (OffHook)
			{
				EndAllCalls();
				return;
			}

			var cmd = IsVoipDialer
				? string.Format("EP UA {0} KEY KEY_CALL {1}", ChannelName, number)
				: string.Format("EP {0} KEY KEY_CALL {1}", ChannelName, number);

			SendText(cmd);
		}

		/// <summary>
		/// Ends the current call with the provided Id
		/// </summary>		
		/// <param name="item">Use null as the parameter, use of CodecActiveCallItem is not implemented</param>
		public void EndCall(CodecActiveCallItem item)
		{
			var cmd = IsVoipDialer
				? string.Format("EP UA {0} KEY KEY_HOOK 0", ChannelName)
				: string.Format("EP {0} KEY KEY_HOOK 0", ChannelName);

			SendText(cmd);
		}

		/// <summary>
		/// Ends all connectted calls
		/// </summary>
		public void EndAllCalls()
		{
			var cmd = IsVoipDialer
				? string.Format("EP {0} KEY KEY_HOOK 0", ChannelName)
				: string.Format("EP UA {0} KEY KEY_HOOK 0", ChannelName);

			SendText(cmd);
		}

		/// <summary>
		/// Accepts incoming call
		/// </summary>
		public void AcceptCall()
		{
			IncomingCall = false;

			var cmd = IsVoipDialer
				? string.Format("EP UA {0} KEY KEY_HOOK 1", ChannelName)
				: string.Format("EP {0} KEY KEY_HOOK 1", ChannelName);

			SendText(cmd);
		}

		/// <summary>
		/// Accepts the incoming call overload
		/// </summary>
		/// <param name="item">Use "", use of CodecActiveCallItem is not implemented</param>
		public void AcceptCall(CodecActiveCallItem item)
		{
			IncomingCall = false;

			var cmd = IsVoipDialer
				? string.Format("EP UA {0} KEY KEY_HOOK 1", ChannelName)
				: string.Format("EP {0} KEY KEY_HOOK 1", ChannelName);

			SendText(cmd);
		}

		/// <summary>
		/// Rejects the incoming call
		/// </summary>
		public void RejectCall()
		{
			IncomingCall = false;

			var cmd = IsVoipDialer
				? string.Format("EP UA {0} KEY KEY_HOOK 0", ChannelName)
				: string.Format("EP {0} KEY KEY_REJECT 1", ChannelName);

			SendText(cmd);
		}

		/// <summary>
		/// Rejects the incoming call overload
		/// </summary>
		/// <param name="item"></param>
		public void RejectCall(CodecActiveCallItem item)
		{
			IncomingCall = false;

			var cmd = IsVoipDialer
				? string.Format("EP UA {0} KEY KEY_HOOK 0", ChannelName)
				: string.Format("EP {0} KEY KEY_REJECT 1", ChannelName);

			SendText(cmd);
		}

		/// <summary>
		/// Get Hook state 
		/// </summary>
		public void GetHookState()
		{
			var cmd = IsVoipDialer
				? null
				: string.Format("EP {0} INQUIRE HOOK", ChannelName);

			if (cmd == null)
			{
				Debug.Console(1, this, "GetHookState: Not implemented for VoIP Dialer");
				return;
			}

			SendText(cmd);
		}

		/// <summary>
		/// Sends the DTMF tone of the keypad digit pressed
		/// </summary>
		/// <param name="digit">keypad digit pressed as a string</param>
		public void SendDtmf(string digit)
		{
			Debug.Console(2, this, "SendDtmf: {0}", digit);

			var keypadTag = EKeypadKeys.Clear;
			// Debug.Console(2, "DIaler {0} SendKeypad {1}", this.ke);
			switch (digit)
			{
				case "0":
					keypadTag = EKeypadKeys.Num0;
					break;
				case "1":
					keypadTag = EKeypadKeys.Num1;
					break;
				case "2":
					keypadTag = EKeypadKeys.Num2;
					break;
				case "3":
					keypadTag = EKeypadKeys.Num3;
					break;
				case "4":
					keypadTag = EKeypadKeys.Num4;
					break;
				case "5":
					keypadTag = EKeypadKeys.Num5;
					break;
				case "6":
					keypadTag = EKeypadKeys.Num6;
					break;
				case "7":
					keypadTag = EKeypadKeys.Num7;
					break;
				case "8":
					keypadTag = EKeypadKeys.Num8;
					break;
				case "9":
					keypadTag = EKeypadKeys.Num9;
					break;
				case "#":
					keypadTag = EKeypadKeys.Pound;
					break;
				case "*":
					keypadTag = EKeypadKeys.Star;
					break;
			}

			if (keypadTag == EKeypadKeys.Clear) return;

			SendKeypad(keypadTag);
		}

		/// <summary>
		/// Sends the pressed keypad number
		/// </summary>
		/// <param name="button">Button pressed</param>
		public void SendKeypad(EKeypadKeys button)
		{
			Debug.Console(2, this, "SendKeypad: {0}", button);

			string keypadTag = null;
			// Debug.Console(_debugVersbose, "DIaler {0} SendKeypad {1}", this.ke);
			switch (button)
			{
				case EKeypadKeys.Num0: keypadTag = "0"; break;
				case EKeypadKeys.Num1: keypadTag = "1"; break;
				case EKeypadKeys.Num2: keypadTag = "2"; break;
				case EKeypadKeys.Num3: keypadTag = "3"; break;
				case EKeypadKeys.Num4: keypadTag = "4"; break;
				case EKeypadKeys.Num5: keypadTag = "5"; break;
				case EKeypadKeys.Num6: keypadTag = "6"; break;
				case EKeypadKeys.Num7: keypadTag = "7"; break;
				case EKeypadKeys.Num8: keypadTag = "8"; break;
				case EKeypadKeys.Num9: keypadTag = "9"; break;
				case EKeypadKeys.Pound: keypadTag = "#"; break;
				case EKeypadKeys.Star: keypadTag = "*"; break;
				case EKeypadKeys.Backspace:
					{
						if (DialString.Length > 0)
						{
							DialString = DialString.Remove(DialString.Length - 1, 1);
						}
						break;
					}
				case EKeypadKeys.Clear:
					{
						DialString = String.Empty;
						break;
					}
			}

			if (keypadTag != null && OffHook)
			{
				CrestronInvoke.BeginInvoke(b =>
				{
					var cmdToSend = IsVoipDialer
						? string.Format("EP UA {0} KEY KEY_DIGIT_PRESSED {1}", ChannelName, keypadTag)
						: string.Format("EP {0} KEY KEY_DIGIT_PRESSED {1}", ChannelName, keypadTag);
					SendText(cmdToSend);

					Thread.Sleep(500);

					cmdToSend = IsVoipDialer
						? string.Format("EP UA {0} KEY KEY_DIGIT_RELEASED {1}", ChannelName, keypadTag)
						: string.Format("EP {0} KEY KEY_DIGIT_RELEASED {1}", ChannelName, keypadTag);
					SendText(cmdToSend);

					PollKeypad();
				});
			}
			else if (keypadTag != null && !OffHook)
			{
				DialString = DialString + keypadTag;
			}
		}

		private void PollKeypad()
		{
			Thread.Sleep(50);
			var cmd = IsVoipDialer
				? string.Format("EP UA {0} INQUIRE DIGITS_DIALED_SINCE_OFF_HOOK", ChannelName)
				: string.Format("EP {0} INQUIRE DIGITS_DIALED_SINCE_OFF_HOOK", ChannelName);
			SendText(cmd);
		}

		/// <summary>
		/// Keypad digits pressed enum
		/// </summary>
		public enum EKeypadKeys
		{
			Num0,
			Num1,
			Num2,
			Num3,
			Num4,
			Num5,
			Num6,
			Num7,
			Num8,
			Num9,
			Star,
			Pound,
			Clear,
			Backspace
		}
	}
}