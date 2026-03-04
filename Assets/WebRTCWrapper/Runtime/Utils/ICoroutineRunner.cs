using System.Collections;

namespace WebRTCWrapper.Runtime
{
    public interface ICoroutineRunner
    {
        void StartCoroutine(IEnumerator enumerator);
    }
}
