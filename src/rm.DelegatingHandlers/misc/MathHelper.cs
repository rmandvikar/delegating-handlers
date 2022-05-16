using System.Collections.Generic;

namespace rm.DelegatingHandlers
{
	public static class MathHelper
	{
		public static T Max<T>(T val1, T val2)
		{
			return Comparer<T>.Default.Compare(val1, val2) >= 0 ? val1 : val2;
		}
	}
}
