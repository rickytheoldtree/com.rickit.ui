using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using RicKit.UI.Interfaces;
using RicKit.UI.Panels;
using UnityEngine;

namespace RicKit.UI.Tests
{
    public class TestPanel : AbstractUIPanel
    {
        public bool AwakeCalled { get; private set; }
        public int AnimationInCount { get; private set; }
        public Func<CancellationToken, UniTask> AnimationInHook { get; set; }
        public Func<CancellationToken, UniTask> AnimationOutHook { get; set; }

        protected override void Awake()
        {
            base.Awake();
            AwakeCalled = true;
        }

        public override void OnESCClick()
        {
        }

        protected override async UniTask OnAnimationIn(CancellationToken cancellationToken)
        {
            AnimationInCount++;
            if (AnimationInHook != null) await AnimationInHook(cancellationToken);
            else await UniTask.Yield();
        }

        protected override async UniTask OnAnimationOut(CancellationToken cancellationToken)
        {
            if (AnimationOutHook != null) await AnimationOutHook(cancellationToken);
            else await UniTask.Yield();
        }
    }

    public class PanelA : TestPanel
    {
    }

    public class PanelB : TestPanel
    {
    }

    public class PanelC : TestPanel
    {
    }

    public class DestroyOnClosePanel : TestPanel
    {
        public override bool DestroyOnClose => true;
    }

    /// <summary>根节点上没有面板组件的预制体，用来测实例化失败</summary>
    public class BrokenPanel : TestPanel
    {
    }

    public class TestLoader : IReleasablePanelLoader
    {
        private readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();
        public readonly Dictionary<string, int> Loaded = new Dictionary<string, int>();
        public readonly Dictionary<string, int> Released = new Dictionary<string, int>();
        public string ThrowOnPath { get; set; }

        public void Register(string path, GameObject template) => templates[path] = template;

        public int LoadedOf(string path) => Loaded.TryGetValue(path, out var count) ? count : 0;
        public int ReleasedOf(string path) => Released.TryGetValue(path, out var count) ? count : 0;

        public async UniTask<GameObject> LoadPrefabAsync(string path)
        {
            await UniTask.Yield();
            return LoadPrefab(path);
        }

        public GameObject LoadPrefab(string path)
        {
            if (path == ThrowOnPath) throw new InvalidOperationException("load failed: " + path);
            if (!templates.TryGetValue(path, out var template)) return null;
            Loaded[path] = LoadedOf(path) + 1;
            return template;
        }

        public void ReleasePrefab(string path, GameObject prefab)
        {
            Released[path] = ReleasedOf(path) + 1;
        }
    }
}
