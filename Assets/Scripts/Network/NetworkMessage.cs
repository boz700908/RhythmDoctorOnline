using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RDOnline.Network
{
    /// <summary>
    /// 发送的消息结构
    /// </summary>
    [Serializable]
    public class RequestMessage
    {
        [JsonProperty("type")]
        public string Type{ get; set; }

        [JsonProperty("data")]
        public object Data{ get; set; }

        [JsonProperty("requestId")] 
        public string RequestId { get; set; } = "";
    }

    /// <summary>
    /// 接收的消息结构
    /// </summary>
    [Serializable]
    public class ResponseMessage
    {
        [JsonProperty("type")]
        public string Type{ get; set; }

        [JsonProperty("success")]
        public bool Success{ get; set; }

        [JsonProperty("message")]
        public string Message{ get; set; }

        [JsonProperty("data")]
        public JObject Data{ get; set; }

        [JsonProperty("requestId")]
        public string RequestId{ get; set; }
    }
}
