# WebRTCWrapper

A lightweight wrapper around **Unity.WebRTC** with a Fluent API, UniTask support, and signaling abstraction. Get a peer-to-peer connection up and running quickly without digging into low-level WebRTC details.

---

## Table of Contents

- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Core Components](#core-components)
  - [IPeer / PeerBase](#ipeer--peerbase)
  - [IMedia / MediaCreator](#imedia--mediacreator)
  - [ISignaling](#isignaling)
  - [Signaling Messages](#signaling-messages)
  - [ICoroutineRunner](#icoroutinerunner)
- [Fluent API](#fluent-api)
- [Full Integration Example](#full-integration-example)
- [Dependencies](#dependencies)
- [License](#license)

---

## Features

- 🔗 **Fluent API** — chain calls like `Create().WithConfig(...).WithSignaling(...).WithMedia(...).Finish()`
- ⚡ **UniTask** — all async operations return `UniTask`, zero allocations, seamless Unity integration
- 🎥 **Media tracks** — send and receive video (`VideoStreamTrack`) and audio (`AudioStreamTrack`)
- 📡 **Abstract signaling** — implement `ISignaling` for WebSocket, HTTP, SignalR, etc.
- 🧩 **Extensible** — subclass `PeerBase` and override SDP creation/negotiation
- 🎛️ **Optional Newtonsoft.Json** — `[JsonProperty]` attributes enabled via the `Newtonsoft` define

---

## Requirements

| Component | Version |
|---|---|
| Unity | 2021.3 LTS or newer |
| [com.unity.webrtc](https://docs.unity3d.com/Packages/com.unity.webrtc@latest) | 3.x |
| [UniTask (Cysharp)](https://github.com/Cysharp/UniTask) | 2.x |

> **Note:** video sending uses `VideoStreamTrack(RenderTexture)`; audio uses `AudioStreamTrack(AudioSource)` with microphone capture.

---

## Installation

### Option 1 — Git URL (UPM)

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.ini-dev2.webrtc-wrapper": "https://github.com/ini-dev2/WebRTCWrapper.git"
  }
}
```

### Option 2 — Clone

```bash
git clone https://github.com/ini-dev2/WebRTCWrapper.git
```

Copy the contents of `Assets/` into your project.

---

## Quick Start

```csharp
using WebRTCWrapper.Runtime;
using Unity.WebRTC;
using Cysharp.Threading.Tasks;

public class WebRTCTest : UnityEngine.MonoBehaviour, ICoroutineRunner
{
    private IPeer _peer;

    private async void Start()
    {
        var config = new RTCConfiguration
        {
            iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } }
        };

        _peer = await new PeerBase()
            .Create()
            .WithConfig(config)
            .WithCoroutineRunner(this)
            .WithSignaling(new MySignaling())   // your ISignaling implementation
            .WithMedia(new MediaCreator(MediaType.ForSend))
            .Finish();

        await _peer.InitCall(); // initiate offer
    }
}
```

---

## Architecture

```
┌─────────────┐      ┌──────────────┐      ┌─────────────┐
│   IPeer     │◄─────│   ISignaling │◄─────│  SdpData    │
│  (PeerBase) │      │              │      │  IceData    │
└──────┬──────┘      └──────────────┘      └─────────────┘
       │
       │  holds
       ▼
┌─────────────┐
│   IMedia    │──► MediaCreator
│  (tracks)   │
└─────────────┘
```

`PeerBase` wires an `RTCPeerConnection` to `ISignaling` and one or more `IMedia` instances.

---

## Core Components

### IPeer / PeerBase

The main interface for managing a peer connection.

| Method | Description |
|---|---|
| `Create()` | Creates the `RTCPeerConnection` |
| `WithConfig(RTCConfiguration)` | Sets configuration (STUN/TURN) |
| `WithCoroutineRunner(ICoroutineRunner)` | Drives `WebRTC.Update()` via a coroutine runner |
| `WithSignaling(ISignaling)` | Attaches the signaling transport and hooks ICE/SDP handlers |
| `WithMedia(IMedia)` | Adds a media stream (for sending or receiving) |
| `Finish()` | Finalizes setup and registers state events |
| `InitCall()` | Creates an offer and sends it through signaling |

**Events:**

- `OnIceSend` — an ICE candidate has been sent
- `OnConnectionStateChange` — connection state changed
- `OnIceConnectionChange` — ICE connection state changed

> ⚠️ `Dispose()` is not implemented yet (throws `NotImplementedException`). Release the `RTCPeerConnection` manually.

---

### IMedia / MediaCreator

Media abstraction. `MediaCreator` is the ready-to-use implementation.

```csharp
var media = new MediaCreator(MediaType.ForSend);

// Video from a camera (render texture)
await media.CreateVideoStream(camera, width: 1280, height: 720);

// Audio from a microphone
await media.CreateAudioStream(audioSource, sampleRate: 48000);

// Get tracks to send
IReadOnlyList<MediaStreamTrack> tracks = media.MediaToSend;
```

**Media types (`MediaType`):**

| Value | Purpose |
|---|---|
| `None` | not set |
| `ForSend` | tracks we send |
| `ForReceive` | tracks we receive (via `OnTrack`) |

**Receive events:**

```csharp
media.OnVideoStreamTrack += track => { /* render to RawImage */ };
media.OnAudioStreamTrack += track => { /* play audio */ };
```

---

### ISignaling

Interface for the signaling transport — implement it against your backend.

```csharp
public interface ISignaling
{
    event Action<SdpData> OnReceiveSdp;
    event Action<IceData> OnReceiveIce;

    UniTask SendSdp(SdpData sdp);
    UniTask SendIce(IceData ice);
}
```

Example skeleton for WebSocket:

```csharp
public class WsSignaling : ISignaling
{
    public event Action<SdpData> OnReceiveSdp;
    public event Action<IceData> OnReceiveIce;

    private readonly WebSocket _ws;

    public WsSignaling(WebSocket ws)
    {
        _ws = ws;
        _ws.OnMessage += msg =>
        {
            var envelope = JsonUtility.FromJson<Envelope>(msg);
            if (envelope.type == "sdp") OnReceiveSdp?.Invoke(JsonUtility.FromJson<SdpData>(envelope.payload));
            if (envelope.type == "ice") OnReceiveIce?.Invoke(JsonUtility.FromJson<IceData>(envelope.payload));
        };
    }

    public UniTask SendSdp(SdpData sdp) => _ws.SendAsync("...");
    public UniTask SendIce(IceData ice) => _ws.SendAsync("...");
}
```

---

### Signaling Messages

All messages implement the marker interface `ISignalingMessage`.

**`SdpData`** — SDP description:

```csharp
public class SdpData : ISignalingMessage
{
    public RTCSdpType Type;  // Offer / Answer / PrAnswer / Rollback
    public string Sdp;
}
```

**`IceData`** — ICE candidate:

```csharp
public class IceData : ISignalingMessage
{
    public string Candidate;
    public string SdpMid;
    public int? SdpMLineIndex;
}
```

> When the `Newtonsoft` compile symbol is defined, fields receive `[JsonProperty("...")]` attributes for correct serialization via Newtonsoft.Json.

---

### ICoroutineRunner

Coroutine-launch abstraction (lets you test `PeerBase` outside a `MonoBehaviour`).

```csharp
public interface ICoroutineRunner
{
    void StartCoroutine(IEnumerator enumerator);
}
```

Usually it's enough to implement it on your own `MonoBehaviour`:

```csharp
public class CoroutineRunner : MonoBehaviour, ICoroutineRunner { }
```

---

## Fluent API

All `IPeer` methods return `UniTask<IPeer>`, enabling chained calls. The `PeerFluentExtensions` class adds convenience extensions:

| Extension | Equivalent |
|---|---|
| `WithConfig(config)` | `peer.WithConfig(config)` |
| `WithCoroutineRunner(runner)` | `peer.WithCoroutineRunner(runner)` |
| `WithSignaling(signaling)` | `peer.WithSignaling(signaling)` |
| `WithMedia(media)` | `peer.WithMedia(media)` |
| `Finish()` | `peer.Finish()` |

---

## Full Integration Example

```csharp
using WebRTCWrapper.Runtime;
using Unity.WebRTC;
using Cysharp.Threading.Tasks;

public class RtcController : UnityEngine.MonoBehaviour, ICoroutineRunner
{
    [SerializeField] private UnityEngine.Camera _camera;
    [SerializeField] private UnityEngine.AudioSource _audioSource;

    private IPeer _peer;
    private MediaCreator _sendMedia;
    private MediaCreator _receiveMedia;

    public async void StartCall()
    {
        // 1. Outgoing media
        _sendMedia = new MediaCreator(MediaType.ForSend);
        await _sendMedia.CreateVideoStream(_camera, 1280, 720);
        await _sendMedia.CreateAudioStream(_audioSource);

        // 2. Incoming media
        _receiveMedia = new MediaCreator(MediaType.ForReceive);
        _receiveMedia.OnVideoStreamTrack += track => { /* bind to RawImage */ };

        // 3. ICE configuration
        var config = new RTCConfiguration
        {
            iceServers = new[]
            {
                new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } }
            }
        };

        // 4. Build the peer
        _peer = await new PeerBase()
            .Create()
            .WithConfig(config)
            .WithCoroutineRunner(this)
            .WithSignaling(new WsSignaling(/* ... */))
            .WithMedia(_sendMedia)
            .WithMedia(_receiveMedia)
            .Finish();

        _peer.OnConnectionStateChange += state =>
            UnityEngine.Debug.Log($"Connection: {state}");

        // 5. Initiate the call
        await _peer.InitCall();
    }
}
```

---

## Dependencies

- [Unity.WebRTC](https://docs.unity3d.com/Packages/com.unity.webrtc@latest) — peer connection, tracks, ICE
- [UniTask](https://github.com/Cysharp/UniTask) — allocation-free async
- [Newtonsoft.Json](https://www.newtonsoft.com/json) *(optional)* — serialization attributes via `#if Newtonsoft`

---
