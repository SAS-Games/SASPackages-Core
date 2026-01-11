#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SAS.Core.TagSystem.Editor
{
    internal static class TagUsageFinder
    {
        public static List<string> FindUsages(int targetGuid)
        {
            var results = new List<string>();

            // ScriptableObjects
            ScanAssets<ScriptableObject>(targetGuid, results);

            // Prefabs (components inside)
            ScanPrefabs(targetGuid, results);

            // Scenes (saved scenes only)
            ScanScenes(targetGuid, results);

            return results;
        }


        private static void ScanAssets<T>(int guid, List<string> results) where T : Object
        {
            var assetGuids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

            foreach (var assetGuid in assetGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetGuid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);

                if (asset == null)
                    continue;

                ScanSerializedObject(new SerializedObject(asset), $"{asset.name} ({path})", guid, results);
            }
        }
        
        private static void ScanPrefabs(int guid, List<string> results)
        {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");

            foreach (var prefabGuid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(prefabGuid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null)
                    continue;

                foreach (var mb in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null)
                        continue;

                    ScanSerializedObject(
                        new SerializedObject(mb),
                        $"{prefab.name}/{mb.GetType().Name} ({path})",
                        guid,
                        results
                    );
                }
            }
        }


        private static void ScanScenes(int guid, List<string> results)
        {
            var sceneGuids = AssetDatabase.FindAssets("t:Scene");

            foreach (var sceneGuid in sceneGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(sceneGuid);
                if (!AssetDatabase.IsOpenForEdit(path))
                    continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (mb == null)
                            continue;

                        ScanSerializedObject(new SerializedObject(mb), $"{scene.name}/{mb.gameObject.name}/{mb.GetType().Name}",
                            guid,
                            results
                        );
                    }
                }

                EditorSceneManager.CloseScene(scene, true);
            }
        }
        
        private static void ScanSerializedObject(SerializedObject so, string location, int guid, List<string> results)
        {
            var prop = so.GetIterator();

            while (prop.NextVisible(true))
            {
                if (prop.propertyType != SerializedPropertyType.Generic)
                    continue;

                if (prop.type != nameof(Tag))
                    continue;

                var guidProp = prop.FindPropertyRelative("guid");
                if (guidProp != null && guidProp.intValue == guid)
                {
                    results.Add($"{location} → {prop.propertyPath}");
                    return;
                }
            }
        }
    }
}
#endif
