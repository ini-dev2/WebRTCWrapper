using System;
using Cysharp.Threading.Tasks;

namespace WebRTCWrapper.Runtime
{
    public interface ISignaling
    {
        event Action<SdpData> OnReceiveSdp;
        event Action<IceData> OnReceiveIce;

        UniTask SendSdp(SdpData sdp);
        UniTask SendIce(IceData ice);
    }
}