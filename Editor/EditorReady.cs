using UnityEditor;
using UnityEngine;

namespace RicKit.UI.Editor
{
    public static class EditorReady
    {
        [InitializeOnLoadMethod]
        public static void MakeSureSortingLayersReady()
        {
            AddSortingLayer("UI", 114514);
            AddSortingLayer("Blocker", 810975);
        }

        private static void AddSortingLayer(string sortingLayer, int uniqueID)
        {
            if (IsHasSortingLayer(sortingLayer)) return;
            var tagManager =
                new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var it = tagManager.GetIterator();
            while (it.NextVisible(true))
            {
                if (it.name != "m_SortingLayers") continue;
                it.InsertArrayElementAtIndex(it.arraySize);
                var dataPoint = it.GetArrayElementAtIndex(it.arraySize - 1);
                while (dataPoint.NextVisible(true))
                {
                    switch (dataPoint.name)
                    {
                        case "name":
                            dataPoint.stringValue = sortingLayer;
                            tagManager.ApplyModifiedProperties();
                            break;
                        case "uniqueID":
                            dataPoint.intValue = uniqueID;
                            tagManager.ApplyModifiedProperties();
                            break;
                    }
                }
            }
        }

        private static bool IsHasSortingLayer(string sortingLayer)
        {
            var layers = SortingLayer.layers;
            foreach (var layer in layers)
            {
                if (layer.name == sortingLayer)
                {
                    return true;
                }
            }
            return false;
        }
    }
}