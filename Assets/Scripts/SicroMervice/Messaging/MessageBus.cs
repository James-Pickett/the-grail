using SicroMervice.Services;
using Debug = UnityEngine.Debug;

namespace SicroMervice.Messaging
{
    public class MessageBus : IMessageBus
    {
        private readonly PropertyToServiceMap _propertyToServiceMap;

        public MessageBus()
        {
            _propertyToServiceMap = new PropertyToServiceMap();
        }

        public void QueueMessage(string message)
        {
            Debug.Log(message: $"Message Queued: {message}");

            foreach (var iService in _propertyToServiceMap.GetObservers(message: message))
            {
                iService.ReceiveMessage(message: message);
            }
        }

        public void AddMapping(PropertyValuePair propertyValuePair, IService iService)
        {
            _propertyToServiceMap.AddMap(propertyValuePair: propertyValuePair, iService: iService);
        }
    }
}
