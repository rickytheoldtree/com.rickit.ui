using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using RicKit.UI.Panels;

namespace RicKit.UI
{
    public static class UIManagerExtensions
    {
        /// <summary>
        /// 打开面板并等到它关闭（隐藏或销毁），用来串弹窗链。等价于 ShowUIAsync 之后 WaitUntilUIHideEnd
        /// </summary>
        public static async UniTask ShowUIAndWaitHideAsync<T>(this IUIManager ui, Action<T> onInit = null,
            CancellationToken cancellationToken = default, string layer = "UI", int orderInLayerDelta = 5,
            bool asyncLoadNew = true) where T : AbstractUIPanel
        {
            await ui.ShowUIAsync(onInit, layer, orderInLayerDelta, asyncLoadNew);
            await ui.WaitUntilUIHideEnd<T>(cancellationToken);
        }
    }
}
