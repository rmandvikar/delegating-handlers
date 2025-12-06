namespace rm.DelegatingHandlersTest;

public class TurnDownForWhatException : Exception
{
	public TurnDownForWhatException() { }
	public TurnDownForWhatException(string message) : base(message) { }
	public TurnDownForWhatException(string message, Exception inner) : base(message, inner) { }
}
