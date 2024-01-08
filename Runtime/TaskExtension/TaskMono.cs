using UnityEngine;

namespace RicKit.UI.TaskExtension
{
    public class TaskMono : MonoBehaviour
    {
        private static TaskMono instance;
        public static TaskMono Instance
        {
            get
            {
                if (instance != null) return instance;
                var go = new GameObject("TaskMono");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<TaskMono>();
                return instance;
            }
        }
    }
}