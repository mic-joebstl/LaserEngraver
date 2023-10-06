using LaserPathEngraver.Core.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserPathEngraver.Core.Services
{
    public class EngraverService
	{
		private Device? _device;


	}

	public enum EngraverServiceStatus
	{
		Disconnected,
		Connecting,
		Ready,
		Executing,
		Disconnecting
	}

	public class EngraverServiceStateChangedEventArgs : EventArgs
	{
		internal EngraverServiceStateChangedEventArgs(EngraverServiceStatus oldState, EngraverServiceStatus newState)
		{
			OldState = oldState;
			NewState = newState;
		}

		public EngraverServiceStatus OldState { get; private set; }
		public EngraverServiceStatus NewState { get; private set; }
	}

	public delegate void EngraverServiceStateChangedEventHandler(Device sender, EngraverServiceStateChangedEventArgs args);

}

