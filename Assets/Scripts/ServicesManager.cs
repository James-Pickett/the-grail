using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SicroMervice;
using UnityEngine;

public class ServicesManager : MonoBehaviour
{
    // Use this for initialization
    private void Start()
    {
        var configFilePath = $"{Application.dataPath}/StreamingAssets/MessageSubscriptions.json";

        if (!File.Exists(path: configFilePath))
        {
            Debug.LogError(message: $"Message Subscription not found at {configFilePath}");
            return;
        }

        var messageSubscriptions = File.ReadAllText(path: configFilePath);

        var testMessage = new Dictionary<string, string> {{"Label", "HelloWorld"}, {"Text", "Hello World!"}};

        Initializer.InitializeSystem(config: messageSubscriptions)
            .QueueMessage(message: JsonConvert.SerializeObject(value: testMessage));
    }
}