#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
using Newtonsoft.Json;
using ProtoBuf;
using System.Collections.Generic;

namespace TapSDK.Core.Standalone.Internal
{
    [ProtoContract]
    public class LogContent
    {
        [JsonProperty("Key")]
        [ProtoMember(1)]
        public string Key { get; set; }

        [JsonProperty("Value")]
        [ProtoMember(2)]
        public string Value { get; set; }
    }

    [ProtoContract]
    public class Log
    {
        [JsonProperty("Value")]
        [ProtoMember(1)]
        public uint Time { get; set; }
        [JsonProperty("Contents")]
        [ProtoMember(2)]
        public List<LogContent> Contents { get; set; }
    }

    [ProtoContract]
    public class LogGroup
    {
        [JsonProperty("Logs")]
        [ProtoMember(1)]
        public List<Log> Logs { get; set; }
    }
}
#endif
