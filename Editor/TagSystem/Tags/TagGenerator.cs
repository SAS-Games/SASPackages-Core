using UnityEditor;
using UnityEngine;

namespace SAS.Core.TagSystem.Editor
{
    [InitializeOnLoad]
    static class TagDatabaseBootstrap
    {
        static TagDatabaseBootstrap()
        {
            EditorApplication.delayCall += () =>
            {
                TagDatabaseEditorUtility.CreateDatabase(TagDatabase.NAME);
            };
        }
    }
    
    public static class TagDatabaseEditorUtility
    {
        private const string RootPath = "Assets/SASTag";
        private const string ResourcesPath = RootPath + "/Resources";
        private const string DatabaseFolder = ResourcesPath + "/TagDatabase";

        /// <summary>
        /// Creates a TagDatabase ScriptableObject with the given name.
        /// Returns the existing asset if it already exists.
        /// </summary>
        public static TagDatabase CreateDatabase(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                Debug.LogError("[TagSystem] Database name is null or empty.");
                return null;
            }

            EnsureFolders();

            string assetPath = $"{DatabaseFolder}/{databaseName}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<TagDatabase>(assetPath);
            if (existing != null)
                return existing;

            var database = ScriptableObject.CreateInstance<TagDatabase>();
            AssetDatabase.CreateAsset(database, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TagSystem] TagDatabase created: {assetPath}");
            return database;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(RootPath))
                AssetDatabase.CreateFolder("Assets", "SASTag");

            if (!AssetDatabase.IsValidFolder(ResourcesPath))
                AssetDatabase.CreateFolder(RootPath, "Resources");

            if (!AssetDatabase.IsValidFolder(DatabaseFolder))
                AssetDatabase.CreateFolder(ResourcesPath, "TagDatabase");
        }
    }
}