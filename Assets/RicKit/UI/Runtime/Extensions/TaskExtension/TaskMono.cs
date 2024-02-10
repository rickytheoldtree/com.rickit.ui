using System;
using UnityEngine;

namespace RicKit.UI.Extensions.TaskExtension
{
    public class TaskMono : MonoBehaviour
    {
        private static TaskMono instance;
        private static TaskMono Instance
        {
            get
            {
                if (instance) return instance;
                instance = new GameObject("TaskMono").AddComponent<TaskMono>();
                DontDestroyOnLoad(instance);
                return instance;
            }
        }
        private Action continuation;
        public static void AddContinuation(Action action)
        {
            Instance.continuation += action;
        }

        private void Update()
        {
            if (continuation == null) return;
            var c = continuation;
            continuation = null;
            c();
        }
    }
}