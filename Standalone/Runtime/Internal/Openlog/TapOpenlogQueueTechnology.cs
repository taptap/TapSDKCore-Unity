#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
namespace TapSDK.Core.Standalone.Internal
{
    public class TapOpenlogQueueTechnology : TapOpenlogQueueBase
    {
        private const string eventFilePath = "TapLogTechnology";
        private const string urlPath = "putrecords/tds/tapsdk-apm";
        public TapOpenlogQueueTechnology() : base("Technology")
        {
        }
        protected override string GetEventFilePath() => eventFilePath;
        protected override string GetUrlPath() => urlPath;
    }
}
#endif