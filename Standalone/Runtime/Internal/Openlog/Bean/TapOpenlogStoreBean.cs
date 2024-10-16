using System.Collections.Generic;
using Newtonsoft.Json;

namespace TapSDK.Core.Standalone.Internal.Openlog
{
    public class TapOpenlogStoreBean
    {
        [JsonProperty("action")]
        public string action;

        [JsonProperty("timestamp")]
        public long timestamp;

        [JsonProperty("t_log_id")]
        public string tLogId;

        [JsonProperty("props")]
        public readonly Dictionary<string, string> props;

        public TapOpenlogStoreBean(string action, long timestamp, string tLogId, Dictionary<string, string> props)
        {
            this.action = action;
            this.timestamp = timestamp;
            this.tLogId = tLogId;
            this.props = props;
        }

        public override string ToString()
        {
            return $"TapOpenlogStoreBean(action: {action} , tLogId: {tLogId} , timestamp: {timestamp})";
        }
    }
}