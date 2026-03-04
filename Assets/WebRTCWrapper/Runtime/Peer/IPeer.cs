using System;
using Unity.WebRTC;
using Cysharp.Threading.Tasks;

namespace WebRTCWrapper.Runtime
{
    public interface IPeer
    {
        event Action<string> OnIceSend;
        event DelegateOnConnectionStateChange OnConnectionStateChange;
        event DelegateOnIceConnectionChange OnIceConnectionChange;

        UniTask<IPeer> Create();
        UniTask<IPeer> WithConfig(RTCConfiguration config);
        UniTask<IPeer> WithSignaling(ISignaling signaling);
        UniTask<IPeer> WithMedia(IMedia media);
        UniTask<IPeer> Finish();

        UniTask InitCall();
    }
}