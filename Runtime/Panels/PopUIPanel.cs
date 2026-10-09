using System.Threading;
using Cysharp.Threading.Tasks;
using RicKit.UI.Ease;
using RicKit.UI.Extensions.TaskExtension;
using UnityEngine;
using UnityEngine.UI;

namespace RicKit.UI.Panels
{
    public abstract class PopUIPanel : AbstractUIPanel
    {
        protected const float Duration = 0.3f;

        [SerializeField] protected CanvasGroup cgBlocker;

        [SerializeField] protected Transform panel;

        [SerializeField] protected Button btnBack;

        [SerializeField] protected Button[] moreBtnBacks;

        protected override void Awake()
        {
            base.Awake();
            if (btnBack)
                btnBack.onClick.AddListener(OnBackClick);
            foreach (var btn in moreBtnBacks)
            {
                btn.onClick.AddListener(OnBackClick);
            }
            cgBlocker.alpha = 0;
            cgBlocker.blocksRaycasts = true;
            CanvasGroup.alpha = 0;
        }

        public override void OnESCClick()
        {
            if (btnBack && btnBack.gameObject.activeSelf)
            {
                OnBackClick();
                return;
            }
            foreach (var btn in moreBtnBacks)
            {
                if (btn.gameObject.activeSelf)
                {
                    OnBackClick();
                    break;
                }
            }
        }

        protected override async UniTask OnAnimationIn(CancellationToken cancellationToken)
        {
            panel.localScale = 0.1f * Vector3.one;
            cgBlocker.alpha = 0;
            CanvasGroup.alpha = 0;
            await UniTask.WhenAll(
                CanvasGroup.Fade(1, Duration, cancellationToken: cancellationToken),
                panel.Scale(Vector3.one, Duration, AnimEase.OutBack, cancellationToken),
                cgBlocker.Fade(1, Duration, cancellationToken: cancellationToken));
        }

        protected override async UniTask OnAnimationOut(CancellationToken cancellationToken)
        {
            await UniTask.WhenAll(
                CanvasGroup.Fade(0, Duration, AnimEase.InBack, cancellationToken: cancellationToken),
                panel.Scale(0.1f * Vector3.one, Duration, AnimEase.InBack, cancellationToken),
                cgBlocker.Fade(0, Duration, cancellationToken: cancellationToken));
        }

        protected virtual void OnBackClick()
        {
            UI.Back();
        }
    }
}