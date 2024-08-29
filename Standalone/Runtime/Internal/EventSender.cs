
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace TapSDK.Core.Standalone.Internal {
    public class EventSender : MonoBehaviour
    {
        private const string EventFilePath = "events.json";
        private string persistentDataPath = Application.persistentDataPath;
        
        private Queue<Dictionary<string, object>> eventQueue = new Queue<Dictionary<string, object>>();
        private HttpClient httpClient;
        private const int MaxEvents = 50;
        private const int MaxBatchSize = 200;
        private const float SendInterval = 15f;
        private Timer timer;
        private DateTime lastSendTime;

        private int QueueCount => eventQueue.Count;

        public EventSender()
        {
            // 设置计时器
            timer = new Timer(OnTimerElapsed, null, TimeSpan.Zero, TimeSpan.FromSeconds(SendInterval));
            lastSendTime = DateTime.Now;

            // 初始化 HttpClient
            var header = new Dictionary<string, string>
            {
                { "User-Agent", $"{TapTapSDK.SDKPlatform}/{TapTapSDK.Version}" }
            };

            var coreOptions = TapCoreStandalone.coreOptions;
            if(coreOptions.region == TapTapRegionType.CN) {
                HttpClientConfig config = new HttpClientConfig(Constants.SERVER_URL_BJ, header);
                httpClient = new HttpClient(config);
            } else {
                HttpClientConfig config = new HttpClientConfig(Constants.SERVER_URL_SG, header);
                httpClient = new HttpClient(config);
            }

            // 加载未发送的事件
            LoadEvents();
            SendEventsAsync(null);
        }

        public async void SendEventsAsync(Action onSendComplete)
        {
            if (eventQueue.Count == 0)
            {
                onSendComplete?.Invoke();
                return;
            }

            var  eventsToSend = new List<Dictionary<string, object>>();
            for (int i = 0; i < MaxBatchSize && eventQueue.Count > 0; i++)
            {
                eventsToSend.Add(eventQueue.Dequeue());
            }

            var body = new Dictionary<string, object> {
                { "data", eventsToSend }
            };

            var resonse = await httpClient.Post("batch", body);
            
            // 判断是否请求成功
            if (resonse == null)
            {
                // 将事件重新添加到队列
                foreach (var eventParams in eventsToSend)
                {
                    eventQueue.Enqueue(eventParams);
                }
            }
            else
            {
                Debug.Log("Events sent successfully");
            }
            onSendComplete?.Invoke();
            SaveEvents();
        }

        public void Send(Dictionary<string, object> eventParams)
        {
            // 将事件添加到队列
            eventQueue.Enqueue(eventParams);
            SaveEvents();

            // 检查队列大小
            if (QueueCount >= MaxEvents)
            {
                SendEvents();
                ResetTimer();
            }
        }

        private void OnTimerElapsed(object state)
        {
            var offset = (DateTime.Now - lastSendTime).TotalSeconds;
            if (offset >= SendInterval)
            {
                SendEvents();
                ResetTimer();
            }
        }


        private void ResetTimer()
        {
            timer.Change(TimeSpan.FromSeconds(SendInterval), TimeSpan.FromSeconds(SendInterval));
        }

        private void LoadEvents()
        {
            string filePath = Path.Combine(persistentDataPath, EventFilePath);
            if (File.Exists(filePath))
            {
                string jsonData = File.ReadAllText(filePath);
                if (string.IsNullOrEmpty(jsonData))
                {
                    return;
                }
                
                var savedEvents = ConvertToListOfDictionaries(Json.Deserialize(jsonData));
                if (savedEvents == null)
                {
                    return;
                }
                foreach (var eventParams in savedEvents)
                {
                    eventQueue.Enqueue(eventParams);
                }
            }
        }

        private void SaveEvents()
        {
            try
            {
                if (eventQueue == null)
                {
                    return;
                }

                var eventList = eventQueue.ToList();
                string jsonData = Json.Serialize(eventList);

                if (string.IsNullOrEmpty(EventFilePath))
                {
                    Debug.LogError("EventFilePath is null or empty");
                    return;
                }

                string filePath = Path.Combine(persistentDataPath, EventFilePath);

                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }

                File.WriteAllText(filePath, jsonData);
            }
            catch (Exception ex)
            {
                Debug.LogError("SaveEvents Exception - " + ex.Message);
            }
        }

        public void SendEvents()
        {
            SendEventsAsync(() => lastSendTime = DateTime.Now);
        }

        private Dictionary<string, object> ConvertToDictionary(Dictionary<string, object> original)
        {
            var result = new Dictionary<string, object>();
            foreach (var keyValuePair in original)
            {
                if (keyValuePair.Value is Dictionary<string, object> nestedDictionary)
                {
                    result[keyValuePair.Key] = ConvertToDictionary(nestedDictionary);
                }
                else if (keyValuePair.Value is List<object> nestedList)
                {
                    result[keyValuePair.Key] = ConvertToListOfDictionaries(nestedList);
                }
                else
                {
                    result[keyValuePair.Key] = keyValuePair.Value;
                }
            }
            return result;
        }
        private List<Dictionary<string, object>> ConvertToListOfDictionaries(object deserializedData)
        {
            if (deserializedData is List<object> list)
            {
                var result = new List<Dictionary<string, object>>();
                foreach (var item in list)
                {
                    if (item is Dictionary<string, object> dictionary)
                    {
                        result.Add(ConvertToDictionary(dictionary));
                    }
                    else
                    {
                        return null; // 数据格式不匹配
                    }
                }
                return result;
            }
            return null; // 数据格式不匹配
        }

        [Serializable]
        private class Serialization<T>
        {
            public List<T> items;
            public Serialization(List<T> items)
            {
                this.items = items;
            }

            public List<T> ToList()
            {
                return items;
            }
        }
    }
}