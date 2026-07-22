using System.IO;
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
        private const string EditorDefaultResourcesPath = "Assets/Editor Default Resources";
        private const string RootPath = EditorDefaultResourcesPath + "/SASTag";
        private const string DatabaseFolder = RootPath + "/TagDatabase";
        public static string DefaultDatabaseAssetPath => $"{DatabaseFolder}/{TagDatabase.NAME}.asset";

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
        /// Finds a TagDatabase asset anywhere under the Assets folder, migrating the
        /// old runtime Resources location into Editor Default Resources when needed.
        /// </summary>
        public static TagDatabase FindDatabase()
        {
            TagDatabase defaultDatabase = AssetDatabase.LoadAssetAtPath<TagDatabase>(DefaultDatabaseAssetPath);

            if (defaultDatabase != null)
                return defaultDatabase;

            string[] databaseGuids = AssetDatabase.FindAssets($"t:{nameof(TagDatabase)}", new[] { "Assets" });

            if (databaseGuids.Length == 0)
                return null;

            if (databaseGuids.Length > 1)
            {
                Debug.LogWarning($"[TagSystem] Found {databaseGuids.Length} TagDatabase assets. " + "Only one database is expected. The first database will be used.");
            }

            foreach (string guid in databaseGuids)
            {
                string databasePath = AssetDatabase.GUIDToAssetPath(guid);

                TagDatabase database = AssetDatabase.LoadAssetAtPath<TagDatabase>(databasePath);

                if (database == null)
                    continue;

                return database;
            }

            return null;
        }

        public static TagDatabase LoadEditorResourceDatabase(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
                return null;

            string normalizedPath = databasePath.Replace('\\', '/');

            if (!normalizedPath.EndsWith(".asset"))
                normalizedPath += ".asset";

            if (!normalizedPath.StartsWith("Assets/"))
                normalizedPath = $"{EditorDefaultResourcesPath}/{normalizedPath}";

            return AssetDatabase.LoadAssetAtPath<TagDatabase>(normalizedPath);
        }

        [MenuItem("Tools/Tags/Open Tag Database")]
        public static void OpenTagDatabase()
        {
            TagDatabase database = GetOrCreateDatabase(TagDatabase.NAME);

            if (database == null)
                return;

            Selection.activeObject = database;
            EditorGUIUtility.PingObject(database);
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

        private static TagDatabase MoveDatabaseToEditorResources(TagDatabase database, string currentPath)
        {
            EnsureFolders();

            string moveError = AssetDatabase.MoveAsset(currentPath, DefaultDatabaseAssetPath);

            if (!string.IsNullOrEmpty(moveError))
            {
                Debug.LogWarning(
                    $"[TagSystem] Could not move TagDatabase from runtime Resources to editor resources: {moveError}",
                    database);

                return database;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            TagEditorUtility.ClearDatabaseCache();
            DeleteAssetFolderIfEmpty(Path.GetDirectoryName(currentPath)?.Replace('\\', '/'));

            TagDatabase movedDatabase = AssetDatabase.LoadAssetAtPath<TagDatabase>(DefaultDatabaseAssetPath);
            Debug.Log($"[TagSystem] TagDatabase moved to editor resources: {DefaultDatabaseAssetPath}", movedDatabase);

            return movedDatabase != null ? movedDatabase : database;
        }

        private static void DeleteAssetFolderIfEmpty(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
                return;

            string absoluteFolderPath = Path.Combine(Directory.GetCurrentDirectory(), folderPath);

            if (!Directory.Exists(absoluteFolderPath))
                return;

            foreach (string entry in Directory.EnumerateFileSystemEntries(absoluteFolderPath))
            {
                if (!entry.EndsWith(".meta"))
                    return;
            }

            if (AssetDatabase.DeleteAsset(folderPath))
                AssetDatabase.Refresh();
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(EditorDefaultResourcesPath))
                AssetDatabase.CreateFolder("Assets", "Editor Default Resources");

            if (!AssetDatabase.IsValidFolder(RootPath))
                AssetDatabase.CreateFolder(EditorDefaultResourcesPath, "SASTag");

            if (!AssetDatabase.IsValidFolder(DatabaseFolder))
                AssetDatabase.CreateFolder(RootPath, "TagDatabase");
        }
    }
}
