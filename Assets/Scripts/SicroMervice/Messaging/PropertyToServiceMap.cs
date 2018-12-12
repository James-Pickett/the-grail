using System.Collections.Generic;
using Newtonsoft.Json;
using SicroMervice.Services;
using UnityEngine;

namespace SicroMervice.Messaging
{
    public class PropertyToServiceMap : Dictionary<PropertyValuePair, ISet<IService>>
    {
        public void AddMap(PropertyValuePair propertyValuePair, IService iService)
        {
            if (!ContainsKey(key: propertyValuePair))
            {
                Add(key: propertyValuePair, value: new HashSet<IService>());
            }

            if (this[key: propertyValuePair].Contains(item: iService))
            {
                Debug.LogWarning(
                    message:
                    $"Attempted to add duplicate map [Property] {propertyValuePair.Property} [Value] {propertyValuePair.Value} [Service] {iService.GetType()}");
            }

            this[key: propertyValuePair].Add(item: iService);
        }

        public IEnumerable<IService> GetObservers(string message)
        {
            var properties = JsonConvert.DeserializeObject<Dictionary<string, string>>(value: message);

            var iServices = new HashSet<IService>();

            foreach (var property in properties)
            {
                var propertyValuePair = new PropertyValuePair {Property = property.Key, Value = property.Value};

                if (!ContainsKey(key: propertyValuePair))
                {
                    continue;
                }

                foreach (var iService in this[key: propertyValuePair])
                {
                    iServices.Add(item: iService);
                }
            }

            return iServices;
        }
    }
}