
namespace TapSDK.Core.Standalone.Internal {
    public static class Constants {
        public static readonly string EVENT = "event";

        public static readonly string PROPERTY_INITIALIZE_TYPE = "initialise";
        public static readonly string PROPERTY_UPDATE_TYPE = "update";
        public static readonly string PROPERTY_ADD_TYPE = "add";


        public readonly static string SERVER_URL_BJ = "https://e.tapdb.net/v2";
        public readonly static string SERVER_URL_SG = "https://e.tapdb.ap-sg.tapapis.com/v2";
        public readonly static string DEVICE_LOGIN = "device_login";
        public readonly static string USER_LOGIN = "user_login";

        internal static string ClientSettingsFileName = "TapSDKClientSettings.json";
        internal static string ClientSettingsEventKey = "ClientSettingsEventKey";

        public static string TAPSDK_HOST {
            get {
                if (TapCoreStandalone.isRnd) {
                    if (TapCoreStandalone.coreOptions.region == TapTapRegionType.CN)
                        return "https://tapsdk.api.xdrnd.cn";
                    else
                        return "https://tapsdk.api.xdrnd.com";
                } else {
                    if (TapCoreStandalone.coreOptions.region == TapTapRegionType.CN)
                        return "https://tapsdk.tapapis.cn";
                    else
                        return "https://tapsdk.tapapis.com";
                }
            }
        }

    }

}
