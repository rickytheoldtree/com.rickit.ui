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
            var t = duration <= 0 ? 1 : Math.Max(0, Math.Min(1, time / duration));
            // Evaluate directly to avoid constructing a method-group delegate on every animation frame.
            switch (ease)
            {
                case AnimEase.Linear: return Linear(t);
                case AnimEase.QuadIn: return QuadIn(t);
                case AnimEase.QuadOut: return QuadOut(t);
                case AnimEase.QuadInOut: return QuadInOut(t);
                case AnimEase.InBack: return InBack(t);
                case AnimEase.OutBack: return OutBack(t);
                case AnimEase.InOutBack: return InOutBack(t);
                default: throw new ArgumentOutOfRangeException(nameof(ease), ease, null);
            }
        }
    }
}