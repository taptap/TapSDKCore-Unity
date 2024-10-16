#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
namespace TapSDK.Core.Standalone.Internal
{
    public class TapOpenlogQueueBusiness : TapOpenlogQueueBase
    {
        private const string eventFilePath = "TapLogBusiness";
        private const string urlPath = "putrecords/tds/tapsdk";

        public TapOpenlogQueueBusiness() : base("Business")
        {
        }

        protected override string GetEventFilePath() => eventFilePath;
        protected override string GetUrlPath() => urlPath;
    }
}
#endif