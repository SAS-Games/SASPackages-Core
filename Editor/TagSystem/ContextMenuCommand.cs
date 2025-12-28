using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SAS.Core.TagSystem.Editor
{
    public class ContextMenuCommand
    {
        [MenuItem("CONTEXT/Component/Add Tag")]
        private static void AddTag(MenuCommand command)
        {
            var self = (Component)command.context;
            var tagger = self.gameObject.GetComponent<Tagger>();

            if (tagger == null)
                tagger = self.gameObject.AddComponent<Tagger>();
            tagger.AddTag(self);
            UnityEditor.EditorUtility.SetDirty(self);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("CONTEXT/Component/Remove Tag")]
        private static void RemoveTag(MenuCommand command)
        {
            var self = (Component)command.context;
            var tagger = self.gameObject.GetComponent<Tagger>();
            tagger.RemoveTag(self);
            UnityEditor.EditorUtility.SetDirty(self);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("CONTEXT/Component/RemoveAll Tags")]
        private static void RemoveAllTags(MenuCommand command)
        {
            var self = (Component)command.context;
            var tagger = self.gameObject.GetComponent<Tagger>();
            tagger.RemoveAllTags(self);
            UnityEditor.EditorUtility.SetDirty(self);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("CONTEXT/Component/Log Tags")]
        private static void LogTags(MenuCommand command)
        {
            var self = (Component)command.context;
            var tagger = self.gameObject.GetComponent<Tagger>();
            tagger.LogAllTags(self);
        }
    }
}
