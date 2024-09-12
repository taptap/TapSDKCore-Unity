using UnityEngine;
using Newtonsoft.Json;

namespace TapSDK.Core
{
    public interface TapTapSdkBaseOptions
    {
        string moduleName { get; }

        AndroidJavaObject androidObject{get;}
    }

    public enum TapTapRegionType
    {
        CN = 0,
        Overseas = 1
    }


    public enum TapTapLanguageType
    {
        Auto = 0,// 自动
        zh_Hans,// 简体中文
        en,// 英文
        zh_Hant,// 繁体中文
        ja,// 日文
        ko,// 韩文
        th,// 泰文
        id,// 印度尼西亚语
        de,// 德语
        es,// 西班牙语
        fr,// 法语
        pt,// 葡萄牙语
        ru,// 俄罗斯语
        tr,// 土耳其语
        vi// 越南语
    }

    public class TapTapSdkOptions : TapTapSdkBaseOptions
    {
        /// <summary>
        /// 客户端 ID，开发者后台获取
        /// </summary>
        public string clientId;
        /// <summary>
        /// 客户端令牌，开发者后台获取
        /// </summary>
        public string clientToken;
        /// <summary>
        /// 地区，CN 为国内，Overseas 为海外
        /// </summary>
        public TapTapRegionType region = TapTapRegionType.CN;
        /// <summary>
        /// 语言，默认为 Auto，默认情况下，国内为 zh_Hans，海外为 en
        /// </summary>
        public TapTapLanguageType preferredLanguage = TapTapLanguageType.Auto;
        /// <summary>
        /// 渠道，如 AppStore、GooglePlay
        /// </summary>
        public string channel = null;
        /// <summary>
        /// 游戏版本号，如果不传则默认读取应用的版本号
        /// </summary>
        public string gameVersion = null;
        /// <summary>
        /// 初始化时传入的自定义参数，会在初始化时上报到 device_login 事件
        /// </summary>
        public string propertiesJson = null;
        /// <summary>
        /// CAID，仅国内 iOS
        /// </summary>
        public string caid = null;
        /// <summary>
        /// 是否能够覆盖内置参数，默认为 false
        /// </summary>
        public bool overrideBuiltInParameters = false;
        /// <summary>
        /// 是否开启广告商 ID 收集，默认为 false
        /// </summary>
        public bool enableAdvertiserIDCollection = false;
        /// <summary>
        /// 是否开启自动上报 IAP 事件
        /// </summary>
        public bool enableAutoIAPEvent = true;
        /// <summary>
        /// OAID证书, 仅 Android，用于上报 OAID 仅 [TapTapRegion.CN] 生效
        /// </summary>
        public string oaidCert = null;
        /// <summary>
        /// 是否开启日志，Release 版本请设置为 false
        /// </summary>
        public bool enableLog = false;
  
        [JsonProperty("moduleName")]
        private string _moduleName = "TapTapSDKCore";
        [JsonIgnore]
        public string moduleName
        {
            get => _moduleName;
        }

        public AndroidJavaObject androidObject => throw new System.NotImplementedException();
    }
}
