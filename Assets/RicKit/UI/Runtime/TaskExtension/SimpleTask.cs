using System;
using UnityEngine;

namespace RicKit.UI.TaskExtension
{
    public struct SimpleTask : System.Runtime.CompilerServices.INotifyCompletion
    {
        public static SimpleTask Yield() => new SimpleTask();
        public SimpleTask GetAwaiter() => this;
        public bool IsCompleted => false;

        public void GetResult()
        {
        }

        public void OnCompleted(Action continuation)
        {
            TaskMono.AddContinuation(continuation);
        }
    }
    
}