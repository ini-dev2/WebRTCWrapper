using System;
using UnityEngine;
using Unity.WebRTC;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace WebRTCWrapper.Runtime
{
    public class PeerBase : IPeer, IDisposable
    {
        protected RTCPeerConnection _peer;
        protected ISignaling _signaling;
        protected List<IMedia> _medias = new();
        protected ICoroutineRunner _corotineRunner;

        public event Action<string> OnIceSend;
        public event DelegateOnConnectionStateChange OnConnectionStateChange;
        public event DelegateOnIceConnectionChange OnIceConnectionChange;

        public async UniTask<IPeer> Create()
        {
            _peer = new RTCPeerConnection();
            return this;
        }

        public async UniTask<IPeer> WithConfig(RTCConfiguration config)
        {
            _peer.SetConfiguration(ref config);
            return this;
        }

        public async UniTask<IPeer> WithCoroutineRunner(ICoroutineRunner runner)
        {
            _corotineRunner = runner;
            return this;
        }

        public async UniTask<IPeer> WithSignaling(ISignaling signaling)
        {
            if (signaling == null)
            {
                Debug.LogError("signaling cant be null");
                return null;
            }
            _signaling = signaling;

            _peer.OnIceCandidate += async (ice) => await SendIce(ice);

            _signaling.OnReceiveSdp += async (sdp) => await SetSdp(sdp);

            _signaling.OnReceiveIce += (ice) => _peer.AddIceCandidate(new(new()
            {
                candidate = ice.Candidate,
                sdpMid = ice.SdpMid,
                sdpMLineIndex = ice.SdpMLineIndex
            }));

            return this;
        }

        public async UniTask<IPeer> WithMedia(IMedia media)
        {
            if (media == null)
            {
                Debug.LogError("media cant be null");
                return null;
            }

            await HandleMedia(media);

            _medias.Add(media);
            return this;
        }

        public async UniTask<IPeer> Finish()
        {
            _peer.OnConnectionStateChange += OnConnectionStateChange;
            _peer.OnIceConnectionChange += OnIceConnectionChange;

            _corotineRunner.StartCoroutine(WebRTC.Update());

            return this;
        }

        public async UniTask InitCall()
        {
            var offer = await CreateSdp(RTCSdpType.Offer);
            await _signaling.SendSdp(new(RTCSdpType.Offer, offer.sdp));
        }

        private async UniTask HandleMedia(IMedia media)
        {
            var type = media.Type == MediaType.ForReceive ?
                AddReceiveMedia(media) :
                AddMediaToSend(media);

            await type;
        }

        protected virtual async UniTask AddReceiveMedia(IMedia media)
        {
            _peer.OnTrack += async (e) =>
            {
                Debug.Log($"ASDASD {e}");
                await media.ReceiveMedia(e.Track);
            };
        }

        protected virtual async UniTask AddMediaToSend(IMedia media)
        {
            if (media.MediaToSend.Count < 0)
            {
                Debug.Log("List of media for send is empty");
            }

            for (int i = 0; i < media.MediaToSend.Count; i++)
            {
                _peer.AddTrack(media.MediaToSend[i]);
            }       
        }

        protected virtual async UniTask<RTCSessionDescription> CreateSdp(RTCSdpType rTCSdpType)
        {
            var desc = rTCSdpType == RTCSdpType.Offer ? 
                _peer.CreateOffer() : _peer.CreateAnswer();
            await desc;

            Debug.Log($"Create sdp {rTCSdpType}");

            await SetLocalDescription(desc.Desc);

            if (rTCSdpType == RTCSdpType.Answer)
            {
                await _signaling.SendSdp(new(desc.Desc.type, desc.Desc.sdp));
            }
            
            return desc.Desc;
        }

        protected virtual async UniTask<RTCSessionDescription> SetSdp(SdpData sdpData)
        {
            await SetRemoteDescription(new()
            {
                type = sdpData.Type,
                sdp = sdpData.Sdp
            });

            Debug.Log($"Set sdp {sdpData.Type}");

            var typ = sdpData.Type == RTCSdpType.Offer ?
                await CreateSdp(RTCSdpType.Answer) : default;

            return typ;
        }

        protected virtual async UniTask SetLocalDescription(RTCSessionDescription desc)
            => await _peer.SetLocalDescription(ref desc);

        protected virtual async UniTask SetRemoteDescription(RTCSessionDescription desc)
            => await _peer.SetRemoteDescription(ref desc);

        protected virtual async UniTask SendIce(RTCIceCandidate rTCIceCandidate)
        {
            var res = _signaling.SendIce(new (rTCIceCandidate.Candidate, rTCIceCandidate.SdpMid, rTCIceCandidate.SdpMLineIndex));
            await res;

            Debug.Log("Send ice");

            OnIceSend?.Invoke(res.Status.ToString());
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        } 
    }
}