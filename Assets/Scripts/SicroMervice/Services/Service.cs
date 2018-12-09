using SicroMervice.Messaging;
using UnityEngine;

namespace SicroMervice.Services
{
    public abstract class Service : IService
    {
        private readonly IMessageBus _iMessageBus;

        protected Service(IMessageBus iMessageBus)
        {
            _iMessageBus = iMessageBus;
            Debug.Log(message: $"{GetType()} Constructed");
        }

        public void ReceiveMessage(string message)
        {
            Debug.Log(message: $"{GetType()} received message: {message}");
            ReceiveMessageImpl(message: message);
        }

        protected abstract void ReceiveMessageImpl(string message);

        protected void QueueMessage(string message)
        {
            _iMessageBus.QueueMessage(message: message);
        }
    }
}
