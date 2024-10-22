
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TapSDK.Core.Internal.Log;
using UnityEngine;

namespace TapSDK.Core.Standalone.Internal.Openlog
{
    public class TapOpenlogStandalone
    {
        public static string openid = "";

        private static Dictionary<string, string> generalParameter = new Dictionary<string, string>();
        private static string sessionID = Guid.NewGuid().ToString();
#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
        private static TapOpenlogQueueBusiness businessQueue;
        private static TapOpenlogQueueTechnology technologyQueue;
#endif
        private string sdkProjectName;
        private string sdkProjectVersion;

        private static TapLog log = new TapLog(module: "Openlog");
        public static void Init()
        {
#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
            businessQueue = new TapOpenlogQueueBusiness();
            technologyQueue = new TapOpenlogQueueTechnology();
#endif
            InitGeneralParameter();
        }

        public TapOpenlogStandalone(string sdkProjectName, string sdkProjectVersion)
        {
            this.sdkProjectName = sdkProjectName;
            this.sdkProjectVersion = sdkProjectVersion;
        }

        public void LogBusiness(
            string action,
            Dictionary<string, string> properties = null
        )
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string tLogId = Guid.NewGuid().ToString();

            if (properties == null)
            {
                properties = new Dictionary<string, string>();
            }
            Dictionary<string, string> props = new Dictionary<string, string>(properties);
            // generalProperties
            InflateGeneralProperties(props);
            // dynamicProperties
            InflateDynamicProperties(props);

            // 该条日志的唯一标识
            props[TapOpenlogParamConstants.PARAM_T_LOG_ID] = tLogId;
            // 客户端生成的时间戳，毫秒级
            props[TapOpenlogParamConstants.PARAM_TIMESTAMP] = timestamp.ToString();

            props["action"] = action;

            TapOpenlogStoreBean bean = new TapOpenlogStoreBean(action, timestamp, tLogId, props);
            log.Log("LogBusiness action = " + action + ", sdkProjectName = " + sdkProjectName + " , sdkProjectVersion = " + sdkProjectVersion, bean + "\n" + JsonConvert.SerializeObject(properties));
#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
            businessQueue.Enqueue(bean);
#else
            // log.Log($"This Platform 【{Application.platform}】 is not supported for Openlog.");
#endif
        }

        public void LogTechnology(
            string action,
            Dictionary<string, string> properties = null
        )
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string tLogId = Guid.NewGuid().ToString();

            if (properties == null)
            {
                properties = new Dictionary<string, string>();
            }
            Dictionary<string, string> props = new Dictionary<string, string>(properties);
            // generalProperties
            InflateGeneralProperties(props);
            // dynamicProperties
            InflateDynamicProperties(props);

            // 该条日志的唯一标识
            props[TapOpenlogParamConstants.PARAM_T_LOG_ID] = tLogId;
            // 客户端生成的时间戳，毫秒级
            props[TapOpenlogParamConstants.PARAM_TIMESTAMP] = timestamp.ToString();

            props["action"] = action;

            TapOpenlogStoreBean bean = new TapOpenlogStoreBean(action, timestamp, tLogId, props);

