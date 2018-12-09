using System.Runtime.Serialization;

namespace SicroMervice.Messaging
{
    public struct MessageSubscriptionConfiguration
    {
        public ServiceSubscription[] ServiceSubscriptions { get; set; }
    }

    public struct ServiceSubscription
    {
        public PropertyValuePair[] PropertyValuePairs { get; set; }
        public AssemblyTypePair AssemblyTypePair { get; set; }
    }

    public struct PropertyValuePair
    {
        [DataMember(Name = "name")]
        public string Property { get; set; }

        [DataMember(Name = "value")]
        public string Value { get; set; }
    }

    public struct AssemblyTypePair
    {
        public string Assembly { get; set; }
        public string Type { get; set; }
    }
}