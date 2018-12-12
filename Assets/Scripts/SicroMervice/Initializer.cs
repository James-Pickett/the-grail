using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using SicroMervice.Messaging;
using SicroMervice.Services;
using UnityEngine;

namespace SicroMervice
{
    public class Initializer
    {
        private readonly IMessageBus _iMessageBus;

        public static IMessageBus InitializeSystem(string config)
        {
            var messageBus = new MessageBus();
            var subscriptionConfiguration =
                JsonConvert.DeserializeObject<MessageSubscriptionConfiguration>(value: config);
            var servicesDict = new Dictionary<string, IService>();

            foreach (var messageSubscription in subscriptionConfiguration.ServiceSubscriptions)
            {
                var iService = GetOrAddService(messageSubscription: messageSubscription, servicesDict: servicesDict,
                    messageBus: messageBus);

                foreach (var propertyValuePair in messageSubscription.PropertyValuePairs)
                {
                    messageBus.AddMapping(propertyValuePair: propertyValuePair, iService: iService);
                }
            }

            return messageBus;
        }

        private static IService GetOrAddService(ServiceSubscription messageSubscription,
            Dictionary<string, IService> servicesDict, MessageBus messageBus)
        {
            IService iService;

            var assemblyName = messageSubscription.AssemblyTypePair.Assembly;
            var typeName = messageSubscription.AssemblyTypePair.Type;

            var assemblyTypeNameCombo = $"{assemblyName}.{typeName}";

            if (!servicesDict.ContainsKey(key: assemblyTypeNameCombo))
            {
                var assembly = Assembly.Load(assemblyString: assemblyName);

                if (assembly == null)
                {
                    Debug.LogError(
                        message: $"Assembly identified in configuration, {assemblyName}, could not be loaded");
                    return null;
                }

                var type = assembly.GetType(name: assemblyTypeNameCombo);

                if (type == null)
                {
                    Debug.LogError(
                        message:
                        $"Service type identified in configuration, {typeName}, could not be resolved to as a {nameof(Type)}");
                    return null;
                }

                iService = Activator.CreateInstance(type, messageBus) as IService;

                if (iService == null)
                {
                    Debug.LogError(
                        message:
                        $"Service identified in configuration as {typeName} was resolved to type {type} but is not type of {typeof(IService)}");
                    return null;
                }

                servicesDict.Add(key: assemblyTypeNameCombo, value: iService);
            }
            else
            {
                Debug.LogWarning(
                    message: $"Attempted to create duplicate service [Assembly] {assemblyName} [Type] {typeName}");
            }

            iService = servicesDict[key: assemblyTypeNameCombo];
            return iService;
        }
    }
}