            log.Log("LogTechnology action = " + action, bean + "\n" + JsonConvert.SerializeObject(properties));
#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
            technologyQueue.Enqueue(bean);
#else
            // log.Log($"This Platform 【{Application.platform}】 is not supported for Openlog.");
#endif
        }

        private static void InitGeneralParameter()
        {
            // 应用包名
            generalParameter[TapOpenlogParamConstants.PARAM_APP_PACKAGE_NAME] = Application.identifier;
            // 应用版本字符串
            generalParameter[TapOpenlogParamConstants.PARAM_APP_VERSION] = Application.version;
            // 应用版本（数字）
            generalParameter[TapOpenlogParamConstants.PARAM_APP_VERSION_CODE] = "";
            // 固定一个枚举值: TapSDK
            generalParameter[TapOpenlogParamConstants.PARAM_PN] = "TapSDK";
            // SDK 产物类型
            generalParameter[TapOpenlogParamConstants.PARAM_TAPSDK_ARTIFACT] = "Unity";
            // SDK 运行平台
            generalParameter[TapOpenlogParamConstants.PARAM_PLATFORM] = "PC";
            // 埋点版本号（预留字段），当前全为1
            generalParameter[TapOpenlogParamConstants.PARAM_TRACK_CODE] = "1";
            // SDK生成的设备一次安装的唯一标识
            generalParameter[TapOpenlogParamConstants.PARAM_INSTALL_UUID] = Identity.InstallationId;
            // 设备品牌，eg: Xiaomi
            generalParameter[TapOpenlogParamConstants.PARAM_DV] = "";
            // 设备品牌型号，eg：21051182C
            generalParameter[TapOpenlogParamConstants.PARAM_MD] = SystemInfo.deviceModel;
            // 设备CPU型号，eg：arm64-v8a
            generalParameter[TapOpenlogParamConstants.PARAM_CPU] = "";
            // 支持 CPU 架构，eg：arm64-v8a
            generalParameter[TapOpenlogParamConstants.PARAM_CPU_ABIS] = "";
            // 设备操作系统
            generalParameter[TapOpenlogParamConstants.PARAM_OS] = SystemInfo.operatingSystemFamily.ToString();
            // 设备操作系统版本
            generalParameter[TapOpenlogParamConstants.PARAM_SV] = SystemInfo.operatingSystem;
            // 物理设备真实屏幕分辨率宽
            generalParameter[TapOpenlogParamConstants.PARAM_WIDTH] = Screen.currentResolution.width.ToString();
            // 物理设备真实屏幕分辨率高
            generalParameter[TapOpenlogParamConstants.PARAM_HEIGHT] = Screen.currentResolution.height.ToString();
            // 设备总存储空间（磁盘），单位B
            generalParameter[TapOpenlogParamConstants.PARAM_TOTAL_ROM] = "";
            // 设备总内存，单位B
            generalParameter[TapOpenlogParamConstants.PARAM_TOTAL_RAM] = DeviceInfo.RAM;
            // 芯片型号，eg：Qualcomm Technologies, Inc SM7250
            generalParameter[TapOpenlogParamConstants.PARAM_HARDWARE] = SystemInfo.processorType;
            // SDK进程粒度的本地日志 session_id
            generalParameter[TapOpenlogParamConstants.PARAM_P_SESSION_ID] = sessionID;
        }

        private void InflateGeneralProperties(Dictionary<string, string> props)
        {
            if (generalParameter != null)
            {
                foreach (KeyValuePair<string, string> kv in generalParameter)
                {
                    props[kv.Key] = kv.Value;
                }
            }
        }

        private void InflateDynamicProperties(Dictionary<string, string> props)
        {

            // 客户端时区，eg：Asia/Shanghai
            props[TapOpenlogParamConstants.PARAM_TIMEZONE] = "";
            // SDK接入项目具体模块枚举值
            props[TapOpenlogParamConstants.PARAM_TAPSDK_PROJECT] = sdkProjectName;
            // SDK 模块版本号
            props[TapOpenlogParamConstants.PARAM_TAPSDK_VERSION] = sdkProjectVersion;
            // SDK设置的地区，例如 zh_CN
            props[TapOpenlogParamConstants.PARAM_SDK_LOCALE] = Tracker.getServerLanguage();
            // 游戏账号 ID（非角色 ID）
            props[TapOpenlogParamConstants.PARAM_GAME_USER_ID] = TapCoreStandalone.User.Id;
            // SDK生成的设备全局唯一标识
            props[TapOpenlogParamConstants.PARAM_GID] = "";
            // SDK生成的设备唯一标识
            props[TapOpenlogParamConstants.PARAM_DEVICE_ID] = SystemInfo.deviceUniqueIdentifier;
            // 设备可用存储空间（磁盘），单位B
            props[TapOpenlogParamConstants.PARAM_ROM] = "0";
            // 设备可用内存，单位B
            props[TapOpenlogParamConstants.PARAM_RAM] = "0";
            // taptap的用户ID的外显ID（加密）
            props[TapOpenlogParamConstants.PARAM_OPEN_ID] = openid;
            // 网络类型，eg：wifi, mobile
            props[TapOpenlogParamConstants.PARAM_NETWORK_TYPE] = "";
        }
    }
}