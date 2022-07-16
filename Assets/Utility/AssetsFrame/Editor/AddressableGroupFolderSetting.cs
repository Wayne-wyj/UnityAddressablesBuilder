using System;
using System.Collections.Generic;
using InspectorHelper;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Serialization;

namespace Utility.Assetframe.Editor
{
    [CreateAssetMenu(fileName = "AddressableGroupPathSetting",
        menuName = "Addressables/AddressableBuilder/AddressableGroupPathSetting")]
    public class AddressableGroupFolderSetting : ScriptableObject
    {
        public GroupFolderSetting[] Settings;
    }

    [Serializable]
    public struct GroupFolderSetting
    {
        public AddressableAssetGroup Group;
        [FormerlySerializedAs("PathSettings")] public FolderSetting[] FolderSettings;

        public string GroupName
        {
            get
            {
                return Group != null ? Group.name : "Group  is  Null !!";
            }
        }
    }

    [Serializable]
    public struct FolderSetting
    {
#if UNITY_EDITOR
        [Header("Only For Editor")] public string Title;
#endif
        [Header("Runtime")] public LabelType LabelType;
        public FolderData[] Folders;
        public SuffixType SuffixFilter;

        public List<string> GetSuffixFilter()
        {
            List<string> result = new List<string>();
            var enumArray = Enum.GetValues(typeof(SuffixType));
            for (int i = 0; i < enumArray.Length; i++)
            {
                if(((int)SuffixFilter & (1<<i)) >0)
                {
                    SuffixType type = (SuffixType) (1 << i);
                    result.Add(type.ToString());
                }
            }

            return result;
        }
    }

    [Serializable]
    public struct FolderData : IEquatable<FolderData>
    {
        public FolderReference Reference;
        [HideInInspector]
        public string CustomLabelName;
        [HideInInspector]
        public string PrefixLabelName;
        [HideInInspector]
        public string SuffixLabelName;
        [HideInInspector]
        [ReadOnly]
        public string PreviewLabelName;
        /// <summary>
        /// 预览Label的名字
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public string GetLabelName(LabelType type)
        {
            switch (type)
            {
                case LabelType.None:

                    break;
                case LabelType.SameAsFolder:
                    string[] pathStrs1 = Reference.InspectorPath.Split('/');
                    return pathStrs1[pathStrs1.Length - 1];
                case LabelType.FloderWithPrefixAndSuffix:
                    string[] pathStrs2 = Reference.InspectorPath.Split('/');
                    return $"{PrefixLabelName}{pathStrs2[pathStrs2.Length - 1]}{SuffixLabelName}";
                case LabelType.Custom:
                    return CustomLabelName;
            }

            return string.Empty;
        }

        public bool Equals(FolderData other)
        {
            return Reference.Equals(other.Reference) && CustomLabelName == other.CustomLabelName &&
                   PrefixLabelName == other.PrefixLabelName && SuffixLabelName == other.SuffixLabelName;
        }

        public override bool Equals(object obj)
        {
            return obj is FolderData other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Reference.GetHashCode();
                hashCode = (hashCode * 397) ^ (CustomLabelName != null ? CustomLabelName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PrefixLabelName != null ? PrefixLabelName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SuffixLabelName != null ? SuffixLabelName.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    [Flags]
    public enum SuffixType
    {
        /// <summary>
        /// 带Flag枚举 的默认选项，逻辑中默认设置为Prefab
        /// </summary>
        Prefab = 1<<0,
        Png = 1<<1,
        Material = 1<<2,
    }

    /// <summary>
    /// Label名字的类型
    /// </summary>
    public enum LabelType
    {
        /// <summary>
        /// 不存在Label
        /// </summary>
        None,

        /// <summary>
        /// 与文件夹名相同
        /// </summary>
        SameAsFolder,

        /// <summary>
        /// 文件夹名+前后缀
        /// </summary>
        FloderWithPrefixAndSuffix,

        /// <summary>
        /// 完全自定义
        /// </summary>
        Custom
    }
}