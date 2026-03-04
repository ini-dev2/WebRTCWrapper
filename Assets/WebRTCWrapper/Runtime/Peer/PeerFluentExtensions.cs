using Unity.WebRTC;
using Cysharp.Threading.Tasks;

namespace WebRTCWrapper.Runtime
{
    public static class PeerFluentExtensions
    {
        public static async UniTask<IPeer> WithConfig(this UniTask<IPeer> task, RTCConfiguration config)
        {
            var peer = await task;
            return await peer.WithConfig(config);
        }

        public static async UniTask<IPeer> WithSignaling(this UniTask<IPeer> task, ISignaling signaling)
        {
            var peer = await task;
            return await peer.WithSignaling(signaling);
        }

        public static async UniTask<IPeer> WithMedia(this UniTask<IPeer> task, IMedia media)
        {
            var peer = await task;
            return await peer.WithMedia(media);
        }

        public static async UniTask<IPeer> Finish(this UniTask<IPeer> task)
        {
            var peer = await task;
            return await peer.Finish();
        }
    }
}