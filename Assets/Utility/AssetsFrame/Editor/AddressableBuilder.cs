using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        //所有Group,缓存备用
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
        /// 原生工具清理出的重复引用所在Group,需要用户自行处理
        /// </summary>
        private static AddressableAssetGroup duplicateAssetIsolationGroup;

        private static readonly string assetSettingsPath =
            "Assets/AddressableAssetsData/AddressableAssetSettings.asset";
        /// <summary>
        /// Asset设置,记录Group等信息
        /// </summary>
        private static AddressableAssetSettings assetSettings;

        private static readonly string groupFolderSettingPath =
            "Assets/Utility/AssetsFrame/Setting/AddressableGroupFolderSetting.asset";
        /// <summary>
        /// 分组设置
        /// </summary>
        private static AddressableGroupFolderSetting groupFolderSetting;


        #region Setting,Group

        /// <summary>
        /// 获取assetSetting,groupSetting各个Group
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
            TryGetGroup(localStaticSeperatelyName, out localStaticSeperatelyGroup);
            TryGetGroup(localStaticTogetherName, out localStaticTogetherGroup);
            TryGetGroup(localStaticLabelName, out localStaticLabelGroup);
            TryGetGroup(remoteNonStaticSeperatelyName, out remoteNonStaticSeperatelyGroup);
            TryGetGroup(remoteNonStaticTogetherName, out remoteNonStaticTogetherGroup);
            TryGetGroup(remoteNonStaticLabelName, out remoteNonStaticLabelGroup);
            TryGetGroup(remoteStaticSeperatelyName, out remoteStaticSeperatelyGroup);
            TryGetGroup(remoteStaticTogetherName, out remoteStaticTogetherGroup);
            TryGetGroup(remoteStaticLabelName, out remoteStaticLabelGroup);
            TryGetGroup(duplicateAssetIsolationGroupName, out duplicateAssetIsolationGroup);
            TryGetGroup(builtInDataGroupName, out builtInDataGroup);
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
        /// 创建新的AssetGroup
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="groupName"></param>
        /// <typeparam name="SchemaType"></typeparam>
        /// <returns></returns>
        private static AddressableAssetGroup CreateAssetGroup(string groupName)
        {
            return assetSettings.CreateGroup(groupName, false, false, false,
                new List<AddressableAssetGroupSchema> {assetSettings.DefaultGroup.Schemas[0]},
                typeof(BundledAssetGroupSchema));
        }

        /// <summary>
        /// 根据名字获取对应的AssetGroup
        /// </summary>
        /// <param name="settings">Reference to the <see cref="AddressableAssetSettings" /></param>
        /// <param name="groupName">The name of the group for the search.</param>
        /// <param name="group">The <see cref="AddressableAssetGroup" /> if found. Set to <see cref="null" /> if not found.</param>
        /// <returns>True if a group is found.</returns>
        private static bool TryGetGroup(string groupName, out AddressableAssetGroup group)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                group = assetSettings.DefaultGroup;
                return true;
            }

            return (group = assetSettings.groups.Find(g => string.Equals(g.Name, groupName.Trim()))) != null;
        }

        #endregion

        #region Build

        /// <summary>
        ///     标记原有的RawBundles目录至资源组中
        /// </summary>
        private static void MarkAssetsIntoGroup()
        {
            int totalCount = 0, tempCount = 0;

            //groupSetting,遍历各个文件夹，分别标记
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


            //检查冗余资源后，需要手动处理Label和Bundle设置，不再自动Build
            //结束标记
            EditorUtility.ClearProgressBar();
            Debug.Log("Mark Finish !");
        }

        #region GroupMark

        /// <summary>
        /// 标记一个folderSetting中的资源
        /// </summary>
        /// <param name="setting"></param>
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
                            MarkFolder(setting.Group, folder.Reference.MarkPath,
                                folder.GetLabelName(folderSetting.LabelType),
                                suffixFilter: folderSetting.GetSuffixFilter());
                        }
                    }
                }
            }
        }

        #endregion

        #region Common Methods

        /// <summary>
        /// 标记整个文件夹中的资源（包含子文件夹）
        /// </summary>
        /// <param name="group"></param>
        /// <param name="folderPath"></param>
        /// <param name="label"></param>
        /// <param name="singleCallback">每个Object执行的回调</param>
        /// <param name="allCallback">single执行完后，统一执行的回调</param>
        /// <param name="suffixFilter">后缀限定</param>
        private static void MarkFolder(AddressableAssetGroup group, string folderPath, string label,
            System.Action<Object> singleCallback = null, System.Action<IList<Object>> allCallback = null,
            List<string> suffixFilter = null)
        {
            var allAssets = GetAssetsAtPath(folderPath);
            List<Object> toRemoveAssets = new List<Object>();
            if (suffixFilter != null && suffixFilter.Count > 0)
            {
                for (int i = allAssets.Count - 1; i >= 0; i--)
                {
                    var asset = allAssets[i];
                    string path = AssetDatabase.GetAssetPath(asset);
                    string[] strs = path.Split('.');
                    string suffix = strs[strs.Length - 1];
                    if (suffixFilter.Contains(suffix))
                    {
                        toRemoveAssets.Add(allAssets[i]);
                        allAssets.RemoveAt(i);
                    }
                }
            }
            MarkAssetToGroup(group, allAssets, label, singleCallback, allCallback);
            ClearEntries(toRemoveAssets);
        }

        /// <summary>
        /// 将资源List标记进入Group中
        /// </summary>
        /// <param name="group"></param>
        /// <param name="objs"></param>
        /// <param name="labelName"></param>
        /// <param name="singleCallback"></param>
        /// <param name="allCallback"></param>
        /// <param name="forceToOverrideLabel"></param>
        private static void MarkAssetToGroup(AddressableAssetGroup group, IList<Object> objs, string labelName = "",
            System.Action<Object> singleCallback = null, System.Action<IList<Object>> allCallback = null)
        {
            foreach (var asset in objs)
            {
                MarkAssetToGroup(@group, asset, labelName, singleCallback);
            }

            allCallback?.Invoke(objs);
        }
        
        /// <summary>
        /// 需要额外排除的Asset名字
        /// </summary>
        private static readonly string exceptionAssetName = "unity default resources";

        /// <summary>
        /// 将单个资源标记进入Group中
        /// </summary>
        /// <param name="group"></param>
        /// <param name="asset"></param>
        /// <param name="labelName"></param>
        /// <param name="singleCallback"></param>
        private static void MarkAssetToGroup(AddressableAssetGroup group, Object asset, string labelName = "",
            System.Action<Object> singleCallback = null)
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
                {
                    singleCallback?.Invoke(asset);
                    return;
                }
                entry = assetSettings.CreateOrMoveEntry(guid, @group);
                entry.address = fullNameWithSuffix;
            }
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
            singleCallback?.Invoke(asset);
        }

        /// <summary>
        /// 资源缓存，用以返回数据
        /// </summary>
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

        /// <summary>
        /// 路径缓存，用以返回数据
        /// </summary>
        private static readonly IList<string> m_CachePaths = new List<string>();

        /// <summary>
        /// 获取文件夹下的所有Leaf文件夹路径
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="maxDepth"></param>
        /// <returns></returns>
        public static IList<string> GetLeafPaths(string folderPath, int maxDepth = 1)
        {
            m_CachePaths.Clear();

            void SearchSubFolder(string curPath, int curDepth)
            {
                string[] folderPaths = Directory.GetDirectories(curPath);
                curDepth++;
                if (folderPaths.Length == 0 || curDepth > maxDepth)
                {
                    m_CachePaths.Add(curPath.Replace("\\", "/"));
                    return;
                }

                foreach (var tempPath in folderPaths)
                {
                    SearchSubFolder(tempPath.Replace(Application.dataPath, "Assets/"), curDepth);
                }
            }

            SearchSubFolder(folderPath.StartsWith("Assets/") ? folderPath : "Assets/" + folderPath, 1);
            return m_CachePaths;
        }

        /// <summary>
        /// 清除Group中，所有Entry
        /// </summary>
        /// <param name="group"></param>
        static void ClearEntries(AddressableAssetGroup group)
        {
            if (group == null || group.entries == null)
            {
                return;
            }

            var entryList = group.entries.ToList();
            foreach (var entry in entryList)
            {
                group.RemoveAssetEntry(entry);
            }
        }

        /// <summary>
        /// 清除资源List对应的Entry
        /// </summary>
        /// <param name="assets"></param>
        static void ClearEntries(List<Object> assets)
        {
            if (assetSettings.groups == null || assets ==null || assets.Count==0)
            {
                return;
            }
            foreach (var asset in assets)
            {
                var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
                foreach (var group in assetSettings.groups)
                {
                    var entry= group.GetAssetEntry(guid);
                    if (entry != null)
                    {
                        group.RemoveAssetEntry(entry);
                        break;
                    }
                }
            }
        }

        #endregion

        #endregion


    }
}