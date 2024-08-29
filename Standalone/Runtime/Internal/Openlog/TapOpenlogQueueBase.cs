
#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using TapSDK.Core.Internal.Log;
using UnityEngine;
using Newtonsoft.Json;
using TapSDK.Core.Standalone.Internal.Openlog;
using ProtoBuf;

namespace TapSDK.Core.Standalone.Internal
{
    public abstract class TapOpenlogQueueBase
    {
        private TapLog log;
        private string module;
        private string persistentDataPath = Application.persistentDataPath;
        private Queue<TapOpenlogStoreBean> queue = new Queue<TapOpenlogStoreBean>();
        private TapOpenlogHttpClient httpClient = new TapOpenlogHttpClient();
        private const int MaxEvents = 50;
        private const int MaxBatchSize = 200;
        private const float SendInterval = 15;
        private Timer timer;
        private int queueCount => queue.Count;

        protected abstract string GetUrlPath();
        protected abstract string GetEventFilePath();

        public TapOpenlogQueueBase(string module)
        {
            this.module = module;
            log = new TapLog(module: "Openlog." + module);
            // 加载未发送的事件
            LoadStorageLogs();
            SendEventsAsync();
        }

        public void Enqueue(TapOpenlogStoreBean bean)
        {
            // 将事件添加到队列
            queue.Enqueue(bean);
            SaveEvents();

            // 检查队列大小
            if (queueCount >= MaxEvents)
            {
                log.Log("队列大小超过最大值 = " + queueCount);
                SendEventsAsync();
                log.Log("队列大小超过最大值 end");
            }
            else
            {
                ResetTimer();
            }
        }

        public async void SendEventsAsync()
        {
            if (queueCount == 0)
            {
                return;
            }
            var eventsToSend = new List<TapOpenlogStoreBean>();
            LogGroup logGroup = new LogGroup();
            logGroup.Logs = new List<Log>();
            for (int i = 0; i < MaxBatchSize && queueCount > 0; i++)
            {
                TapOpenlogStoreBean bean = queue.Dequeue();
                eventsToSend.Add(bean);

                Log log = new Log();
                log.Time = (uint)(bean.timestamp / 1000);
                log.Contents = new List<LogContent>();
                foreach (var kvp in bean.props)
                {
                    LogContent logContent = new LogContent
                    {
                        Key = kvp.Key ?? "",
                        Value = kvp.Value ?? ""
                    };
                    log.Contents.Add(logContent);
                }
                logGroup.Logs.Add(log);
            }

            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, logGroup);
                bytes = stream.ToArray();
            }

            var result = await httpClient.Post(GetUrlPath(), content: bytes);
            if (!result)
            {
                foreach (var eventParams in eventsToSend)
                {
                    queue.Enqueue(eventParams);
                }
                SaveEvents();
            }
            else
            {
                log.Log("SendEvents success", JsonConvert.SerializeObject(logGroup));
                SaveEvents();
                if (queueCount > MaxEvents)
                {
                    SendEventsAsync();
                }
            }

        }

        private void OnTimerElapsed(object state)
        {
            timer.Dispose();
            timer = null;
            SendEventsAsync();
        }

        private void ResetTimer()
        {
            if (timer == null)
            {
                // 设置计时器，15秒后触发一次
                timer = new Timer(OnTimerElapsed, null, TimeSpan.FromSeconds(SendInterval), Timeout.InfiniteTimeSpan);
            }
        }

        private void LoadStorageLogs()
        {
            string filePath = Path.Combine(persistentDataPath, GetEventFilePath());
            if (File.Exists(filePath))
            {
                string jsonData = File.ReadAllText(filePath);
                if (string.IsNullOrEmpty(jsonData))
                {
                    return;
                }
                List<TapOpenlogStoreBean> deserializedData;
                try
                {
                    deserializedData = JsonConvert.DeserializeObject<List<TapOpenlogStoreBean>>(jsonData);
                }
                catch (Exception ex)
                {
                    log.Warning($"LoadLogs( FileName : {GetEventFilePath()} ) Exception", ex.ToString());
                    File.Delete(filePath);
                    return;
                }
                if (deserializedData != null && deserializedData.Count > 0)
                {
                    foreach (var item in deserializedData)
                    {
                        queue.Enqueue(item);
                    }
                }
            }
            log.Log("LoadStorageLogs end, count = " + queue.Count, JsonConvert.SerializeObject(queue.ToList()));
        }

        private void SaveEvents()
        {
            try
            {
                if (queue == null)
                {
                    return;
                }

                var eventList = queue.ToList();
                string jsonData = JsonConvert.SerializeObject(eventList);

                if (string.IsNullOrEmpty(GetEventFilePath()))
                {
                    log.Log("EventFilePath is null or empty");
                    return;
                }

                string filePath = Path.Combine(persistentDataPath, GetEventFilePath());
                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }

                File.WriteAllText(filePath, jsonData);
            }
            catch (Exception ex)
            {
                log.Warning("SaveEvents Exception" + ex.Message);
            }
        }
    }
}
#endif