using Newtonsoft.Json;
using SicroMervice.Messaging;
using SicroMervice.Services;
using Debug = UnityEngine.Debug;

namespace HelloWorldService
{
    public class HelloWorldService : Service {

        private class HelloWorldMessage
        {
            public string Text { get; set; }
        }

        public HelloWorldService(IMessageBus iMessageBus) : base(iMessageBus: iMessageBus)
        {
        }

        protected override void ReceiveMessageImpl(string message)
        {
            var msg = JsonConvert.DeserializeObject<HelloWorldMessage>(message);

            Debug.Log(message: $"{GetType()} received a message at {nameof(ReceiveMessageImpl)} method. It said {msg.Text}!");
        }
    }
}
