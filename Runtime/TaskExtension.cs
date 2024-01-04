using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RicKit.UI
{
    public static class TaskExtension
    {
        public static async Task Fade(this CanvasGroup target, float targetAlpha, float duration, CancellationToken cancellationToken)
        {
            var startAlpha = target.alpha;
            float time = 0;

            while (time < duration)
            {
                // 检查是否有取消操作
                cancellationToken.ThrowIfCancellationRequested();

                time += Time.deltaTime;
                var blend = Mathf.Clamp01(time / duration);
                target.alpha = Mathf.Lerp(startAlpha, targetAlpha, blend);

                // 等待下一帧
                await Task.Yield();
            }

            target.alpha = targetAlpha;
        }
        public static async Task Scale(this Transform target, Vector3 endValue, float duration, CancellationToken cancellationToken)
        {
            var startValue = target.localScale;
            float time = 0;

            while (time < duration)
            {
                // 检查是否有取消操作
                cancellationToken.ThrowIfCancellationRequested();

                time += Time.deltaTime;
                var blend = Mathf.Clamp01(time / duration);
                target.localScale = Vector3.Lerp(startValue, endValue, blend);

                // 等待下一帧
                await Task.Yield();
            }

            target.localScale = endValue;
        }
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