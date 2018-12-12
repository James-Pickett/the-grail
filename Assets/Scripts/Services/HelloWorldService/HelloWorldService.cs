using Newtonsoft.Json;
using SicroMervice.Messaging;
using SicroMervice.Services;
using UnityEngine;

namespace HelloWorldService
{
    public class HelloWorldService : Service
    {
        public HelloWorldService(IMessageBus iMessageBus) : base(iMessageBus: iMessageBus)
        {
        }

        protected override void ReceiveMessageImpl(string message)
        {
            var msg = JsonConvert.DeserializeObject<HelloWorldMessage>(value: message);

            Debug.Log(
                message: $"{GetType()} received a message at {nameof(ReceiveMessageImpl)} method. It said {msg.Text}!");
        }

        private class HelloWorldMessage
        {
            public string Text { get; set; }
        }
    }
}