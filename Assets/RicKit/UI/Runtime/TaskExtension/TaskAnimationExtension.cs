using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using RicKit.UI.Ease;
using UnityEngine;

namespace RicKit.UI.TaskExtension
{
    public static class TaskAnimationExtension
    {
        public static async Task Fade(this CanvasGroup target, float targetAlpha, float duration, AnimEase ease = default, CancellationToken cancellationToken = default)
        {
            var completionSource = new TaskCompletionSource<bool>();
            TaskMono.Instance.StartCoroutine(FadeCoroutine(completionSource, target, targetAlpha, duration, ease, cancellationToken));
            await completionSource.Task;
        }
        private static IEnumerator FadeCoroutine(TaskCompletionSource<bool> completionSource, CanvasGroup target, float targetAlpha, float duration, AnimEase ease = default, CancellationToken cancellationToken = default)
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
                yield return null;
            }
            target.alpha = targetAlpha;
            completionSource.SetResult(true);
        }
        public static async Task Scale(this Transform target, Vector3 endValue, float duration, AnimEase ease = default, CancellationToken cancellationToken = default)
        {
            var completionSource = new TaskCompletionSource<bool>();
            TaskMono.Instance.StartCoroutine(ScaleCoroutine(completionSource, target, endValue, duration, ease, cancellationToken));
            await completionSource.Task;
        }
        private static IEnumerator ScaleCoroutine(TaskCompletionSource<bool> completionSource, Transform target, Vector3 endValue, float duration, AnimEase ease = default, CancellationToken cancellationToken = default)
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
                yield return null;
            }
            target.localScale = endValue;
            completionSource.SetResult(true);
        }
    }
}