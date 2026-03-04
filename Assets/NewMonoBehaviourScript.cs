using System.Collections;
using Unity.WebRTC;
using UnityEngine;
using WebRTCWrapper.Runtime;

public class NewMonoBehaviourScript : MonoBehaviour, ICoroutineRunner
{
    [SerializeField] NewMonoBehaviourScriptS newMonoBehaviourScriptS;
    [SerializeField] NewMonoBehaviourScriptS newMonoBehaviourScriptS2;

    public AudioSource s;
    public AudioSource s2;

    [SerializeField] public PeerBase peer;
    [SerializeField] public PeerBase peer2;

    private async void Start()
    {
        MediaCreator cr = new(MediaType.ForSend);
        await cr.CreateAudioStream(s);

        MediaCreator cr2 = new(MediaType.ForReceive);
        cr2.OnAudioStreamTrack += (e) => s2.SetTrack(e);

        RTCConfiguration config = default;
        config.iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } };

        peer = new();
        await peer.Create()
            .WithConfig(config)
            .WithCoroutineRunner(this)
            .WithSignaling(newMonoBehaviourScriptS)
            .WithMedia(cr)
            .Finish();

        peer2 = new();
        await peer2.Create()
            .WithConfig(config)
            .WithCoroutineRunner(this)
            .WithSignaling(newMonoBehaviourScriptS2)
            .WithMedia(cr2)
            .Finish();
    }

    [ContextMenu("asdasd")]
    public async void CTx()
    {
        await peer.InitCall();
    }

    void ICoroutineRunner.StartCoroutine(IEnumerator enumerator)
    {
        StartCoroutine(enumerator);
    }
}