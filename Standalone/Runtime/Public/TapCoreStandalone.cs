using TapSDK.Core.Internal;
using UnityEngine;
using TapSDK.Core.Standalone.Internal;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using TapSDK.Core.Internal.Utils;
using TapSDK.Core.Standalone.Internal.Openlog;

namespace TapSDK.Core.Standalone
{
    /// <summary>
    /// Represents the standalone implementation of the TapCore SDK.
    /// </summary>
    public class TapCoreStandalone : ITapCorePlatform
    {
        internal static Prefs Prefs;
        internal static Tracker Tracker;
        internal static User User;
        internal static TapTapSdkOptions coreOptions;
        public static bool isRnd = false;
        internal static bool enableAutoEvent = true;

        internal static Dictionary<string, object> gatekeeperData = new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TapCoreStandalone"/> class.
        /// </summary>
        public TapCoreStandalone()
        {
            Debug.Log("TapCoStandalone constructor");
            // Instantiate modules
            Prefs = new Prefs();
            Tracker = new Tracker();
            User = new User();
            TapLoom.Initialize();
        }

        private static void SetRND(bool isRnd)
        {
            Debug.Log("SetRND called" + isRnd);
            TapCoreStandalone.isRnd = isRnd;
        }

        /// <summary>
        /// Initializes the TapCore SDK with the specified options.
        /// </summary>
        /// <param name="options">The TapCore SDK options.</param>
        public void Init(TapTapSdkOptions options)
        {
            Init(options, null);
        }

        /// <summary>
        /// Initializes the TapCore SDK with the specified core options and additional options.
        /// </summary>
        /// <param name="coreOption">The TapCore SDK core options.</param>
        /// <param name="otherOptions">Additional TapCore SDK options.</param>
        public void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
            Debug.Log("SDK inited with other options + " + coreOption.ToString() + coreOption.ToString());
            coreOptions = coreOption;

            var path = Path.Combine(Application.persistentDataPath, Constants.ClientSettingsFileName);
            if (File.Exists(path))
            {
                var clientSettings = File.ReadAllText(path);
                Debug.Log("本地 clientSettings: " + clientSettings);
                SetAutoEvent(Json.Deserialize(clientSettings) as Dictionary<string, object>);

                try
                {
                    gatekeeperData = Json.Deserialize(clientSettings) as Dictionary<string, object>;
                }
                catch (System.Exception e)
                {
                    Debug.LogError("TriggerEvent error: " + e.Message);
                }
            }

            Tracker.Init();

            TapOpenlogStandalone.Init();

            requestClientSetting();
        }

        public void UpdateLanguage(TapTapLanguageType language)
        {
            if (coreOptions == null)
            {
                Debug.Log("coreOptions is null");
                return;
            }
            Debug.Log("UpdateLanguage called with language: " + language);
            coreOptions.preferredLanguage = language;
        }

       public static string getGatekeeperConfigUrl(string key)
               {
                   if (gatekeeperData != null)
                   {
                       var urlsData = gatekeeperData["urls"] as Dictionary<string, object>;
                       if (urlsData != null && urlsData.ContainsKey(key))
                       {
                           var keyData = urlsData[key] as Dictionary<string, object>;
                           if (keyData != null)
                           {
                               return (string)keyData["browser"];
                           }
                       }
                   }
                   return null;
               }

        private async void requestClientSetting()
        {
            // 使用 httpclient 请求 /sdk-core/v1/gatekeeper 获取配置
            HttpClientConfig config = new HttpClientConfig(Constants.TAPSDK_HOST, null, true);
            var httpClient = new HttpClient(config);
#if UNITY_EDITOR
            var bundleIdentifier = PlayerSettings.applicationIdentifier;
#else
            var bundleIdentifier = Application.identifier;
#endif
            var path = $"sdk-core/v1/gatekeeper?client_id={coreOptions.clientId}";
            var body = new Dictionary<string, object> {
                { "platform", "pc" },
                { "bundle_id", bundleIdentifier }
            };
            var response = await httpClient.Post(path, body, headers: NetUtils.commonHeaders);
            if (response != null)
            {
                var responseJson = Json.Deserialize(response) as Dictionary<string, object>;
                if (responseJson != null)
                {
                    /**
                    * {
                    *   "data": {
                    *     "check": {},
                    *     "switch": {
                    *       "auto_event": true,
                    *       "heartbeat": true
                    *     },
                    *     "urls": {
                    *       "achievement_my_list_url": {
                    *         "webview": "https://tapsdk.xdrnd.cn/achievement/me?client_id=rfciqabirt4vqav7io",
                    *         "browser": "https://www.xdrnd.cn/app/70253/achievement/6"
                    *       }
                    *     },
                    *     "webview": {
                    *       "js_bridge": {
                    *         "host_allowlist": [
                    *           "tapsdk.xdrnd.cn"
                    *         ]
                    *       }
                    *     }
                    *   },
                    *   "now": 1719972670,
                    *   "success": true
                    * }
                    */
                    var isSuccess = (bool)responseJson["success"];
                    if (isSuccess)
                    {
                        var data = responseJson["data"] as Dictionary<string, object>;
                        SetAutoEvent(data);
                        gatekeeperData = data;
                        // 把 data 存储在本地
                        saveClientSettings(data);
                        // 发通知
                        EventManager.TriggerEvent(Constants.ClientSettingsEventKey, data);
                    }
                }
            }
        }

        private void saveClientSettings(Dictionary<string, object> settings)
        {
            string json = Json.Serialize(settings);
            Debug.Log("saveClientSettings: " + json);
            File.WriteAllText(Path.Combine(Application.persistentDataPath, Constants.ClientSettingsFileName), json);
        }

        private void SetAutoEvent(Dictionary<string, object> data)
        {
            if (data != null)
            {
                var switchData = data["switch"] as Dictionary<string, object>;
                enableAutoEvent = (bool)switchData["auto_event"];
            }
            Debug.Log("SetAutoEvent enableAutoEvent is: " + enableAutoEvent);
        }
    }

    public interface IOpenIDProvider
    {
        string GetOpenID();
    }
}
