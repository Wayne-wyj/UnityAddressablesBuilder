using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Utility.Assetframe
{
    public class AddressablesHotfixManager : MonoBehaviour
    {
        /// <summary>
        /// 最上层调用，开始热更新流程
        /// </summary>
        /// <param name="onHotfixComplete">热更新下载完毕或确认不下载后，接续正常游戏逻辑的回调</param>
        public void StartHotfix(Action onHotfixComplete)
        {
            //整体的初始化
            Addressables.InitializeAsync().Completed += (initHandleT) =>
            {
                //下载UpdateCatalog
                UpdateCatalog(()=>
                {
                    //检测需要热更新的大小
                    CheckDownloadSize(() =>
                    {
                        //todo:可切换为UI逻辑中的确认是否下载
                        //清除本地旧版本资源，下载新资源
                        ClearCacheAndDownloadAllAsset(onHotfixComplete);
                    });
                });
            };
        }

        #region Catalog

        /// <summary>
        /// 更新Catalog
        /// </summary>
        /// <param name="onCatalogUpdateComplete"></param>
        void UpdateCatalog(Action onCatalogUpdateComplete)
        {
            var checkCatalogHandle = Addressables.CheckForCatalogUpdates();
            checkCatalogHandle.Completed += (checkUpdateHandleT) =>
            {
                if (checkUpdateHandleT.Status != AsyncOperationStatus.Succeeded)
                {
                    return;
                }
                var catalogsToUpdate = checkUpdateHandleT.Result;
                if (catalogsToUpdate.Any())
                {
                    var updateCatalogHandle = Addressables.UpdateCatalogs(catalogsToUpdate);
                    updateCatalogHandle.Completed += (updateHandleT) =>
                    {
                        onCatalogUpdateComplete?.Invoke();
                    };
                }
            };
          
        }

        #endregion

        #region Download

        /// <summary>
        /// Locator.key - AssetBundle.size ,需要下载的Key和大小
        /// </summary>
        private Dictionary<object,long> toDownloadKeySizeDict = new Dictionary<object,long>();
        /// <summary>
        /// 总共需要下载的大小
        /// </summary>
        private long totalDonwloadSize = 0;
        /// <summary>
        /// 当前已下载的大小
        /// </summary>
        private long alreadyDownloadSize = 0;

        /// <summary>
        /// 检测下载容量
        /// </summary>
        /// <param name="onCheckComplete"></param>
        void CheckDownloadSize(Action onCheckComplete)
        {
            toDownloadKeySizeDict.Clear();
            totalDonwloadSize = 0;
            alreadyDownloadSize = 0;
            int maxKeyCount = 0;
            int tempKeyCount = 0;
            foreach (var loc in Addressables.ResourceLocators)
            {
                maxKeyCount += loc.Keys.Count();
                foreach (var key in loc.Keys)
                {
                    var tempKey = key;
                    var sizeAsync = Addressables.GetDownloadSizeAsync(key);
                    sizeAsync.Completed += (sizeHandleT) =>
                    {
                        long downloadSize = sizeAsync.Result;
                        tempKeyCount++;
                        //locatorID 和 Hash 作为Key时都存在下载容量，但本质上是同一个文件，所以进行筛选,暂且只用 locatorId 检测
                        //locatorId(包含后缀): xxxxx.prefab / xxx.png
                        if (downloadSize > 0 && key.ToString().Contains('.'))
                        {
                            totalDonwloadSize+=downloadSize;
                            toDownloadKeySizeDict.Add(tempKey,downloadSize);
                        }
                        //All Handle Complete
                        if (tempKeyCount == maxKeyCount)
                        {
                            onCheckComplete?.Invoke();
                        }
                        Addressables.Release(sizeHandleT);
                    };
                }
            }
           
        }
        
        
        /// <summary>
        /// 下载所有需要下载的资源
        /// </summary>
        /// <param name="onDownloadComplete"></param>
        /// <returns></returns>
        IEnumerator DownloadAllValidKeys(Action onDownloadComplete)
        {
            foreach (var pair in toDownloadKeySizeDict)
            {
                var downloadHandle=Addressables.DownloadDependenciesAsync(pair.Key,true);
                while (!downloadHandle.IsDone)
                {
                    float percent = downloadHandle.PercentComplete;
                    //todo:刷新UI中当前下载进度
                    yield return new WaitForEndOfFrame();
                }
                alreadyDownloadSize += pair.Value;
                //todo:刷新UI中当前下载进度
            }
            onDownloadComplete?.Invoke();
        }

        #endregion

        #region ClearCache

        /// <summary>
        /// 清除本地旧版本资源，下载新资源
        /// </summary>
        /// <param name="onDownloadComplete"></param>
        void ClearCacheAndDownloadAllAsset(Action onDownloadComplete)
        {
            //First , clear old version cache.
            ClearAllAssetDependencyCache(() =>
            {
                //Then , down new version bundles.
                if (toDownloadKeySizeDict.Any())
                {
                    StartCoroutine(DownloadAllValidKeys(onDownloadComplete));
                }
                else
                {
                    onDownloadComplete?.Invoke();
                }
            });
        }

        
        /// <summary>
        /// 清除旧版本资源
        /// </summary>
        /// <param name="onClearComplete"></param>
        void ClearAllAssetDependencyCache(Action onClearComplete)
        {
           
            int maxCount = toDownloadKeySizeDict.Count();
            if (maxCount > 0)
            { 
                int curCount = 0;
                foreach (var pair in toDownloadKeySizeDict)
                {
                    var async = Addressables.ClearDependencyCacheAsync(pair.Key, true);
                    async.Completed += (handle) =>
                    {
                        curCount++;
                        if (curCount == maxCount)
                        {
                            onClearComplete?.Invoke();
                        }
                    };
                }
            }
            else
            {
                onClearComplete?.Invoke();
            }
        }

        #endregion
       
    }
}