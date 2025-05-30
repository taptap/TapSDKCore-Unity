using System;
using System.Collections.Generic;
using TapSDK.UI;

namespace TapSDK.Core.Internal.Utils
{
    public sealed class EventManager : Singleton<EventManager> 
    {
        public const string OnApplicationPause = "OnApplicationPause";
        public const string OnApplicationQuit = "OnApplicationQuit";

        public const string OnComplianceUserChanged = "OnComplianceUserChanged";

        public const string IsLaunchedFromTapTapPCFinished = "IsLaunchedFromTapTapPCFinished";
        private Dictionary<string, Action<object>> eventRegistries = new Dictionary<string, Action<object>>();
        
        public static void AddListener(string eventName, Action<object> listener) {
            if (listener == null) return;
            if (string.IsNullOrEmpty(eventName)) return;
            Action<object> thisEvent;
            if (Instance.eventRegistries.TryGetValue(eventName, out thisEvent)) {
                thisEvent += listener;
                Instance.eventRegistries[eventName] = thisEvent;
            } else {
                thisEvent += listener;
                Instance.eventRegistries.Add(eventName, thisEvent);
            }
        }
        
        public static void RemoveListener(string eventName, Action<object> listener) {
            if (listener == null) return;
            if (string.IsNullOrEmpty(eventName)) return;
            Action<object> thisEvent;
            if (Instance.eventRegistries.TryGetValue(eventName, out thisEvent)) {
                thisEvent -= listener;
                Instance.eventRegistries[eventName] = thisEvent;
            }
        }

        public static void TriggerEvent(string eventName, object message) {
            Action<object> thisEvent = null;
            if (Instance.eventRegistries.TryGetValue(eventName, out thisEvent)) {
                thisEvent.Invoke(message);
            }
        }
    }
}