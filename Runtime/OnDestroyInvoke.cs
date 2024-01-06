using System;
using UnityEngine;

namespace RicKit.UI
{
    public class OnDestroyInvoke : MonoBehaviour
    {
        public Action OnDestroyEvent { get; set; }
        private void OnDestroy()
        {
            OnDestroyEvent?.Invoke();
        }
    }
}