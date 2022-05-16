namespace rm.DelegatingHandlersTest
{
	[Serializable]
	public class TurnDownForWhatException : Exception
	{
		public TurnDownForWhatException() { }
		public TurnDownForWhatException(string message) : base(message) { }
		public TurnDownForWhatException(string message, Exception inner) : base(message, inner) { }
		protected TurnDownForWhatException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
