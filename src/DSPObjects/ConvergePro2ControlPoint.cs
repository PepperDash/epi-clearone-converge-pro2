using PepperDash.Core;
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
		/// Control point label
		/// </summary>
		public string Label { get; set; }

		public string ChannelName { get; set; }
		public string BlockName { get; set; }
		public string LevelParameter { get; set; }
		public string MuteParameter { get; set; }

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
		/// <param name="config"></param>
		/// <param name="parent">parent dsp device</param>
		protected ConvergePro2DspControlPoint(ConvergePro2DspLevelControlBlockConfig config, ConvergePro2Dsp parent)
		{
			Label = config.Label;
			ChannelName = config.ChannelName;
			BlockName = config.BlockName;
			LevelParameter = config.LevelParameter;
			MuteParameter = config.MuteParameter;
			Parent = parent;
		}

		/// <summary>
		/// Send full command with string array
		/// </summary>
		/// <param name="cmd"></param>
		public virtual void SendText(string cmd)
		{
			Parent.SendText(cmd);
		}
	}
}