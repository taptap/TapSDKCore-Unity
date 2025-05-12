using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections;
using TapSDK.Core.Internal;
using TapSDK.Core;

namespace TapSDK.Core.Mobile {
    public class IOSNativeWrapper
    {

#if UNITY_IOS
    // 导入 C 函数
        [DllImport("__Internal")]
        private static extern void _TapTapSDKInitWithCoreAndOtherOptions(
            string initOptionsJson,
            string otherOptionsJson
        );

        [DllImport("__Internal")]
        private static extern void _TapTapSDKInit(
            string initOptionsJson
        );
        
        [DllImport("__Internal")]
        private static extern void _TapTapEventSetUserID(string userID);

        [DllImport("__Internal")]
        private static extern void _TapTapEventSetUserIDWithProperties(string userID, string propertiesJson);

        [DllImport("__Internal")]
        private static extern void _TapTapEventClearUser();

        [DllImport("__Internal")]
        private static extern string _TapTapEventGetDeviceId();

        [DllImport("__Internal")]
        private static extern void _TapTapEventLogEvent(string name, string propertiesJson);

        [DllImport("__Internal")]
        private static extern void _TapTapEventLogChargeEvent(string orderID, string productName, long amount, string currencyType, string paymentMethod, string propertiesJson);

        [DllImport("__Internal")]
        private static extern void _TapTapEventDeviceInitialize(string propertiesJson);

        [DllImport("__Internal")]
        private static extern void _TapTapEventDeviceUpdate(string propertiesJson);

        [DllImport("__Internal")]
        private static extern void _TapTapEventDeviceAdd(string propertiesJson);

        [DllImport("__Internal")]
        private static extern void _TapTapEventUserInitialize(string propertiesJson);

        [DllImport("__Internal")]
        private static extern void _TapTapEventUserUpdate(string propertiesJson);

        [DllImport("__Internal")]
        private static extern void _TapTapEventUserAdd(string propertiesJson);

        [DllImport("__Internal")]
        private static extern void _TapTapEventAddCommonProperty(string key, string value);

        [DllImport("__Internal")]
        private static extern void _TapTapEventAddCommon(string propertiesJson);

        [DllImport("__Internal")]
        private static extern void _TapTapEventClearCommonProperty(string key);

        [DllImport("__Internal")]
        private static extern void _TapTapEventClearCommonProperties(string[] keys, int count);

        [DllImport("__Internal")]
        private static extern void _TapTapEventClearAllCommonProperties();


        // 定义一个委托类型，匹配 Objective-C 中的 block 参数
        public delegate string DynamicPropertiesCalculatorDelegate();

        // 注意：这个方法的封装比较特殊，因为它需要一个返回 NSDictionary 的回调。
        [DllImport("__Internal")]
        private static extern void _TapTapEventRegisterDynamicProperties(DynamicPropertiesCalculatorDelegate callback);


        // 插入 UA
        [DllImport("__Internal")]
        private static extern void _TapTapSDKCoreAppendUA(string platform, string version);

        [DllImport("__Internal")]
        private static extern void _TapTapSDKCoreSetSDKArtifact(string artifact);

        [DllImport("__Internal")]
        private static extern void _TapTapSDKCoreSwitchToRND();

        // 提供给 Unity 调用的 C# 方法
        public static void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
            // 将其他选项转换为JSON字符串
            string otherOptionsJson = ConvertOtherOptionsToJson(otherOptions);
            // 调用C方法
            _TapTapSDKInitWithCoreAndOtherOptions(
                JsonUtility.ToJson(coreOption),
                otherOptionsJson
            );
        }

        // 提供给 Unity 调用的 C# 方法
        public static void Init(TapTapSdkOptions coreOption)
        {
            // 调用C方法
            _TapTapSDKInit(
                JsonUtility.ToJson(coreOption)
            );
        }

        public static void SetUserID(string userID)
        {
            _TapTapEventSetUserID(userID);
        }

        public static void SetUserID(string userID, string properties)
        {
            _TapTapEventSetUserIDWithProperties(userID, properties);
        }

        public static void ClearUser()
        {
            _TapTapEventClearUser();
        }

