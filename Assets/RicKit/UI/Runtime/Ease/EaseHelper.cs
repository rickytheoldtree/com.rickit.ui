using System;

namespace RicKit.UI.Ease
{
    public enum AnimEase
    {
        Linear,
        QuadIn,
        QuadOut,
        QuadInOut,
        InBack,
        OutBack,
        InOutBack,
    }
    public static class EaseHelper
    {
        private static float Linear(float t) => t;
        private static float QuadIn(float t) => t * t;
        private static float QuadOut(float t) => t * (2 - t);

        private static float QuadInOut(float t)
        {
            if (t < 0.5f) return 2 * t * t;
            return -1 + (4 - 2 * t) * t;
        }

        private const float Back = 1.70158f;
        private static float InBack(float t)
        {
            return t * t * ((Back + 1) * t - Back);
        }
        private static float OutBack(float t)
        {
            var f = t - 1;
            return f * f * ((Back + 1) * f + Back) + 1;
        }
        private static float InOutBack(float t)
        {
            var f = t * 2;
            if (t < 0.5)
            {
                return 0.5f * (f * f * ((Back * 1.525f + 1) * f - Back * 1.525f));
            }
            else
            {
                f -= 2;
                return 0.5f * (f * f * ((Back * 1.525f + 1) * f + Back * 1.525f) + 2);
            }
        }
        public static float Apply(float time, float duration, AnimEase ease)
        {
            return Apply(time, duration, GetEasingFunction(ease));
        }

        private static Func<float, float> GetEasingFunction(AnimEase ease)
        {
            return ease switch
            {
                AnimEase.Linear => Linear,
                AnimEase.QuadIn => QuadIn,
                AnimEase.QuadOut => QuadOut,
                AnimEase.QuadInOut => QuadInOut,
                AnimEase.InBack => InBack,
                AnimEase.OutBack => OutBack,
                AnimEase.InOutBack => InOutBack,
                _ => throw new ArgumentOutOfRangeException(nameof(ease), ease, null)
            };
        }

        private static float Apply(float time, float duration, Func<float, float> easingFunction)
        {
            if (duration <= 0) return 1;
            if (time < 0) return 0;
            if (time > duration) return 1;
            return easingFunction(time / duration);
        }
    }

}