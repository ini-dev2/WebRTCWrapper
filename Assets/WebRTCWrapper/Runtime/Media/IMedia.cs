using System;
using UnityEngine;
using Unity.WebRTC;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace WebRTCWrapper.Runtime
{
    public interface IMedia
    {
        event Action<VideoStreamTrack> OnVideoStreamTrack;
        event Action<AudioStreamTrack> OnAudioStreamTrack;

        MediaType Type { get; }
        IReadOnlyList<MediaStreamTrack> MediaToSend { get; }

        UniTask ReceiveMedia(MediaStreamTrack mediaStream);

        UniTask CreateVideoStream();
        UniTask CreateVideoStream(Camera camera, int width = 1280, int height = 720, int depth = 24, GraphicsFormat format = GraphicsFormat.B8G8R8A8_SRGB);

        UniTask CreateAudioStream();
        UniTask CreateAudioStream(AudioSource source, int sampleRate = 48000, int lengthSec = 1, int microId = 0);
    }
}