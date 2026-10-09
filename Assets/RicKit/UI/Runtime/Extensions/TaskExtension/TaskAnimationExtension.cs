using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using RicKit.UI.Ease;
using UnityEngine;

namespace RicKit.UI.Extensions.TaskExtension
{
    public static class TaskAnimationExtension
    {
        public static async UniTask Fade(this CanvasGroup target, float targetAlpha, float duration,
            AnimEase ease = default, CancellationToken cancellationToken = default, bool ignoreTimeScale = true)
        {
            UIManager.EnsureMainThread();
            cancellationToken.ThrowIfCancellationRequested();
            if (float.IsNaN(duration) || float.IsInfinity(duration))
                throw new ArgumentOutOfRangeException(nameof(duration), "Animation duration must be finite.");
            if (!target) throw new ArgumentNullException(nameof(target));
            var startAlpha = target.alpha;
            float time = 0;
            while (time < duration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!target) throw new OperationCanceledException("The animation target was destroyed.", cancellationToken);
                time += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
                target.alpha = Mathf.LerpUnclamped(startAlpha, targetAlpha, EaseHelper.Apply(time, duration, ease));
            }
            cancellationToken.ThrowIfCancellationRequested();
            target.alpha = targetAlpha;
        }

        public static async UniTask Scale(this Transform target, Vector3 endValue, float duration,
            AnimEase ease = default, CancellationToken cancellationToken = default, bool ignoreTimeScale = true)
        {
            UIManager.EnsureMainThread();
            cancellationToken.ThrowIfCancellationRequested();
            if (float.IsNaN(duration) || float.IsInfinity(duration))
                throw new ArgumentOutOfRangeException(nameof(duration), "Animation duration must be finite.");
            if (!target) throw new ArgumentNullException(nameof(target));
            var startValue = target.localScale;
            float time = 0;
            while (time < duration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!target) throw new OperationCanceledException("The animation target was destroyed.", cancellationToken);
                time += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
                target.localScale = Vector3.LerpUnclamped(startValue, endValue, EaseHelper.Apply(time, duration, ease));
            }
            cancellationToken.ThrowIfCancellationRequested();
            target.localScale = endValue;
        }
    }
}
