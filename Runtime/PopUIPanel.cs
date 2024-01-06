using System.Threading;
using System.Threading.Tasks;
using RicKit.UI.Ease;
using RicKit.UI.TaskExtension;
using UnityEngine;
using UnityEngine.UI;

namespace RicKit.UI
{
    public abstract class PopUIPanel : AbstractUIPanel
    {
        protected const float Duration = 0.3f;

        [SerializeField] private CanvasGroup cgBlocker;

        [SerializeField] protected Transform panel;

        [SerializeField] protected Button btnBack;

        protected override void Awake()
        {
            base.Awake();
            if (btnBack)
                btnBack.onClick.AddListener(OnBackClick);
            cgBlocker.alpha = 0;
            cgBlocker.blocksRaycasts = true;
            CanvasGroup.alpha = 0;
        }

        public override void OnESCClick()
        {
            if (!btnBack || !btnBack.gameObject.activeSelf) return;
            OnBackClick();
        }

        protected override async Task OnAnimationIn(CancellationToken cancellationToken)
        {
            panel.localScale = 0.1f * Vector3.one;
            cgBlocker.alpha = 0;
            CanvasGroup.alpha = 0;
            await Task.WhenAll(
                CanvasGroup.Fade(1, Duration, cancellationToken: cancellationToken),
                panel.Scale(Vector3.one, Duration, AnimEase.OutBack, cancellationToken),
                cgBlocker.Fade(1, Duration, cancellationToken: cancellationToken));
            OnAnimationInEnd();
        }

        protected override async Task OnAnimationOut(CancellationToken cancellationToken)
        {
            await Task.WhenAll(
                panel.Scale(0.1f * Vector3.one, Duration, AnimEase.InBack, cancellationToken),
                cgBlocker.Fade(0, Duration, cancellationToken: cancellationToken));
            OnAnimationOutEnd();
        }

        protected virtual void OnBackClick()
        {
            UI.Back();
        }
    }
}