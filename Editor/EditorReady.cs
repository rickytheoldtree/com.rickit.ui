using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RicKit.UI.Editor
{
    public static class EditorReady
    {
        [InitializeOnLoadMethod]
        public static void MakeSureSortingLayersReady()
        {
            AddSortingLayer("UI");
            AddSortingLayer("Blocker");
        }

        private static void AddSortingLayer(string sortingLayer)
        {
            foreach (var layer in SortingLayer.layers)
                if (layer.name == sortingLayer) return;

            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var tagManager = new SerializedObject(asset);

            var prop = tagManager.FindProperty("m_SortingLayers");
            var insertIndex = prop.arraySize;
            prop.InsertArrayElementAtIndex(insertIndex);

            var element = prop.GetArrayElementAtIndex(insertIndex);
            var nameProp = element.FindPropertyRelative("name");
            var idProp   = element.FindPropertyRelative("uniqueID");

            nameProp.stringValue = sortingLayer;
            idProp.intValue      = GenerateUniqueID(prop);

            tagManager.ApplyModifiedProperties();
        }

        private static int GenerateUniqueID(SerializedProperty layerArray)
        {
            var used = new HashSet<int>();
            for (var i = 0; i < layerArray.arraySize; i++)
            {
                var id = layerArray
                    .GetArrayElementAtIndex(i)
                    .FindPropertyRelative("uniqueID")
                    .intValue;
                used.Add(id);
            }
            var candidate = 1;
            while (used.Contains(candidate))
                candidate++;
            return candidate;
        }
    }
}