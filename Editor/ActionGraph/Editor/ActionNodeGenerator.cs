using System.IO;
using UnityEditor;
using UnityEngine;

public static class ActionNodeGenerator
{
    [MenuItem("Assets/Create/Action Graph/Action Node", false, 80)]
    public static void CreateActionNode()
    {
        string folderPath = GetSelectedPath();

        string fileName = "NewActionNode.cs";

        string fullPath = Path.Combine(folderPath, fileName);

        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
            0,
            ScriptableObject.CreateInstance<CreateActionNodeAsset>(),
            fullPath,
            null,
            null
        );
    }

    private static string GetSelectedPath()
    {
        string path = "Assets";

        var selection = Selection.activeObject;

        if (selection != null)
        {
            path = AssetDatabase.GetAssetPath(selection);

            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
            {
                path = Path.GetDirectoryName(path);
            }
        }

        return path;
    }
}