using System.Linq;

namespace SAS.Core.TagSystem.Editor
{
    internal static class TagRenameUtility
    {
        public static bool CanRename(TagDatabase db, int guid, string newName, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(newName))
            {
                error = "Tag name cannot be empty.";
                return false;
            }

            if (db.Entries.Any(e => e.name == newName && e.guid != guid))
            {
                error = $"A tag named '{newName}' already exists.";
                return false;
            }

            return true;
        }
    }
}