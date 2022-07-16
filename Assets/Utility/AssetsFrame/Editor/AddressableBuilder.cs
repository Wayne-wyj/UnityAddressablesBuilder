using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Utility.Assetframe.Editor
{
    public static partial class AddressableBuilder
    {
        private const string BundleRootPath = "RawBundles/";
        private static readonly string localStaticSeperatelyName = "Local_Static_PackSeperately";
        private static AddressableAssetGroup localStaticSeperatelyGroup;
        private static readonly string localStaticTogetherName = "Local_Static_PackTogether";
        private static AddressableAssetGroup localStaticTogetherGroup;
        private static readonly string localStaticLabelName = "Local_Static_PackTogetherByLabel";
        private static AddressableAssetGroup localStaticLabelGroup;
        private static readonly string remoteNonStaticSeperatelyName = "Remote_NonStatic_PackSeperately";
        private static AddressableAssetGroup remoteNonStaticSeperatelyGroup;
        private static readonly string remoteNonStaticTogetherName = "Remote_NonStatic_PackTogether";
        private static AddressableAssetGroup remoteNonStaticTogetherGroup;
        private static readonly string remoteNonStaticLabelName = "Remote_NonStatic_PackTogetherByLabel";
        private static AddressableAssetGroup remoteNonStaticLabelGroup;
        private static readonly string remoteStaticSeperatelyName = "Remote_Static_PackSeperately";
        private static AddressableAssetGroup remoteStaticSeperatelyGroup;
        private static readonly string remoteStaticTogetherName = "Remote_Static_PackTogether";
        private static AddressableAssetGroup remoteStaticTogetherGroup;
        private static readonly string remoteStaticLabelName = "Remote_Static_PackTogetherByLabel";
        private static AddressableAssetGroup remoteStaticLabelGroup;
        private static readonly string builtInDataGroupName = "Built In Data";
        private static AddressableAssetGroup builtInDataGroup;
        private static readonly string duplicateAssetIsolationGroupName = "Duplicate Asset Isolation";
        /// <summary>
        /// 由于重复引用，通过设置Label分为多个小型bundle的组
        /// </summary>
        private static AddressableAssetGroup duplicateAssetIsolationGroup;

        /// <summary>
        /// 查找引用时，引用Unity原生资源名
        /// </summary>
        //private static readonly string exceptionAssetName = "unity default resources";
        private static readonly string assetSettingsPath =
            "Assets/AddressableAssetsData/AddressableAssetSettings.asset";

        private static readonly string groupFolderSettingPath =
            "Assets/Utility/AssetsFrame/Setting/AddressableGroupFolderSetting.asset";

        /// <summary>
        /// 分组配置
        /// </summary>
        private static AddressableGroupFolderSetting groupFolderSetting;

        private static AddressableAssetSettings assetSettings;

        #region Setting,Group

        /// <summary>
        /// 获取assetSetting和Group
        /// </summary>
        /// <param name="onUnityInit">Unity是否刚打开</param>
        public static void CheckSettingsAndGroup(bool onUnityInit = false)
        {
            CheckGroupFolderSettings();

            if (assetSettings != null)
            {
                return;
            }
            if (onUnityInit)
                assetSettings =
                    AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(assetSettingsPath);
            else
                assetSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            TryGetGroup(assetSettings, localStaticSeperatelyName, out localStaticSeperatelyGroup);
            TryGetGroup(assetSettings, localStaticTogetherName, out localStaticTogetherGroup);
            TryGetGroup(assetSettings, localStaticLabelName, out localStaticLabelGroup);
            TryGetGroup(assetSettings, remoteNonStaticSeperatelyName, out remoteNonStaticSeperatelyGroup);
            TryGetGroup(assetSettings, remoteNonStaticTogetherName, out remoteNonStaticTogetherGroup);
            TryGetGroup(assetSettings, remoteNonStaticLabelName, out remoteNonStaticLabelGroup);
            TryGetGroup(assetSettings, remoteStaticSeperatelyName, out remoteStaticSeperatelyGroup);
            TryGetGroup(assetSettings, remoteStaticTogetherName, out remoteStaticTogetherGroup);
            TryGetGroup(assetSettings, remoteStaticLabelName, out remoteStaticLabelGroup);
            TryGetGroup(assetSettings, duplicateAssetIsolationGroupName, out duplicateAssetIsolationGroup);
            TryGetGroup(assetSettings, builtInDataGroupName, out builtInDataGroup);

        }

        /// <summary>
        /// 获取groupSetting
        /// </summary>
        public static void CheckGroupFolderSettings()
        {
            if (groupFolderSetting != null)
            {
                return;
            }

            groupFolderSetting =
                    AssetDatabase.LoadAssetAtPath<AddressableGroupFolderSetting>(groupFolderSettingPath);
        }

        /// <summary>
        ///     创建新的AssetGroup
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="groupName"></param>
        /// <typeparam name="SchemaType"></typeparam>
        /// <returns></returns>
        private static AddressableAssetGroup CreateAssetGroup(AddressableAssetSettings settings, string groupName)
        {
            return settings.CreateGroup(groupName, false, false, false,
                new List<AddressableAssetGroupSchema> {settings.DefaultGroup.Schemas[0]},
                typeof(BundledAssetGroupSchema));
        }

        /// <summary>
        ///     尝试根据名字获取对应的AssetGroup
        /// </summary>
        /// <param name="settings">Reference to the <see cref="AddressableAssetSettings" /></param>
        /// <param name="groupName">The name of the group for the search.</param>
        /// <param name="group">The <see cref="AddressableAssetGroup" /> if found. Set to <see cref="null" /> if not found.</param>
        /// <returns>True if a group is found.</returns>
        private static bool TryGetGroup(AddressableAssetSettings settings, string groupName,
            out AddressableAssetGroup group)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            { 
                group = settings.DefaultGroup;
                return true;
            }

            return (@group = settings.groups.Find(g => string.Equals(g.Name, groupName.Trim()))) == null ? false : true;
        }

        #endregion

        #region Build

        /// <summary>
        ///     标记原有的RawBundles目录至资源组中
        /// </summary>
        private static void MarkAllRawBundlesToGroup()
        {
            int totalCount = 1, tempCount = 0;

            if (groupFolderSetting)
            {
                var length = (groupFolderSetting.Settings != null) ? groupFolderSetting.Settings.Length : 0;
                totalCount += length;
                foreach (var setting in groupFolderSetting.Settings)
                {
                    EditorUtility.DisplayProgressBar("Mark State :", setting.GroupName,
                        (float) ++tempCount / totalCount);
                    MarkGroupSetting(setting);
                }
            }

            EditorUtility.DisplayProgressBar("Mark State :", "Mark finished", (float) ++tempCount / totalCount);

            //检查冗余资源后，需要手动处理Label和Bundle设置，不再自动Build
            //EditorUtility.DisplayProgressBar("Reset Duplicate Asset", "重设重复资源的Label", (float) ++tempCount / totalCount);
            //MarkDuplicateAssetIsolation();

            EditorUtility.ClearProgressBar();
        }

        #region GroupMark

        private static void MarkGroupSetting(GroupFolderSetting setting)
        {
            if (setting.Group == null)
            {
                return;
            }

            if (setting.FolderSettings != null)
            {
                foreach (var folderSetting in setting.FolderSettings)
                {
                    if (folderSetting.Folders != null)
                    {
                        foreach (var folder in folderSetting.Folders)
                        {
                            switch (folderSetting.LabelType)
                            {
                                //无Label
                                case LabelType.None:
                                    MarkFolderSeperately(setting.Group, folder.Reference.MarkPath);
                                    break;
                                //不同的Label命名
                                case LabelType.Custom:
                                case LabelType.SameAsFolder:
                                case LabelType.FloderWithPrefixAndSuffix:
                                    MarkFolderTogether(setting.Group, folder.Reference.MarkPath,
                                        folder.GetLabelName(folderSetting.LabelType),
                                        suffixFilter: folderSetting.GetSuffixFilter());
                                    break;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Common Methods

        /// <summary>
        ///     将整个文件夹标记为一个Label
        /// </summary>
        /// <param name="group"></param>
        /// <param name="folderPath"></param>
        /// <param name="label"></param>
        /// <param name="singleCallback">每个Object执行的回调</param>
        /// <param name="allCallback">single执行完后，统一执行的回调</param>
        /// <param name="suffixFilter">后缀限定</param>
        private static void MarkFolderTogether(AddressableAssetGroup group, string folderPath, string label,
            System.Action<Object> singleCallback = null, System.Action<IList<Object>> allCallback = null,
            List<string> suffixFilter = null)
        {
            var allAssets = GetAssetsAtPath(folderPath);
            if (suffixFilter != null && suffixFilter.Count>0)
            {
                for (int i = allAssets.Count - 1; i >= 0; i--)
                {
                    var asset = allAssets[i];
                    string path = AssetDatabase.GetAssetPath(asset);
                    string[] strs = path.Split('.');
                    if (!suffixFilter.Contains(strs[strs.Length - 1]))
                    {
                        allAssets.RemoveAt(i);
                    }
                }
            }

            MarkAssetToGroup(group, allAssets, label, singleCallback, allCallback);
        }

        /// <summary>
        ///     将文件夹下各个资源分别标记(不需要Label)
        /// </summary>
        private static void MarkFolderSeperately(AddressableAssetGroup group, string folderPath,
            System.Action<Object> singleCallback = null, System.Action<IList<Object>> allCallback = null)
        {
            var allAssets = GetAssetsAtPath(folderPath);
            MarkAssetToGroup(group, allAssets, "", singleCallback, allCallback, forceToOverrideLabel: true);
        }


        private static void MarkAssetToGroup(AddressableAssetGroup group, IList<Object> objs, string labelName = "",
            System.Action<Object> singleCallback = null, System.Action<IList<Object>> allCallback = null,
            bool forceToOverrideLabel = false)
        {
            foreach (var asset in objs)
            {
                MarkAssetToGroup(@group, asset, labelName, singleCallback, forceToOverrideLabel);
            }

            allCallback?.Invoke(objs);
        }

        private static void MarkAssetToGroup(AddressableAssetGroup group, Object asset, string labelName = "",
            System.Action<Object> singleCallback = null, bool forceToOverrideLabel = false)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            //当前组是否含有该entry
            var entry = group.GetAssetEntry(guid);
            var strs = assetPath.Split('/');
            var fullNameWithSuffix = strs[strs.Length - 1];
            if (entry == null || entry.address != fullNameWithSuffix)
            {
                //剔除unity default resource的引用
                if (fullNameWithSuffix == exceptionAssetName)
                    return;
                entry = assetSettings.CreateOrMoveEntry(guid, @group);
                entry.address = fullNameWithSuffix;
            }

            singleCallback?.Invoke(asset);
            if (string.IsNullOrEmpty(labelName) && !forceToOverrideLabel)
                return;
            if (string.IsNullOrEmpty(labelName))
            {
                entry.labels.Clear();
            }
            else
            {
                var curLabels = assetSettings.GetLabels();
                if (!curLabels.Contains(labelName))
                    assetSettings.AddLabel(labelName);
                entry.labels.Clear();
                entry.labels.Add(labelName);
            }
        }

        private static readonly IList<Object> m_CacheAssets = new List<Object>();

        public static IList<Object> GetAssetsAtPath(string path)
        {
            var fileEntries = Directory.GetFiles(Application.dataPath + "/" + path, "*.*", SearchOption.AllDirectories);
            if (fileEntries.Length <= 0)
                return Array.Empty<Object>();
            m_CacheAssets.Clear();
            foreach (var fileName in fileEntries)
            {
                if (fileName.EndsWith(".meta"))
                    continue;
                var t = AssetDatabase.LoadMainAssetAtPath(fileName.Replace(Application.dataPath, "Assets")
                    .Replace("\\", "/"));
                if (t != null) m_CacheAssets.Add(t);
            }

            return m_CacheAssets;
        }

        private static readonly IList<string> m_CachePaths = new List<string>();


        public static IList<string> GetLeafPathsAtPath(string rootPath, int maxLayer = 1)
        {
            m_CachePaths.Clear();

            void SearchSubFolder(string curPath, int curLayer)
            {
                string[] folderPaths = Directory.GetDirectories(curPath);
                curLayer++;
                if (folderPaths.Length == 0 || curLayer > maxLayer)
                {
                    m_CachePaths.Add(curPath.Replace("\\", "/"));
                    return;
                }

                foreach (var tempPath in folderPaths)
                {
                    SearchSubFolder(tempPath.Replace(Application.dataPath, "Assets/"), curLayer);
                }
            }

            SearchSubFolder(rootPath.StartsWith("Assets/") ? rootPath : "Assets/" + rootPath, 1);
            return m_CachePaths;
        }

        #endregion

        #region Dumplicate Asset

        private static void MarkDuplicateAssetIsolation()
        {
        }

        #endregion

        #endregion


        private static readonly string exceptionAssetName = "unity default resources";

        private static void MarkAssetToGroup(AddressableAssetSettings addressableSettings, AddressableAssetGroup group,
            Object asset, string labelName = "")
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            //当前组是否含有该entry
            var entry = group.GetAssetEntry(guid);
            var strs = assetPath.Split('/');
            var fullNameWithSuffix = strs[strs.Length - 1];
            if (entry == null || entry.address != fullNameWithSuffix)
            {
                //剔除unity default resource的引用
                if (fullNameWithSuffix == exceptionAssetName)
                    return;
                //
                entry = addressableSettings.CreateOrMoveEntry(guid, @group);
                entry.address = fullNameWithSuffix;
            }

            if (string.IsNullOrEmpty(labelName))
            {
                entry.labels.Clear();
            }
            else
            {
                var curLabels = addressableSettings.GetLabels();
                if (!curLabels.Contains(labelName))
                    addressableSettings.AddLabel(labelName);
                entry.labels.Clear();
                entry.labels.Add(labelName);
            }
        }
    }
}