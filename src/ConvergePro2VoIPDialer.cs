using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.Codec;

namespace ConvergePro2DspPlugin
{
	/// <summary>
	/// VoIP Dialer (UA)
	/// </summary>
	/// <remarks>
	/// API 
	/// 2.4.22 TELCO_RX - PDF Page 251
	/// 2.4.23 TELCO_TX - PDF Page 267
	/// 2.4.24 UA - PDF Page 269
	/// </remarks>
	public class ConvergePro2VoIPDialer : IHasDialer, IKeyed
	{
		/// <summary>
		/// Dialer Key
		/// </summary>
		public string Key { get; protected set; }

		/// <summary>
		/// Parent DSP
		/// </summary>
		public ConvergePro2Dsp Parent { get; private set; }

		private ConvergePro2DspDialerConfig Config { get; set; }

		public string Label { get; private set; }
		public bool ClearOnHangup { get; private set; }
		public string ChannelName { get; private set; }
		public string EndpointType { get; private set; }
		public string EndpointNumber { get; private set; }

		/// <summary>
		/// Tracks in call state
		/// </summary>
		public bool IsInCall { get; private set; }


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

		public event EventHandler<CodecCallStatusItemChangeEventArgs> CallStatusChange;

		public void OnCallStatusChange(CodecCallStatusItemChangeEventArgs args)
		{
			var handler = CallStatusChange;
			if (handler == null) return;
			CallStatusChange(this, args);
		}


		public ConvergePro2VoIPDialer(string key, ConvergePro2DspDialerConfig config, ConvergePro2Dsp parent)
		{
			Parent = parent;
			Key = string.Format("{0}-{1}", Parent.Key, key);
			Config = config;

			Initialize(Key, Config);
		}

		/// <summary>
		/// Initializes voip dialer
		/// </summary>
		/// <param name="key"></param>
		/// <param name="config"></param>
		public void Initialize(string key, ConvergePro2DspDialerConfig config)
		{
			DeviceManager.AddDevice(this);

			SubscribeToNotifications();
		}

		/// <summary>
		/// Subscribes to asynchronous notifications regarding VoIP status
		/// </summary>
		/// <remarks>
		/// Page 275: EP-UA (20) Notification (4)
		/// </remarks>
		public void SubscribeToNotifications()
		{
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
				var cmd = string.Format("EP UA {0} NOTIFCATION {1}", Config.ChannelName, notification);
				Parent.SendText(cmd);	
			}
		}

		public void Dial(string number)
		{
			var cmd = string.Format("EP UA {0} KEY KEY_CALL {1}", Config.ChannelName, number);
			Parent.SendText(cmd);
		}

		public void EndCall(CodecActiveCallItem activeCall)
		{
			var cmd = string.Format("EP UA {0} KEY KEY_HOOK 0", Config.ChannelName);
			Parent.SendText(cmd);
		}

		public void EndAllCalls()
		{
			var cmd = string.Format("EP UA {0} KEY KEY_HOOK 0", Config.ChannelName);
			Parent.SendText(cmd);
		}

		public void AcceptCall(CodecActiveCallItem item)
		{
			var cmd = string.Format("EP UA {0} KEY KEY_HOOK 1", Config.ChannelName);
			Parent.SendText(cmd);
		}

		public void RejectCall(CodecActiveCallItem item)
		{
			var cmd = string.Format("EP UA {0} KEY KEY_REJECT 1", Config.ChannelName);
			Parent.SendText(cmd);
		}

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
					Parent.SendText(cmdToSend);

					Thread.Sleep(250);

					cmdToSend = string.Format("EP {0} KEY KEY_DIGIT_RELEASED {1}", ChannelName, keypadTag);
					Parent.SendText(cmdToSend);

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
			Parent.SendText(string.Format("EP {0} INQUIRE DIGITS_DIALED_SINCE_OFF_HOOK", ChannelName));
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