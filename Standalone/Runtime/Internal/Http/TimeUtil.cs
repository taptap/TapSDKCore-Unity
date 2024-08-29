using System;
using UnityEngine;

namespace TapSDK.Core.Standalone.Internal {

    public static class TimeUtil
    {
        private static int timeOffset = 0;

        private static void SetTimeOffset(int offset)
        {
            timeOffset = offset;
        }

        // 获取当前时间的秒级时间戳
        public static int GetCurrentTime()
        {
            DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan timeSpan = DateTime.UtcNow - epochStart;
            return (int)timeSpan.TotalSeconds + timeOffset;
        }

        public static void FixTime(int time)
        {
            Debug.Log("FixTime called with time: " + time);
            SetTimeOffset(time - (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds);
        }
    }

}
