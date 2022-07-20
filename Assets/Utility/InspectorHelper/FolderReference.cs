
namespace Utility.InspectorHelper
{
    [System.Serializable]
    public struct FolderReference
    {
        public string GUID;

#if UNITY_EDITOR
        /// <summary>
        /// Path For Inspector  Assets/xxxxxx
        /// </summary>
        public string InspectorPath => UnityEditor.AssetDatabase.GUIDToAssetPath(GUID);

        /// <summary>
        /// Path For Mark   Assets/xxxxxx ,  delete "Assets"
        /// </summary>
        public string MarkPath => InspectorPath.Remove(0, 6);
#endif
    }
}
