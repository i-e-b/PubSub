namespace SevenDigital.Messaging.Integration.Tests.Messages
{
	public interface IComicBookCharacterMessage : IMessage
	{
		string Text { get; }
	}
}