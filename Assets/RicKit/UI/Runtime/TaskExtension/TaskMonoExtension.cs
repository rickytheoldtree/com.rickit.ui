using System.Threading;
using UnityEngine;

namespace RicKit.UI.TaskExtension
{
    public static class TaskMonoExtension
    {
        public static CancellationToken GetCancellationTokenOnDestroy(this MonoBehaviour target)
        {
            var tokenSource = new CancellationTokenSource();
            if (target.TryGetComponent<OnDestroyInvoke>(out var invoke))
            {
                invoke.OnDestroyEvent += () => tokenSource.Cancel();
            }
            else
            {
                invoke = target.gameObject.AddComponent<OnDestroyInvoke>();
                invoke.OnDestroyEvent += () => tokenSource.Cancel();
            }
            return tokenSource.Token;
        }
    }
}