using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RicKit.UI.Panels;
using UnityEditor;
using UnityEngine;

namespace RicKit.UI.Editor.Tests
{
    public class PanelValidatorTests
    {
        private const string Root = "Assets/__RicKitValidatorTests";
        private const string ResourcesUI = Root + "/Resources/UI";
        private const string Prefix = "UI/";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            AssetDatabase.CreateFolder("Assets", "__RicKitValidatorTests");
            AssetDatabase.CreateFolder(Root, "Resources");
            AssetDatabase.CreateFolder(Root + "/Resources", "UI");

            SavePanel<ValidatorFadePanel>($"{ResourcesUI}/ValidatorFadePanel.prefab");
            SavePanel<ValidatorRenamedPanel>($"{ResourcesUI}/Renamed.prefab");
            SavePanel<ValidatorMisplacedPanel>($"{Root}/ValidatorMisplacedPanel.prefab");
            SavePanel<A.ValidatorDupPanel>($"{ResourcesUI}/ValidatorDupPanel.prefab");

            // cgBlocker 空着、moreBtnBacks 有空元素、btnBack 空着（允许）、自己的 btnOk 空着
            SavePanel<ValidatorPopPanel>($"{ResourcesUI}/ValidatorPopPanel.prefab", (root, panel) =>
            {
                var content = new GameObject("Panel", typeof(RectTransform));
                content.transform.SetParent(root.transform, false);
                var so = new SerializedObject(panel);
                so.FindProperty("panel").objectReferenceValue = content.transform;
                var more = so.FindProperty("moreBtnBacks");
                more.arraySize = 1;
                more.GetArrayElementAtIndex(0).objectReferenceValue = null;
                so.ApplyModifiedPropertiesWithoutUndo();
            });
            AssetDatabase.Refresh();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            AssetDatabase.DeleteAsset(Root);
        }

        private static void SavePanel<T>(string path, Action<GameObject, T> setup = null) where T : AbstractUIPanel
        {
            var go = new GameObject(typeof(T).Name, typeof(RectTransform), typeof(T));
            try
            {
                setup?.Invoke(go, go.GetComponent<T>());
                PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static List<PanelIssue> Validate(params Type[] types) => PanelValidator.Validate(types, Prefix);

        [Test]
        public void ValidPanelHasNoIssues()
        {
            Assert.IsEmpty(Validate(typeof(ValidatorFadePanel)));
        }

        [Test]
        public void MissingPrefabIsError()
        {
            var issues = Validate(typeof(ValidatorNoPrefabPanel));
            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(PanelIssueSeverity.Error, issues[0].Severity);
            StringAssert.Contains("没有根节点挂", issues[0].Message);
            StringAssert.EndsWith("ValidatorNoPrefabPanel.cs", issues[0].AssetPath);
        }

        [Test]
        public void PrefabNameMustMatchClass()
        {
            var issues = Validate(typeof(ValidatorRenamedPanel));
            Assert.That(issues.Any(i => i.Severity == PanelIssueSeverity.Error && i.Message.Contains("必须和类名")));
            Assert.That(issues.Any(i => i.Severity == PanelIssueSeverity.Error && i.Message.Contains("找不到资源")));
        }

        [Test]
        public void PopPanelRequiredReferences()
        {
            var issues = Validate(typeof(ValidatorPopPanel));
            var errors = issues.Where(i => i.Severity == PanelIssueSeverity.Error).Select(i => i.Message).ToList();
            var warnings = issues.Where(i => i.Severity == PanelIssueSeverity.Warning).Select(i => i.Message).ToList();
            Assert.That(errors.Any(m => m.StartsWith("cgBlocker 未赋值")), string.Join("\n", errors));
            Assert.That(errors.Any(m => m.StartsWith("moreBtnBacks[0] 未赋值")), string.Join("\n", errors));
            Assert.AreEqual(2, errors.Count, string.Join("\n", errors));
            Assert.AreEqual(1, warnings.Count, string.Join("\n", warnings));
            StringAssert.StartsWith("btnOk 未赋值", warnings[0]);
        }

        [Test]
        public void UnresolvedLoadPathIsError()
        {
            var issues = Validate(typeof(ValidatorMisplacedPanel));
            Assert.AreEqual(1, issues.Count);
            StringAssert.Contains("\"UI/ValidatorMisplacedPanel\" 找不到资源", issues[0].Message);
        }

        [Test]
        public void CustomResolverIsUsed()
        {
            PanelValidator.PathResolver resolver = (string loadPath, out string assetPath) =>
            {
                assetPath = $"{Root}/{loadPath.Substring(Prefix.Length)}.prefab";
                return loadPath == Prefix + nameof(ValidatorMisplacedPanel);
            };
            PanelValidator.RegisterResolver(resolver);
            try
            {
                Assert.IsEmpty(Validate(typeof(ValidatorMisplacedPanel)));
            }
            finally
            {
                PanelValidator.UnregisterResolver(resolver);
            }
        }

        [Test]
        public void ResolverPointingElsewhereIsError()
        {
            PanelValidator.PathResolver resolver = (string loadPath, out string assetPath) =>
            {
                assetPath = $"{ResourcesUI}/ValidatorFadePanel.prefab";
                return true;
            };
            PanelValidator.RegisterResolver(resolver);
            try
            {
                var issues = Validate(typeof(ValidatorMisplacedPanel));
                Assert.AreEqual(1, issues.Count);
                StringAssert.Contains("不是这个预制体", issues[0].Message);
            }
            finally
            {
                PanelValidator.UnregisterResolver(resolver);
            }
        }

        [Test]
        public void DuplicateClassNamesAreErrors()
        {
            var issues = Validate(typeof(A.ValidatorDupPanel), typeof(B.ValidatorDupPanel));
            Assert.AreEqual(2, issues.Count(i => i.Message.Contains("重复")));
        }

        [Test]
        public void DefaultScanSkipsTestAssemblies()
        {
            var issues = PanelValidator.Validate();
            Assert.That(issues.All(i => i.PanelType == null || i.PanelType.Assembly != typeof(ValidatorFadePanel).Assembly));
        }

        [Test]
        public void ReportListsErrorsFirst()
        {
            var report = PanelValidator.FormatReport(Validate(typeof(ValidatorPopPanel)));
            var lines = report.Split('\n').Where(l => l.StartsWith("[")).ToList();
            Assert.AreEqual(3, lines.Count, report);
            StringAssert.StartsWith("[ERROR] ValidatorPopPanel", lines[0]);
            StringAssert.StartsWith("[WARN] ValidatorPopPanel", lines[2]);
        }
    }
}
