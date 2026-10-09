using System;
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

        [SerializeField] protected Button[] moreBtnBacks = Array.Empty<Button>();

        protected override void Awake()
        {
            base.Awake();
            if (btnBack)
                btnBack.onClick.AddListener(OnBackClick);
            foreach (var btn in moreBtnBacks ?? Array.Empty<Button>())
            {
                if (btn) btn.onClick.AddListener(OnBackClick);
            }
            if (cgBlocker)
            {
                cgBlocker.alpha = 0;
                cgBlocker.blocksRaycasts = true;
            }
            CanvasGroup.alpha = 0;
        }

        public override void OnESCClick()
        {
            if (btnBack && btnBack.gameObject.activeInHierarchy && btnBack.interactable)
            {
                OnBackClick();
                return;
            }
            foreach (var btn in moreBtnBacks ?? Array.Empty<Button>())
            {
                if (btn && btn.gameObject.activeInHierarchy && btn.interactable)
                {
                    OnBackClick();
                    break;
                }
            }
        }

        protected override async UniTask OnAnimationIn(CancellationToken cancellationToken)
        {
            ValidateReferences();
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
            ValidateReferences();
            await UniTask.WhenAll(
                CanvasGroup.Fade(0, Duration, AnimEase.InBack, cancellationToken: cancellationToken),
                panel.Scale(0.1f * Vector3.one, Duration, AnimEase.InBack, cancellationToken),
                cgBlocker.Fade(0, Duration, cancellationToken: cancellationToken));
        }

        protected override void RestoreAnimationState(bool shown)
        {
            base.RestoreAnimationState(shown);
            if (panel) panel.localScale = (shown ? 1f : 0.1f) * Vector3.one;
            if (cgBlocker) cgBlocker.alpha = shown ? 1 : 0;
        }
        private void ValidateReferences()
        {
            if (!panel || !cgBlocker)
                throw new InvalidOperationException($"{GetType().Name} requires panel and cgBlocker references.");
        }

        protected virtual void OnBackClick()
        {
            UI.Back();
        }
    }
}