        public static string GetDeviceId()
        {
            return _TapTapEventGetDeviceId();
        }

        public static void LogEvent(string name, string properties)
        {
            _TapTapEventLogEvent(name, properties);
        }

        public static void LogChargeEvent(string orderID, string productName, long amount, string currencyType, string paymentMethod, string properties)
        {
            _TapTapEventLogChargeEvent(orderID, productName, amount, currencyType, paymentMethod, properties);
        }

        public static void DeviceInitialize(string properties)
        {
            _TapTapEventDeviceInitialize(properties);
        }

        public static void DeviceUpdate(string properties)
        {
            _TapTapEventDeviceUpdate(properties);
        }

        public static void DeviceAdd(string properties)
        {
            _TapTapEventDeviceAdd(properties);
        }

        public static void UserInitialize(string properties)
        {
            _TapTapEventUserInitialize(properties);
        }

        public static void UserUpdate(string properties)
        {
            _TapTapEventUserUpdate(properties);
        }

        public static void UserAdd(string properties)
        {
            _TapTapEventUserAdd(properties);
        }

        public static void AddCommonProperty(string key, string value)
        {
            _TapTapEventAddCommonProperty(key, value);
        }

        public static void AddCommon(string properties)
        {
            _TapTapEventAddCommon(properties);
        }

        public static void ClearCommonProperty(string key)
        {
            _TapTapEventClearCommonProperty(key);
        }

        public static void ClearCommonProperties(string[] keys)
        {
            _TapTapEventClearCommonProperties(keys, keys.Length);
        }

        public static void ClearAllCommonProperties()
        {
            _TapTapEventClearAllCommonProperties();
        }

        public static void SetRND(){
            _TapTapSDKCoreSwitchToRND();
        }
        // 定义一个 Func<string> 委托，用于从 Unity 使用者那里获取动态属性
        private static Func<string> dynamicPropertiesCallback;
        public static void RegisterDynamicProperties(Func<string> callback)
        {
            dynamicPropertiesCallback = callback;
            _TapTapEventRegisterDynamicProperties(DynamicPropertiesCalculator);
        }

        // Unity 端的回调方法，返回一个 JSON 字符串
        [AOT.MonoPInvokeCallback(typeof(DynamicPropertiesCalculatorDelegate))]
        private static string DynamicPropertiesCalculator()
        {
            if (dynamicPropertiesCallback != null)
            {
                string properties = dynamicPropertiesCallback();
                return properties;
            }
            return null;
        }

        private static string ConvertOtherOptionsToJson(TapTapSdkBaseOptions[] otherOptions)
        {
            if (otherOptions == null || otherOptions.Length == 0)
            {
                return "[]"; // 如果没有其他选项，则返回空数组的JSON表示
            }

            // 创建一个数组来存储每个选项的JSON字符串
            string[] jsonOptions = new string[otherOptions.Length];
            
            for (int i = 0; i < otherOptions.Length; i++)
            {
                // 获取moduleName
                string moduleName = otherOptions[i].moduleName;
                
                // 使用JsonUtility将每个选项对象转换为JSON字符串
                string optionJson = JsonUtility.ToJson(otherOptions[i]);

                // 将moduleName添加到JSON字符串中
                optionJson = AddModuleNameToJson(optionJson, moduleName);

                jsonOptions[i] = optionJson;
            }

            // 将所有JSON字符串连接成一个JSON数组
            string jsonArray = "[" + string.Join(",", jsonOptions) + "]";
            
            return jsonArray;
        }

        // 辅助方法，用于将moduleName添加到JSON字符串中
        private static string AddModuleNameToJson(string json, string moduleName)
        {
            // 在JSON字符串的开头添加moduleName字段
            return "{\"moduleName\":\"" + moduleName + "\"," + json.TrimStart('{');
        }

        // 调用此方法来设置 xuaMap
        public static void SetPlatformAndVersion(string platform, string version)
        {
            _TapTapSDKCoreAppendUA(platform, version);
        }

        public static void SetSDKArtifact(string artifact)
        {
            _TapTapSDKCoreSetSDKArtifact(artifact);
        }
#endif
    }
}
