using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using RicKit.UI.Panels;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RicKit.UI.Editor
{
    public enum PanelIssueSeverity
    {
        Warning,
        Error
    }

    public class PanelIssue
    {
        public PanelIssueSeverity Severity { get; }
        public Type PanelType { get; }
        /// <summary>
        /// 出问题的预制体，没有预制体时是脚本路径
        /// </summary>
        public string AssetPath { get; }
        public string Message { get; }

        public PanelIssue(PanelIssueSeverity severity, Type panelType, string assetPath, string message)
        {
            Severity = severity;
            PanelType = panelType;
            AssetPath = assetPath;
            Message = message;
        }

        public override string ToString() =>
            $"[{(Severity == PanelIssueSeverity.Error ? "ERROR" : "WARN")}] {PanelType?.Name ?? "-"} ({AssetPath ?? "-"}): {Message}";
    }

    /// <summary>
    /// 检查面板脚本、预制体和运行时加载路径是否对得上。
    /// 菜单 RicKit/UI/Validate Panels；命令行：
    /// Unity -batchmode -nographics -projectPath . -executeMethod RicKit.UI.Editor.PanelValidator.RunBatch [-rickitUIReport path]
    /// </summary>
    public static class PanelValidator
    {
        /// <summary>
        /// 把运行时的加载路径（assetPathPrefix + 类名）解析成编辑器里的资源路径，解析不了返回 false
        /// </summary>
        public delegate bool PathResolver(string loadPath, out string assetPath);

        public const string DefaultReportPath = "Logs/RicKitUI/panel-validation.txt";
        private const string ReportArg = "-rickitUIReport";
        private static readonly List<PathResolver> Resolvers = new List<PathResolver>();

        // 内置基类里运行时一定会用到的字段，空着就是空引用
        private static readonly Dictionary<Type, string[]> RequiredFields = new Dictionary<Type, string[]>
        {
            { typeof(PopUIPanel), new[] { "cgBlocker", "panel", "moreBtnBacks" } }
        };

        // 内置基类里允许为空的字段
        private static readonly Dictionary<Type, string[]> OptionalFields = new Dictionary<Type, string[]>
        {
            { typeof(PopUIPanel), new[] { "btnBack" } }
        };

        /// <summary>
        /// 用自定义 IPanelLoader 的项目在 [InitializeOnLoadMethod] 里注册，先于内置的 Addressables / Resources 解析
        /// </summary>
        public static void RegisterResolver(PathResolver resolver)
        {
            if (resolver != null && !Resolvers.Contains(resolver)) Resolvers.Add(resolver);
        }

        public static void UnregisterResolver(PathResolver resolver) => Resolvers.Remove(resolver);

        [MenuItem("RicKit/UI/Validate Panels", false, 3)]
        public static void ValidateFromMenu()
        {
            var issues = Validate();
            foreach (var issue in issues)
            {
                var context = AssetDatabase.LoadAssetAtPath<Object>(issue.AssetPath ?? "");
                if (issue.Severity == PanelIssueSeverity.Error) Debug.LogError($"[RicKit.UI] {issue}", context);
                else Debug.LogWarning($"[RicKit.UI] {issue}", context);
            }
            Debug.Log($"[RicKit.UI] {Summary(issues)}");
        }

        /// <summary>
        /// 命令行入口：报告写到 -rickitUIReport（默认 Logs/RicKitUI/panel-validation.txt），
        /// 退出码 0 没有错误，1 有错误，2 检查本身出了异常
        /// </summary>
        public static void RunBatch()
        {
            var exitCode = 2;
            try
            {
                var reportPath = GetArg(ReportArg) ?? DefaultReportPath;
                var issues = Validate();
                var report = FormatReport(issues);
                var dir = Path.GetDirectoryName(Path.GetFullPath(reportPath));
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(reportPath, report);
                Debug.Log($"[RicKit.UI] 报告已写到 {reportPath}\n{report}");
                exitCode = issues.Any(i => i.Severity == PanelIssueSeverity.Error) ? 1 : 0;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                if (Application.isBatchMode) EditorApplication.Exit(exitCode);
            }
        }

        public static string FormatReport(IReadOnlyList<PanelIssue> issues)
        {
            var sb = new StringBuilder();
            sb.AppendLine("RicKit UI panel validation");
            sb.AppendLine($"assetPathPrefix: \"{LoadSettings()?.assetPathPrefix}\"");
            sb.AppendLine(Summary(issues));
            foreach (var issue in issues.OrderByDescending(i => i.Severity).ThenBy(i => i.PanelType?.Name))
                sb.AppendLine(issue.ToString());
            return sb.ToString();
        }

        private static string Summary(IReadOnlyCollection<PanelIssue> issues) =>
            $"errors: {issues.Count(i => i.Severity == PanelIssueSeverity.Error)}, " +
            $"warnings: {issues.Count(i => i.Severity == PanelIssueSeverity.Warning)}";

        /// <summary>
        /// 检查 Assets 下所有非抽象面板（测试程序集里的除外），加载前缀取 Resources/UISettings
        /// </summary>
        public static List<PanelIssue> Validate()
        {
            var settings = LoadSettings();
            var issues = new List<PanelIssue>();
            if (!settings)
                issues.Add(new PanelIssue(PanelIssueSeverity.Warning, null, null,
                    "找不到 Resources/UISettings，按空前缀检查加载路径"));
            issues.AddRange(Validate(FindProjectPanelTypes(), settings ? settings.assetPathPrefix : ""));
            return issues;
        }

        public static List<PanelIssue> Validate(IEnumerable<Type> panelTypes, string assetPathPrefix)
        {
            var types = panelTypes.Distinct().ToList();
            var issues = new List<PanelIssue>();
            var scripts = FindScripts();
            var prefabs = FindPanelPrefabs();
            var addressables = new AddressableIndex();

            foreach (var group in types.GroupBy(t => t.Name).Where(g => g.Count() > 1))
            {
                foreach (var type in group)
                    issues.Add(new PanelIssue(PanelIssueSeverity.Error, type, ScriptPath(scripts, type),
                        $"类名 {type.Name} 重复（{string.Join("、", group.Select(t => t.FullName))}），运行时按类名加载，只能有一个"));
            }

            foreach (var type in types.OrderBy(t => t.Name))
            {
                var scriptPath = ScriptPath(scripts, type);
                if (scriptPath == null)
                    issues.Add(new PanelIssue(PanelIssueSeverity.Error, type, null,
                        $"找不到 {type.Name}.cs：脚本文件名必须和类名一致，否则预制体挂不上这个组件"));

                prefabs.TryGetValue(type, out var candidates);
                if (candidates == null || candidates.Count == 0)
                {
                    issues.Add(new PanelIssue(PanelIssueSeverity.Error, type, scriptPath,
                        $"没有根节点挂 {type.Name} 的预制体"));
                    continue;
                }

                var prefab = candidates.FirstOrDefault(p => p.name == type.Name);
                if (!prefab)
                {
                    prefab = candidates[0];
                    issues.Add(new PanelIssue(PanelIssueSeverity.Error, type, AssetDatabase.GetAssetPath(prefab),
                        $"预制体名是 {prefab.name}，必须和类名 {type.Name} 一致（运行时按 assetPathPrefix + 类名加载）"));
                }
                foreach (var other in candidates.Where(p => p != prefab))
                    issues.Add(new PanelIssue(PanelIssueSeverity.Warning, type, AssetDatabase.GetAssetPath(other),
                        $"另一个根节点挂 {type.Name} 的预制体，运行时用的是 {AssetDatabase.GetAssetPath(prefab)}"));

                CheckPrefab(type, prefab, issues);
                CheckLoadPath(type, prefab, assetPathPrefix + type.Name, addressables, issues);
            }
            return issues;
        }

        private static void CheckPrefab(Type type, GameObject prefab, List<PanelIssue> issues)
        {
            var path = AssetDatabase.GetAssetPath(prefab);
            if (!prefab.GetComponent<RectTransform>())
                issues.Add(new PanelIssue(PanelIssueSeverity.Error, type, path, "根节点没有 RectTransform"));

            var panels = prefab.GetComponents<AbstractUIPanel>().Where(p => p).ToList();
            if (panels.Count > 1)
                issues.Add(new PanelIssue(PanelIssueSeverity.Warning, type, path,
                    $"根节点挂了多个面板组件：{string.Join("、", panels.Select(p => p.GetType().Name))}"));

            var missingScripts = prefab.GetComponentsInChildren<Transform>(true)
                .Where(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
                .Select(t => GetPath(t, prefab.transform))
                .ToList();
            if (missingScripts.Count > 0)
                issues.Add(new PanelIssue(PanelIssueSeverity.Warning, type, path,
                    $"有丢失的脚本（Missing Script）：{string.Join("、", missingScripts)}"));

            var panel = panels.FirstOrDefault(p => p.GetType() == type);
            if (panel) CheckReferences(type, panel, path, issues);
        }

        private static void CheckReferences(Type type, AbstractUIPanel panel, string path, List<PanelIssue> issues)
        {
            var it = new SerializedObject(panel).GetIterator();
            var enter = true;
            while (it.NextVisible(enter))
            {
                enter = false;
                if (it.name == "m_Script") continue;
                var declaring = FindField(type, it.name)?.DeclaringType;
                if (declaring != null && OptionalFields.TryGetValue(declaring, out var optional) && optional.Contains(it.name))
                    continue;
                var required = declaring != null && RequiredFields.TryGetValue(declaring, out var names) && names.Contains(it.name);
                var severity = required ? PanelIssueSeverity.Error : PanelIssueSeverity.Warning;
                var reason = required ? $"（{declaring.Name} 运行时会用到，空着会空引用）" : "";

                if (it.propertyType == SerializedPropertyType.ObjectReference)
                {
                    var problem = DescribeEmpty(it);
                    if (problem != null)
                        issues.Add(new PanelIssue(severity, type, path, $"{it.name} {problem}{reason}"));
                }
                else if (it.isArray && it.arrayElementType.StartsWith("PPtr<"))
                {
                    for (var i = 0; i < it.arraySize; i++)
                    {
                        var problem = DescribeEmpty(it.GetArrayElementAtIndex(i));
                        if (problem != null)
                            issues.Add(new PanelIssue(severity, type, path, $"{it.name}[{i}] {problem}{reason}"));
                    }
                }
            }
        }

        private static string DescribeEmpty(SerializedProperty prop)
        {
            if (prop.objectReferenceValue) return null;
            return prop.objectReferenceInstanceIDValue != 0 ? "引用丢失（Missing）" : "未赋值";
        }

        private static void CheckLoadPath(Type type, GameObject prefab, string loadPath, AddressableIndex addressables,
            List<PanelIssue> issues)
        {
            var prefabPath = AssetDatabase.GetAssetPath(prefab);
            string resolved = null;
            string source = null;
            foreach (var resolver in Resolvers)
            {
                if (!resolver(loadPath, out var assetPath)) continue;
                resolved = assetPath;
                source = "自定义解析";
                break;
            }
            if (resolved == null && addressables.TryGetAssetPath(loadPath, out var addressPath))
            {
                resolved = addressPath;
                source = "Addressables";
            }
            if (resolved == null)
            {
                var loaded = Resources.Load<GameObject>(loadPath);
                if (loaded)
                {
                    resolved = AssetDatabase.GetAssetPath(loaded);
                    source = "Resources";
                }
            }

            if (resolved == null)
            {
                var hint = addressables.TryGetAddress(prefabPath, out var address)
                    ? $"这个预制体的 Addressables 地址是 \"{address}\"，和运行时路径不一致"
                    : "Resources 下没有、Addressables 也没有这个地址；用自定义加载器的项目请用 PanelValidator.RegisterResolver 注册解析规则";
                issues.Add(new PanelIssue(PanelIssueSeverity.Error, type, prefabPath,
                    $"运行时加载路径 \"{loadPath}\" 找不到资源：{hint}"));
            }
            else if (!string.Equals(resolved, prefabPath, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new PanelIssue(PanelIssueSeverity.Error, type, prefabPath,
                    $"运行时加载路径 \"{loadPath}\" 经 {source} 解析到 {resolved}，不是这个预制体"));
            }
        }

        private static UISettings LoadSettings()
        {
            var settings = Resources.Load<UISettings>("UISettings");
            if (settings) return settings;
            return AssetDatabase.FindAssets("t:UISettings")
                .Select(g => AssetDatabase.LoadAssetAtPath<UISettings>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(s => s);
        }

        private static IEnumerable<Type> FindProjectPanelTypes()
        {
            var projectAssemblies = new HashSet<string>(CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                .Where(a => a.sourceFiles.Any(f => f.StartsWith("Assets/")))
                .Select(a => a.name));
            return TypeCache.GetTypesDerivedFrom<AbstractUIPanel>()
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
                .Where(t => projectAssemblies.Contains(t.Assembly.GetName().Name));
        }

        private static Dictionary<Type, string> FindScripts()
        {
            var scripts = new Dictionary<Type, string>();
            foreach (var guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                var type = script ? script.GetClass() : null;
                if (type != null && typeof(AbstractUIPanel).IsAssignableFrom(type)) scripts[type] = path;
            }
            return scripts;
        }

        private static string ScriptPath(Dictionary<Type, string> scripts, Type type) =>
            scripts.TryGetValue(type, out var path) ? path : null;

        // 运行时要求面板组件挂在根节点，只看根节点上的精确类型
        private static Dictionary<Type, List<GameObject>> FindPanelPrefabs()
        {
            var prefabs = new Dictionary<Type, List<GameObject>>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (!go) continue;
                foreach (var panel in go.GetComponents<AbstractUIPanel>())
                {
                    if (!panel) continue;
                    if (!prefabs.TryGetValue(panel.GetType(), out var list))
                        prefabs[panel.GetType()] = list = new List<GameObject>();
                    if (!list.Contains(go)) list.Add(go);
                }
            }
            return prefabs;
        }

        private static FieldInfo FindField(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (var t = type; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
            {
                var field = t.GetField(name, flags);
                if (field != null) return field;
            }
            return null;
        }

        private static string GetPath(Transform t, Transform root)
        {
            var path = t.name;
            while (t != root && t.parent)
            {
                t = t.parent;
                path = $"{t.name}/{path}";
            }
            return path;
        }

        private static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        private class AddressableIndex
        {
            private readonly Dictionary<string, string> addressToPath = new Dictionary<string, string>();
            private readonly Dictionary<string, string> pathToAddress = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public AddressableIndex()
            {
#if RICKIT_ADDRESSABLES
                var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
                if (!settings) return;
                var entries = new List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry>();
                foreach (var group in settings.groups)
                {
                    if (!group) continue;
                    foreach (var entry in group.entries)
                    {
                        if (entry.IsFolder) entry.GatherAllAssets(entries, false, true, false);
                        else entries.Add(entry);
                    }
                }
                foreach (var entry in entries)
                {
                    if (string.IsNullOrEmpty(entry.address) || string.IsNullOrEmpty(entry.AssetPath)) continue;
                    addressToPath[entry.address] = entry.AssetPath;
                    pathToAddress[entry.AssetPath] = entry.address;
                }
#endif
            }

            public bool TryGetAssetPath(string address, out string path) => addressToPath.TryGetValue(address, out path);

            public bool TryGetAddress(string path, out string address) => pathToAddress.TryGetValue(path, out address);
        }
    }
}
