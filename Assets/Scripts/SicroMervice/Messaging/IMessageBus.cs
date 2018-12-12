namespace SicroMervice.Messaging
{
    public interface IMessageBus
    {
        void QueueMessage(string message);
    }
}