using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.Codec;

namespace ConvergePro2DspPlugin
{
	public class ConvergePro2DspDialer : IHasDialer, IKeyed
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
		public bool ClearOnHangup { get; private set; }
		public string ChannelName { get; private set; }
		public string EndpointType { get; private set; }
		public string EndpointNumber { get; private set; }

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
		public ConvergePro2DspDialer(string key, ConvergePro2DspDialerConfig config, ConvergePro2Dsp parent)
		{
			Key = key;

			//if (parent == null)
			//{
			//    Debug.Console(0, this, "Constructor: parent is null or empty");
			//    return;
			//}			
			Parent = parent;

			//if (config == null)
			//{
			//    Debug.Console(0, this, "Constructor: config is null or empty");
			//    return;
			//}
			Label = config.Label;

			if (string.IsNullOrEmpty(config.ChannelName))
			{
				EndpointType = config.EndpointType;
				EndpointNumber = config.EndpointNumber;

				IsVoipDialer = EndpointType.ToLower().Contains("voip");

				ChannelName = string.Format("{0} {1}", EndpointType, EndpointNumber);
			}
			else
			{
				ChannelName = config.ChannelName;
			}

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

		public void Initialize(string key, ConvergePro2DspDialerConfig config)
		{
			Key = string.Format("{0}-{1}", Parent.Key, key);

			DeviceManager.AddDevice(this);
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
		/// <param name="command"></param>
		/// <param name="values"></param>
		public void ParseResponse(string command, string[] values)
		{
			Debug.Console(1, this, "Parsing response {0} values: '{1}'", command, string.Join(", ", values));
			switch (command)
			{
				case "AUTO_ANSWER_RINGS":
					{
						break;
					}
				case "AUTO_DISCONNECT_MODE":
					{
						break;
					}
				case "KEY_CALL":
					{
						break;
					}
				case "KEY_HOOK_FLASH":
					{
						break;
					}
				case "KEY_REDIAL":
					{
						break;
					}
				case "INCOMING_CALL":
					{
						IncomingCall = values[0] == "1";
						break;
					}
				case "CALLER_ID":
					{
						break;
					}
				case "HOOK":
					{
						OffHook = values[0] == "1";
						break;
					}
				case "RING":
					{
						break;
					}
				case "LOCAL_NUMBER":
					{
						LocalNumber = values[0];
						break;
					}
				default:
					{
						Debug.Console(2, this, "ResponseRecieved: unhandled response '{0} {1}'", command, values.ToString());
						break;
					}
			}
		}

		/// <summary>
		/// Subscription method
		/// </summary>
		/*		
		public void Subscribe()
		{
			try
			{
				// Do subscriptions and blah blah
				// This would be better using reflection JTA 2018-08-28
				//PropertyInfo[] properties = Tags.GetType().GetCType().GetProperties();
				var properties = Tags.GetType().GetCType().GetProperties();
				//GetPropertyValues(Tags);

				Debug.Console(2, "QscDspDialer Subscribe");
				foreach (var prop in properties)
				{
					if (prop.Name.Contains("Tag") && !prop.Name.ToLower().Contains("keypad"))
					{
						var propValue = prop.GetValue(Tags, null) as string;
						Debug.Console(2, "Property {0}, {1}, {2}\n", prop.GetType().Name, prop.Name, propValue);
						SendSubscriptionCommand(propValue);
					}
				}
			}
			catch (Exception e)
			{
				Debug.Console(2, "QscDspDialer Subscription Error: '{0}'\n", e);
			}

			// SendSubscriptionCommand(, "1");
			// SendSubscriptionCommand(config. , "mute", 500);
		}
		*/

		/// <summary>
		/// Toggles the do not disturb state
		/// </summary>
		public void DoNotDisturbToggle()
		{
			var dndStateInt = !DoNotDisturbState ? 1 : 0;
			Parent.SendLine(string.Format("EP {0} SETTINGS RING_ENABLE {1}", ChannelName, dndStateInt));
		}

		/// <summary>
		/// Sets the do not disturb state on
		/// </summary>
		public void DoNotDisturbOn()
		{
			Parent.SendLine(string.Format("EP {0} SETTINGS RING_ENABLE 0", ChannelName));
		}

		/// <summary>
		/// Sets the do not disturb state off
		/// </summary>
		public void DoNotDisturbOff()
		{
			Parent.SendLine(string.Format("EP {0} SETTINGS RING_ENABLE 1", ChannelName));
		}

		/// <summary>
		/// Toggles the auto answer state
		/// </summary>
		public void AutoAnswerToggle()
		{
			var autoAnswerStateInt = !AutoAnswerState ? 1 : 0;
			Parent.SendLine(string.Format("EP {0} SETTINGS AUTO_ANSWER_RINGS {1}",
				ChannelName, autoAnswerStateInt));
		}

		/// <summary>
		/// Sets the auto answer state on
		/// </summary>
		public void AutoAnswerOn()
		{
			Parent.SendLine(string.Format("EP {0} SETTINGS AUTO_ANSWER_RINGS 1",
				ChannelName));
		}

		/// <summary>
		/// Sets the auto answer state off
		/// </summary>
		public void AutoAnswerOff()
		{
			Parent.SendLine(string.Format("EP {0} SETTINGS AUTO_ANSWER_RINGS 0",
				ChannelName));
		}

		private void PollKeypad()
		{
			Thread.Sleep(50);
			Parent.SendLine(string.Format("EP {0} INQUIRE DIGITS_DIALED_SINCE_OFF_HOOK", ChannelName));
		}

		/// <summary>
		/// Sends the pressed keypad number
		/// </summary>
		/// <param name="button">Button pressed</param>
		public void SendKeypad(EKeypadKeys button)
		{
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
					var cmdToSend = string.Format("EP {0} KEY KEY_DIGIT_PRESSED {1}", ChannelName, keypadTag);
					Parent.SendLine(cmdToSend);

					Thread.Sleep(500);

					cmdToSend = string.Format("EP {0} KEY KEY_DIGIT_RELEASED {1}", ChannelName, keypadTag);
					Parent.SendLine(cmdToSend);

					PollKeypad();
				});
			}
			else if (keypadTag != null && !OffHook)
			{
				DialString = DialString + keypadTag;
			}
		}

		/// <summary>
		/// Toggles the hook state
		/// </summary>
		public void Dial()
		{
			if (OffHook) EndAllCalls();
			else
			{
				Parent.SendLine(string.Format("EP {0} KEY KEY_CALL {1}", ChannelName, DialString));
			}
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
			}
			Parent.SendLine(string.Format("EP {0} KEY KEY_CALL {1}", ChannelName, number));
		}

		/// <summary>
		/// Ends the current call with the provided Id
		/// </summary>		
		/// <param name="item">Use null as the parameter, use of CodecActiveCallItem is not implemented</param>
		public void EndCall(CodecActiveCallItem item)
		{
			Parent.SendLine(string.Format("EP {0} KEY KEY_HOOK 0", ChannelName));
		}
		/// <summary>
		/// Get Hook state 
		/// </summary>
		public void GetHookState()
		{
			Parent.SendLine(string.Format("EP {0} INQUIRE HOOK", ChannelName));
		}
		/// <summary>
		/// Ends all connectted calls
		/// </summary>
		public void EndAllCalls()
		{
			Parent.SendLine(string.Format("EP {0} KEY KEY_HOOK 0", ChannelName));
		}

		/// <summary>
		/// Accepts incoming call
		/// </summary>
		public void AcceptCall()
		{
			IncomingCall = false;
			Parent.SendLine(string.Format("EP {0} KEY KEY_HOOK 1", ChannelName));
		}

		/// <summary>
		/// Accepts the incoming call overload
		/// </summary>
		/// <param name="item">Use "", use of CodecActiveCallItem is not implemented</param>
		public void AcceptCall(CodecActiveCallItem item)
		{
			IncomingCall = false;
			Parent.SendLine(string.Format("EP {0} KEY KEY_HOOK 1", ChannelName));
		}

		/// <summary>
		/// Rejects the incoming call
		/// </summary>
		public void RejectCall()
		{
			IncomingCall = false;
			Parent.SendLine(string.Format("EP {0} KEY KEY_HOOK 0", ChannelName));
		}

		/// <summary>
		/// Rejects the incoming call overload
		/// </summary>
		/// <param name="item"></param>
		public void RejectCall(CodecActiveCallItem item)
		{
			IncomingCall = false;
			Parent.SendLine(string.Format("EP {0} KEY KEY_HOOK 0", ChannelName));
		}

		/// <summary>
		/// Sends the DTMF tone of the keypad digit pressed
		/// </summary>
		/// <param name="digit">keypad digit pressed as a string</param>
		public void SendDtmf(string digit)
		{
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