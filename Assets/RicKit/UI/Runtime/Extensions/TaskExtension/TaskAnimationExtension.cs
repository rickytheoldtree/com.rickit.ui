using System.Threading;
using Cysharp.Threading.Tasks;
using RicKit.UI.Ease;
using UnityEngine;

namespace RicKit.UI.Extensions.TaskExtension
{
    public static class TaskAnimationExtension
    {
        public static async UniTask Fade(this CanvasGroup target, float targetAlpha, float duration, AnimEase ease = default, CancellationToken cancellationToken = default)
        {
            var startAlpha = target.alpha;
            float time = 0;
            while (time < duration)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                time += Time.deltaTime;
                target.alpha = Mathf.LerpUnclamped(startAlpha, targetAlpha, EaseHelper.Apply(time, duration, ease));
                await UniTask.Yield();
            }
            target.alpha = targetAlpha;
        }
        public static async UniTask Scale(this Transform target, Vector3 endValue, float duration, AnimEase ease = default, CancellationToken cancellationToken = default)
        {
            var startValue = target.localScale;
            float time = 0;
            while (time < duration)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                time += Time.deltaTime;
                target.localScale = Vector3.LerpUnclamped(startValue, endValue, EaseHelper.Apply(time, duration, ease));
                await UniTask.Yield();
            }
            target.localScale = endValue;
        }
    }
}