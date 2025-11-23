using ApiAggregatorService.Services.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiAggregatorService.Tests.TestHelpers
{
	public class FakeTracker : ApiPerformanceTracker
	{
		public FakeTracker() : base(null!) { }

		public new Task<T> TrackAsync<T>(string apiName, Func<Task<T>> action)
		{
			return action();
		}
	}

}
