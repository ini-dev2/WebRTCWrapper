using System;
using UnityEngine;
using Unity.WebRTC;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace WebRTCWrapper.Runtime
{
    public class MediaCreator : IMedia
    {
        private readonly MediaType _type;

        private readonly List<MediaStreamTrack> mediaStreams = new();

        public MediaType Type => _type;
        public IReadOnlyList<MediaStreamTrack> MediaToSend => mediaStreams;

        public event Action<VideoStreamTrack> OnVideoStreamTrack;
        public event Action<AudioStreamTrack> OnAudioStreamTrack;

        public MediaCreator(MediaType type)
        {
            _type = type;
        }

        public UniTask CreateAudioStream()
        {
            throw new System.NotImplementedException();
        }

        public async UniTask CreateAudioStream(AudioSource source, int sampleRate = 48000, int lengthSec = 1, int microId = 0)
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("No microphone devices found");
                return;
            }

            var microphoneDevice = Microphone.devices[microId];

            source.clip = Microphone.Start(microphoneDevice, true, lengthSec, sampleRate);
            source.loop = true;
            source.mute = false;

            while (!(Microphone.GetPosition(microphoneDevice) > 0))
            {
                await UniTask.Yield();
            }

            source.Play();

            var audioTrack = new AudioStreamTrack(source);
            mediaStreams.Add(audioTrack);
        }

        public UniTask CreateVideoStream()
        {
            throw new System.NotImplementedException();
        }

        public async UniTask CreateVideoStream(Camera camera, int width = 1280, int height = 720, int depth = 24, GraphicsFormat format = GraphicsFormat.B8G8R8A8_SRGB)
        {
            var renderTexture = new RenderTexture(width, height, depth, format);
            renderTexture.Create();

            var videoTrack = new VideoStreamTrack(renderTexture);
            mediaStreams.Add(videoTrack);
        }

        public async UniTask ReceiveMedia(MediaStreamTrack mediaStream)
        {
            if (mediaStream is VideoStreamTrack vst)
            {
                OnVideoStreamTrack?.Invoke(vst);
            }
            else if (mediaStream is AudioStreamTrack ast)
            {
                OnAudioStreamTrack?.Invoke(ast);
            }
        }
    }
}