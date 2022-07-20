using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline.Utilities;

namespace Utility.AssetsFrame.Editor
{
    [InitializeOnLoad]
    public static partial class AddressableBuilder
    {
        #region BuildTool

        static AddressableBuilder()
        {
            EditorApplication.update += InitByUpdate;
        }

        /// <summary>
        /// 刚打开编辑器时，初始化运行工具
        /// </summary>
        public static void InitByUpdate()
        {
            EditorApplication.update -= InitByUpdate;
            CheckSettingsAndGroup(true);
            if (assetSettings == null)
            {
                return;
            }
            switch (assetSettings.ActivePlayModeDataBuilderIndex)
            {
                case 0:
                    UseAssetDatabase();
                    break;
                case 1:
                    SimulateGroup();
                    break;
                case 2:
                    UseExistingBundles();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 标记所有的Addressble
        /// </summary>
        [MenuItem("Tools/Addressables/Mark or Build/Mark Asset(For Editor)")]
        public static void MarkAllAssets()
        {
            CheckSettingsAndGroup();
            //RawBundle,Mark
            MarkAssetsIntoGroup();
            //保存配置
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 保留Group,清空所有Entry
        /// </summary>
        [MenuItem("Tools/Addressables/Mark or Build/Reload Groups and Clear Entries")]
        public static void ClearAllEntries()
        {
            CheckGroupFolderSettings();
            if (groupFolderSetting != null)
            {
                foreach (var group in groupFolderSetting.Groups)
                {
                    ClearEntries(group);
                }
            }
            //builtin不清除.,duplicate清除
            ClearEntries(duplicateAssetIsolationGroup);
        }

        [MenuItem("Tools/Addressables/Mark or Build/New Build(Bundle)")]
        public static void DefaultBuildScript()
        {
            MarkAllAssets();
            AddressableAssetSettings.BuildPlayerContent();
        }

        [MenuItem("Tools/Addressables/Mark or Build/Clean Build Cache and New Build(Bundle)")]
        public static void ClearAndBuild()
        {
            CleanAllBuild();
            MarkAllAssets();
            AddressableAssetSettings.BuildPlayerContent();
        }

        private static void CleanAllBuild()
        {
            AddressableAssetSettings.CleanPlayerContent(null);
            //清空build缓存
            BuildCache.PurgeCache(true);
            //todo:考虑清空RemoteBuild
        }

        private static int selectModeIndex = -1;

        [MenuItem("Tools/Addressables/Choose Play Mode Script/Use AssetDatabase")]
        public static void UseAssetDatabase()
        {
            CheckSettingsAndGroup();
            UnityEditor.Menu.SetChecked("Tools/Addressables/Choose Play Mode Script/Use AssetDatabase", true);
            UnityEditor.Menu.SetChecked("Tools/Addressables/Choose Play Mode Script/Simulate Group", false);
            UnityEditor.Menu.SetChecked("Tools/Addressables/Choose Play Mode Script/Use ExistingBundles", false);
            selectModeIndex = 0;
            assetSettings.ActivePlayModeDataBuilderIndex = selectModeIndex;
        }

        [MenuItem("Tools/Addressables/Choose Play Mode Script/Simulate Group")]
        public static void SimulateGroup()
        {
            CheckSettingsAndGroup();
            UnityEditor.Menu.SetChecked("Tools/Addressables/Choose Play Mode Script/Use AssetDatabase", false);
            UnityEditor.Menu.SetChecked("Tools/Addressables/Choose Play Mode Script/Simulate Group", true);
            UnityEditor.Menu.SetChecked("Tools/Addressables/Choose Play Mode Script/Use ExistingBundles", false);
            selectModeIndex = 1;
            assetSettings.ActivePlayModeDataBuilderIndex = selectModeIndex;
        }

        [MenuItem("Tools/Addressables/Choose Play Mode Script/Use ExistingBundles")]
        public static void UseExistingBundles()
        {
            CheckSettingsAndGroup();
            UnityEditor.Menu.SetChecked("Tools/Addressables/Choose Play Mode Script/Use AssetDatabase", false);
            UnityEditor.Menu.SetChecked("Tools/Addressables/Choose Play Mode Script/Simulate Group", false);
            UnityEditor.Menu.SetChecked("Tools/Addressables/Choose Play Mode Script/Use ExistingBundles", true);
            selectModeIndex = 2;
            assetSettings.ActivePlayModeDataBuilderIndex = selectModeIndex;
        }

        #endregion
    }
}