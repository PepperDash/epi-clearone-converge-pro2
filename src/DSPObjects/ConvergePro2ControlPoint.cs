using PepperDash.Essentials.Devices.Common.DSP;

namespace ConvergePro2DspPlugin
{
	public class ConvergePro2DspControlPoint : DspControlPoint
	{
		/// <summary>
		/// Control point key
		/// </summary>
		public string Key { get; protected set; }

		/// <summary>
		/// Control point friendly label
		/// </summary>
		public string Label { get; set; }

		/// <summary>
		/// Control point channel name
		/// </summary>
		/// <remarks>
		/// Channel name is a combination of the Endpiont Type (EPT), Endpoint Number (EPN), and Block Number (BN)
		/// ex. "(EPT) (EPN) (BN)"
		/// "MIC 101 LEVEL"
		/// "PROC 203 LEVEL"		
		/// </remarks>
		public string ChannelName { get; set; }

		/// <summary>
		/// Control point parent device
		/// </summary>
		public ConvergePro2Dsp Parent { get; private set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Parent DSP instance</param>
		protected ConvergePro2DspControlPoint(ConvergePro2Dsp parent)
		{
			Parent = parent;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="label"></param>
		/// <param name="parent">Parent DSP instance</param>
		protected ConvergePro2DspControlPoint(string label, ConvergePro2Dsp parent)
		{
			Label = label;
			Parent = parent;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="label"></param>
		/// <param name="ept">endpoint type</param>
		/// <param name="epn">endpoint number</param>
		/// <param name="bn">block number</param>
		/// <param name="parent">parent dsp device</param>
		protected ConvergePro2DspControlPoint(string label, string ept, string epn, ConvergePro2Dsp parent)
		{
			Label = label;
			ChannelName = string.Format("{0} {1}", ept, epn);
			Parent = parent;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="label"></param>
		/// <param name="channelName">channel name</param>
		/// <param name="parent">parent dsp device</param>
		protected ConvergePro2DspControlPoint(string label, string channelName, ConvergePro2Dsp parent)
		{
			Label = label;
			ChannelName = channelName;
			Parent = parent;
		}

		/// <summary>
		/// Send full command with string array
		/// </summary>
		/// <param name="values"></param>
		public virtual void SendFullCommand(string[] values)
		{
			var cmdToSend = string.Join(" ", values);
			Parent.SendLine(cmdToSend);
		}

		/// <summary>
		/// Send full command with string value
		/// </summary>
		/// <param name="cmdType">EP, RAMP, ROOM, STACK, BOX, BEAM, BEAMREPORT, FACTORYDEFAULT, RESET, VERSION, METERPRESENT</param>		
		/// <param name="values"></param>
		public virtual void SendFullCommand(string cmdType, string[] values)
		{
			var cmdToSend = string.Format("{0} {1}", cmdType, string.Join(" ", values));
			Parent.SendLine(cmdToSend);
		}

		/// <summary>
		/// Send full command with int value
		/// </summary>
		/// <param name="cmdType">EP, RAMP, ROOM, STACK, BOX, BEAM, BEAMREPORT, FACTORYDEFAULT, RESET, VERSION, METERPRESENT</param>		
		/// <param name="channelName">'ept epn bn'</param>
		/// <param name="paramName"></param>
		/// <param name="value">string value</param>
		public virtual void SendFullCommand(string cmdType, string channelName, string paramName, string value)
		{
			var cmdToSend = string.Format("{0} {1} {2} {3}", cmdType, channelName, paramName, value);
			Parent.SendLine(cmdToSend);
		}

		/// <summary>
		/// Send full command with int value
		/// </summary>
		/// <param name="cmdType">EP, RAMP, ROOM, STACK, BOX, BEAM, BEAMREPORT, FACTORYDEFAULT, RESET, VERSION, METERPRESENT</param>		
		/// <param name="channelName">'ept epn bn'</param>
		/// <param name="paramName"></param>
		/// <param name="value">integer value</param>
		public virtual void SendFullCommand(string cmdType, string channelName, string paramName, int value)
		{
			var cmdToSend = string.Format("{0} {1} {2} {3}", cmdType, channelName, paramName, value);
			Parent.SendLine(cmdToSend);
		}
	}
}