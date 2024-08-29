using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TapSDK.Core.Standalone.Internal
{
    public class NetUtils
    {
        // 随机生成一个 10 位的字符串
        public static string GenerateNonce()
        {
            string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            char[] nonce = new char[10];
            for (int i = 0; i < 10; i++)
            {
                nonce[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
            }
            return new string(nonce);
        }

        public static Dictionary<string, string> commonHeaders {
            get {
                var headers = new Dictionary<string, string> {
                    { "User-Agent", $"{TapTapSDK.SDKPlatform}/{TapTapSDK.Version}" },
                    { "X-Tap-Ts", TimeUtil.GetCurrentTime().ToString() },
                    { "X-Tap-Nonce", NetUtils.GenerateNonce() },
                    { "X-Tap-PN", "TapSDK" },
                    { "X-Tap-Lang", Tracker.getServerLanguage() },
                    { "X-Tap-Device-Id", Identity.DeviceId },
                    { "X-Tap-Platform", "PC" },
                    { "X-Tap-SDK-Module", "TapSDKCore" },
                    { "X-Tap-SDK-Module-Version", TapTapSDK.Version },
                    { "X-Tap-SDK-Artifact", "Unity" }
                };
                
                if (TapCoreStandalone.User.Id != null) {
                    headers.Add("X-Tap-SDK-Game-User-Id", TapCoreStandalone.User.Id);
                }

                return headers;
            }
        }
    }
}