using TapSDK.Core.Internal;
using UnityEngine;
using TapSDK.Core.Standalone.Internal;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using TapSDK.Core.Internal.Utils;
using TapSDK.Core.Standalone.Internal.Openlog;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Standalone.Internal.Http;
using Newtonsoft.Json;
using TapSDK.Core.Standalone.Internal.Bean;

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

        internal static TapGatekeeper gatekeeperData = new TapGatekeeper();

        private readonly TapHttp tapHttp = TapHttp.NewBuilder("TapSDKCore", TapTapSDK.Version).Build();

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
            TapLog.Log("SetRND called = " + isRnd);
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
            TapLog.Log("SDK inited with other options + " + coreOption.ToString() + coreOption.ToString());
            coreOptions = coreOption;

            var path = Path.Combine(Application.persistentDataPath, Constants.ClientSettingsFileName);
            if (File.Exists(path))
            {
                var clientSettings = File.ReadAllText(path);
                TapLog.Log("本地 clientSettings: " + clientSettings);
                try
                {
                    TapGatekeeper tapGatekeeper = JsonConvert.DeserializeObject<TapGatekeeper>(clientSettings);
                    SetAutoEvent(tapGatekeeper);
                    gatekeeperData = tapGatekeeper;
                }
                catch (System.Exception e)
                {
                    TapLog.Warning("TriggerEvent error: " + e.Message);
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
            TapLog.Log("UpdateLanguage called with language: " + language);
            coreOptions.preferredLanguage = language;
        }

        public static string getGatekeeperConfigUrl(string key)
        {
            if (gatekeeperData != null)
            {
                var urlsData = gatekeeperData.Urls;
                if (urlsData != null && urlsData.ContainsKey(key))
                {
                    var keyData = urlsData[key];
                    if (keyData != null)
                    {
                        return keyData.Browser;
                    }
                }
            }
            return null;
        }

        private void requestClientSetting()
        {
            // 使用 httpclient 请求 /sdk-core/v1/gatekeeper 获取配置
#if UNITY_EDITOR
            var bundleIdentifier = PlayerSettings.applicationIdentifier;
#else
            var bundleIdentifier = Application.identifier;
#endif
            var path = "sdk-core/v1/gatekeeper";
            var body = new Dictionary<string, object> {
                { "platform", "pc" },
                { "bundle_id", bundleIdentifier }
            };

            tapHttp.PostJson<TapGatekeeper>(
               url: path,
               json: body,
               onSuccess: (data) =>
               {
                   SetAutoEvent(data);
                   gatekeeperData = data;
                   // 把 data 存储在本地
                   saveClientSettings(data);
                   // 发通知
                   EventManager.TriggerEvent(Constants.ClientSettingsEventKey, data);
               },
               onFailure: (error) =>
               {
                   if (error is TapHttpServerException se)
                   {
                       if (TapHttpErrorConstants.ERROR_INVALID_CLIENT.Equals(se.ErrorData.Error))
                       {
                           TapLog.Error("Init Failed", se.ErrorData.ErrorDescription);
                           TapMessage.ShowMessage(se.ErrorData.Msg, TapMessage.Position.bottom, TapMessage.Time.twoSecond);
                       }
                   }
               }
           );
        }

        private void saveClientSettings(TapGatekeeper settings)
        {
            string json = JsonConvert.SerializeObject(settings);
            Debug.Log("saveClientSettings: " + json);
            File.WriteAllText(Path.Combine(Application.persistentDataPath, Constants.ClientSettingsFileName), json);
        }

        private void SetAutoEvent(TapGatekeeper gatekeeper)
        {
            if (gatekeeper != null)
            {
                var switchData = gatekeeper.Switch;
                if (switchData != null)
                {
                    enableAutoEvent = switchData.AutoEvent;
                }
            }
            Debug.Log("SetAutoEvent enableAutoEvent is: " + enableAutoEvent);
        }
    }

    public interface IOpenIDProvider
    {
        string GetOpenID();
    }
}
