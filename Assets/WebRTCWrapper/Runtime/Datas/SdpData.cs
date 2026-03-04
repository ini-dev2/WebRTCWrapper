using System;
using Unity.WebRTC;

#if Newtonsoft
using Newtonsoft.Json;
#endif

namespace WebRTCWrapper.Runtime
{
    public class SdpData : ISignalingMessage
    {
#if Newtonsoft
        [JsonProperty("type")]
#endif
        public RTCSdpType Type;

#if Newtonsoft
        [JsonProperty("sdp")]
#endif
        public string Sdp;

        public SdpData()
        {
        }

        public SdpData(RTCSdpType type, string sdp)
        {
            Type = type;
            Sdp = sdp ?? throw new ArgumentNullException(nameof(sdp));
        }
    }
}