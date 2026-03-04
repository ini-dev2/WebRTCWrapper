using System;

#if Newtonsoft
using Newtonsoft.Json;
#endif

namespace WebRTCWrapper.Runtime
{
    public class IceData : ISignalingMessage
    {
#if Newtonsoft
        [JsonProperty("candidate")]
#endif
        public string Candidate;

#if Newtonsoft
        [JsonProperty("sdpMid")]
#endif
        public string SdpMid;

#if Newtonsoft
        [JsonProperty("sdpMLineIndex")]
#endif
        public int? SdpMLineIndex;

        public IceData()
        {
        }

        public IceData(string candidate, string sdpMid, int? sdpMLineIndex)
        {
            Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
            SdpMid = sdpMid ?? throw new ArgumentNullException(nameof(sdpMid));
            SdpMLineIndex = sdpMLineIndex;
        }
    }
}