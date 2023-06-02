using PepperDash.Essentials.Devices.Common.DSP;

namespace ConvergePro2DspPlugin
{
	public class ConvergePro2DspControlPoint : DspControlPoint
	{
		public string Key { get; protected set; }
		public string Label { get; set; }
		public string EndpointType { get; set; }
		public string EndpointNumber { get; set; }
		public string BlockNumber { get; set; }
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
		protected ConvergePro2DspControlPoint(string label, string ept, string epn, string bn, ConvergePro2Dsp parent)
		{
			Label = label;
			EndpointType = ept;
			EndpointNumber = epn;
			BlockNumber = bn;
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
		/// <param name="controlTag">'ept epn bn'</param>
		/// <param name="paramName"></param>
		/// <param name="value">string value</param>
		public virtual void SendFullCommand(string cmdType, string controlTag, string paramName, string value)
		{
			var cmdToSend = string.Format("{0} {1} {2} {3}", cmdType, controlTag, paramName, value);
			Parent.SendLine(cmdToSend);
		}

		/// <summary>
		/// Send full command with int value
		/// </summary>
		/// <param name="cmdType">EP, RAMP, ROOM, STACK, BOX, BEAM, BEAMREPORT, FACTORYDEFAULT, RESET, VERSION, METERPRESENT</param>		
		/// <param name="controlTag">'ept epn bn'</param>
		/// <param name="paramName"></param>
		/// <param name="value">integer value</param>
		public virtual void SendFullCommand(string cmdType, string controlTag, string paramName, int value)
		{
			var cmdToSend = string.Format("{0} {1} {2} {3}", cmdType, controlTag, paramName, value);
			Parent.SendLine(cmdToSend);
		}

		/// <summary>
		/// Send full command
		/// </summary>
		/// <param name="cmdType">EP, RAMP, ROOM, STACK, BOX, BEAM, BEAMREPORT, FACTORYDEFAULT, RESET, VERSION, METERPRESENT</param>		
		/// <param name="ept">endpoint type</param>
		/// <param name="epn">endpoint number</param>
		/// <param name="bn">block number</param>
		/// <param name="paramName"></param>
		/// <param name="value"></param>
		public virtual void SendFullCommand(string cmdType, string ept, string epn, string bn, string paramName, string value)
		{
			var cmdToSend = string.Format("{0} {1} {2} {3} {4} {5}", cmdType, ept, epn, bn, paramName, value);
			Parent.SendLine(cmdToSend);
		}

		/// <summary>
		/// Send full command
		/// </summary>
		/// <param name="cmdType">EP, RAMP, ROOM, STACK, BOX, BEAM, BEAMREPORT, FACTORYDEFAULT, RESET, VERSION, METERPRESENT</param>		
		/// <param name="ept">endpoint type</param>
		/// <param name="epn">endpoint number</param>
		/// <param name="bn">block number</param>
		/// <param name="paramName"></param>
		/// <param name="value"></param>
		public virtual void SendFullCommand(string cmdType, string ept, string epn, string bn, string paramName, int value)
		{
			var cmdToSend = string.Format("{0} {1} {2} {3} {4} {5}", cmdType, ept, epn, bn, paramName, value);
			Parent.SendLine(cmdToSend);
		}
	}
}