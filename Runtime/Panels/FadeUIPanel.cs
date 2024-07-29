using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using RicKit.UI.Extensions.TaskExtension;

namespace RicKit.UI.Panels
{
    public abstract class FadeUIPanel : AbstractUIPanel
    {
        protected const float FadeTime = 0.2f;

        protected override async UniTask OnAnimationIn(CancellationToken cancellationToken)
        {
            CanvasGroup.alpha = 0;
            await CanvasGroup.Fade(1, FadeTime, cancellationToken: cancellationToken);
            OnAnimationInEnd();
        }
        
        protected override async UniTask OnAnimationOut(CancellationToken cancellationToken)
        {
            await CanvasGroup.Fade(0, FadeTime, cancellationToken: cancellationToken);
            OnAnimationOutEnd();
        }
    }
}

