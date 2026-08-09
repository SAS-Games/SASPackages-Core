using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ActionGraphAsset))]
public class ActionGraphAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var graphAsset = (ActionGraphAsset)target;

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Action Graph Asset", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Edit this asset in the Action Graph window. The serialized graph data is hidden in the normal inspector; switch the Inspector to Debug mode if you need to inspect raw config data.",
            MessageType.Info);

        if (GUILayout.Button("Open Action Graph", GUILayout.Height(28f)))
            ActionGraphWindow.OpenWithConfig(graphAsset);
    }
}

[InitializeOnLoad]
internal static class ActionGraphAssetIconInitializer
{
    private const string SessionKey = "SAS.ActionGraph.ActionGraphAssetIconApplied";

    static ActionGraphAssetIconInitializer()
    {
        EditorApplication.delayCall += ApplyIconOnce;
    }

    private static void ApplyIconOnce()
    {
        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);

        Texture2D icon = FindIcon();
        if (icon == null)
            return;

        var instance = ScriptableObject.CreateInstance<ActionGraphAsset>();
        MonoScript script = MonoScript.FromScriptableObject(instance);
        UnityEngine.Object.DestroyImmediate(instance);

        if (script == null)
            return;

        EditorGUIUtility.SetIconForObject(script, icon);

        string scriptPath = AssetDatabase.GetAssetPath(script);
        if (AssetImporter.GetAtPath(scriptPath) is MonoImporter importer)
        {
            importer.SetIcon(icon);
            importer.SaveAndReimport();
        }
    }

    private static Texture2D FindIcon()
    {
        string[] iconNames =
        {
            "AnimatorController Icon",
            "d_AnimatorController Icon",
            "Animator Icon",
            "d_Animator Icon",
            "GraphViewTool On",
            "d_GraphViewTool On"
        };

        foreach (string iconName in iconNames)
        {
            if (EditorGUIUtility.IconContent(iconName).image is Texture2D icon)
                return icon;

            icon = EditorGUIUtility.FindTexture(iconName);
            if (icon != null)
                return icon;
        }

        return null;
    }
}
