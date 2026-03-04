using Cysharp.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json;
using System;
using System.Text;
using UnityEngine;
using WebRTCWrapper.Runtime;

public class NewMonoBehaviourScriptS : MonoBehaviour, ISignaling
{
    private WebSocket ws;
    public string url;

    public event Action<SdpData> OnReceiveSdp;
    public event Action<IceData> OnReceiveIce;

    private async UniTask Start()
    {
        ws = new(url);
        ws.OnOpen += () =>
        {
            Debug.Log("Is Open");
        };

        ws.OnMessage += MessageHandler;

        await ws.Connect();
    }

    private void MessageHandler(byte[] bytes)
    {
        string json = Encoding.UTF8.GetString(bytes);
        var ev = JsonConvert.DeserializeObject<Stub>(json);

        Debug.Log(json);

        switch (ev.Event)
        {
            case "ice":
                var ice = JsonConvert.DeserializeObject<IceMessage>(json);
                OnReceiveIce?.Invoke(ice.iceData);
                break;

            case "sdp":
                var sdp = JsonConvert.DeserializeObject<SdpMessage>(json);
                OnReceiveSdp?.Invoke(sdp.sdpData);
                break;
        }
    }

    public async UniTask SendIce(IceData ice)
    {
        IceMessage iceMessage = new()
        {
            iceData = ice
        };

        await ws.SendText(JsonConvert.SerializeObject(iceMessage));
    }

    public async UniTask SendSdp(SdpData sdp)
    {
        SdpMessage sdpMessage = new()
        {
            sdpData = sdp
        };

        await ws.SendText(JsonConvert.SerializeObject(sdpMessage));
    }

    private void Update()
    {
        ws.DispatchMessageQueue();
    }
}

public class Stub
{
    [JsonProperty("event")]
    public string Event;

    [JsonProperty("data")]
    public object Data;
}

public class SdpMessage
{
    [JsonProperty("event")]
    public string Event = "sdp";

    [JsonProperty("data")]
    public SdpData sdpData;
}

public class IceMessage
{
    [JsonProperty("event")]
    public string Event = "ice";

    [JsonProperty("data")]
    public IceData iceData;
}