using UnityEditor;
using UnityEngine;

namespace SAS.Core.TagSystem.Editor
{
    [InitializeOnLoad]
    internal static class TagDatabaseBootstrap
    {
        static TagDatabaseBootstrap()
        {
            EditorApplication.delayCall += EnsureDatabaseExists;
        }

        private static void EnsureDatabaseExists()
        {
            TagDatabaseEditorUtility.GetOrCreateDatabase(TagDatabase.NAME);
        }
    }

    public static class TagDatabaseEditorUtility
    {
        private const string RootPath = "Assets/SASTag";
        private const string ResourcesPath = RootPath + "/Resources";
        private const string DatabaseFolder = ResourcesPath + "/TagDatabase";

        /// <summary>
        /// Finds an existing TagDatabase anywhere in the project.
        /// Creates one at the default location only when none exists.
        /// </summary>
        public static TagDatabase GetOrCreateDatabase(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                Debug.LogError("[TagSystem] Database name is null or empty.");
                return null;
            }

            TagDatabase existingDatabase = FindDatabase();

            if (existingDatabase != null)
                return existingDatabase;

            return CreateDatabase(databaseName);
        }

        /// <summary>
        /// Finds a TagDatabase asset anywhere under the Assets folder.
        /// </summary>
        public static TagDatabase FindDatabase()
        {
            string[] databaseGuids =
                AssetDatabase.FindAssets($"t:{nameof(TagDatabase)}", new[] { "Assets" });

            if (databaseGuids.Length == 0)
                return null;

            if (databaseGuids.Length > 1)
            {
                Debug.LogWarning(
                    $"[TagSystem] Found {databaseGuids.Length} TagDatabase assets. " +
                    "Only one database is expected. The first database will be used.");
            }

            foreach (string guid in databaseGuids)
            {
                string databasePath = AssetDatabase.GUIDToAssetPath(guid);

                TagDatabase database =
                    AssetDatabase.LoadAssetAtPath<TagDatabase>(databasePath);

                if (database != null)
                    return database;
            }

            return null;
        }

        /// <summary>
        /// Creates a TagDatabase at the default location.
        /// This method should normally be called through GetOrCreateDatabase.
        /// </summary>
        private static TagDatabase CreateDatabase(string databaseName)
        {
            EnsureFolders();

            string assetPath = $"{DatabaseFolder}/{databaseName}.asset";

            // Additional protection in case an asset exists at the default path.
            TagDatabase database = AssetDatabase.LoadAssetAtPath<TagDatabase>(assetPath);

            if (database != null)
                return database;

            database = ScriptableObject.CreateInstance<TagDatabase>();

            AssetDatabase.CreateAsset(database, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[TagSystem] TagDatabase created: {assetPath}", database);

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