using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RicKit.UI
{
    public static class UniTaskAnimationExtension
    {
        public static async UniTask Fade(this CanvasGroup target, float targetAlpha, float duration, CancellationToken cancellationToken)
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
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            target.alpha = targetAlpha;
        }
        public static async UniTask Scale(this Transform target, Vector3 endValue, float duration, CancellationToken cancellationToken)
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
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            target.localScale = endValue;
        }
    }
}