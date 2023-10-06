using LaserPathEngraver.Core.Configurations;
using LaserPathEngraver.Core.Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserPathEngraver.Core.Jobs
{
	public interface IEngravePoint
	{
		public int X { get; set; }
		public int Y { get; set; }
		public bool IsVisited { get; set; }
		public byte Intensity { get; set; }
	}

	public sealed class EngraveJob : Job, IPausableJob
	{
		private bool _signalPause;
		private BurnConfiguration _configuration;
		private IEnumerable<IEngravePoint> _source;

		public EngraveJob(BurnConfiguration configuration, IEnumerable<IEngravePoint> source)
		{
			_configuration = configuration;
			_source = source;
		}

		public override string Title => Resources.Localization.Texts.EngraveJobTitle;

		public void Pause()
		{
			_signalPause = true;
		}

		protected override async Task ExecuteCoreAsync(Device device, CancellationToken cancellationToken)
		{
			_signalPause = false;

			if (_signalPause)
			{
				Status = JobStatus.Paused;
				return;
			}

			throw new NotImplementedException(_configuration.PlottingMode.ToString());
		}
	}
}
