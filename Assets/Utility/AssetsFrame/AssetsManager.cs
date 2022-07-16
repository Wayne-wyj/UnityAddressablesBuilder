using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Singleton;
using Object = UnityEngine.Object;

namespace Utility.Assetframe
{
    public sealed class AssetsManager : Singleton<AssetsManager>
    {
        public Action OnAssetsManagerReady
        {
            set;
            get;
        }

        #region Initialize

        /// <summary>
        /// 整体资源管理器初始化
        /// </summary>
        /// <param name="onAssetLoaded"></param>
        public void InitAssetManager(Action onInitFinish)
        {
            //设置加载回调
            OnAssetsManagerReady = onInitFinish;
            //遍历Group,获取name
            Addressables.InitializeAsync().Completed += handler =>
            {
                AssetNames.Clear();
                foreach (var key in  handler.Result.Keys)
                {
                    string name = key.ToString();
                    //规则上，EntryName必须包含后缀 .XXXXX
                    if(!name.Contains("."))
                        continue;
                    AssetNames.Add(name);
                }
                //执行回调
                CheckReady();
            };
        }
        
        private void CheckReady()
        {
            OnAssetsManagerReady?.Invoke();
            OnAssetsManagerReady = null;
        }

        #endregion 

        #region Load
        
        private HashSet<string> AssetNames=new HashSet<string>();

        public bool ContainsAsset(string assetName)
        {
            return AssetNames.Contains(assetName);
        }
        
        /// <summary>
        /// 加载预制体资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="callback"></param>
        /// <param name="userData"></param>
        /// <param name="defaultParent"></param>
        public void CreatePrefabTask(string assetName, OnPrefabLoaded callback, object userData=null,Transform defaultParent=null)
        {
            if (!ContainsAsset(assetName))
            {
                Debug.LogError("No such Asset:" + assetName);
                return;
            }
            AsyncOperationHandle<GameObject>  handler= Addressables.InstantiateAsync(assetName,defaultParent);
            void CustomCallback( AsyncOperationHandle<GameObject> target)
            {
                if (target.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"资源加载失败　:{assetName}");
                    return;
                }
                callback?.Invoke(target.Result,userData);
            }
            handler.Completed += CustomCallback;
        }
        
        /// <summary>
        /// 加载非预制体,非Sprite资源（Texture,Scene,Config....）
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="callback"></param>
        /// <param name="userData"></param>
        /// <param name="defaultParent"></param>
        public void CreateAssetTask(string assetName, OnAssetLoaded callback, object userData)
        {
            if (!ContainsAsset(assetName))
            {
                Debug.LogError("No such Asset:" + assetName);
                return;
            }
            AsyncOperationHandle<Object> handler= Addressables.LoadAssetAsync<Object>(assetName);
            void CustomCallback( AsyncOperationHandle<Object> target)
            {
                if (target.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"资源加载失败　:{assetName}");
                    return;
                }
                //如果是预制体，需要优先执行额外的回调
                callback?.Invoke(target.Result,userData);
            }
            handler.Completed += CustomCallback;
        }

        /// <summary>
        /// 根据AssetReference创建Task,避免不同AssetReference的实例指向同个资源的问题
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="callback"></param>
        public AsyncOperationHandle<T> CreateAssetTask<T>(AssetReference reference, Action<AsyncOperationHandle<T>> callback) where T:Object
        {
            return reference.LoadAssetAsyncIfValid(callback);
        }
        
        /// <summary>
        /// 加载Sprite
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="callback"></param>
        /// <param name="userData"></param>
        public void CreateSpriteTask(string assetName, OnSpriteLoaded callback, object userData)
        {
            if (!ContainsAsset(assetName))
            {
                Debug.LogError("No such Asset:" + assetName);
                return;
            }
            AsyncOperationHandle<Sprite> handler= Addressables.LoadAssetAsync<Sprite>(assetName);
            void CustomCallback( AsyncOperationHandle<Sprite> target)
            {
                if (target.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"资源加载失败　:{assetName}");
                    return;
                }
                Sprite finalTarget =target.Result as Sprite;
                //如果是预制体，需要优先执行额外的回调
                callback?.Invoke(finalTarget,userData);
            }
            handler.Completed += CustomCallback;
        }
        
        /// <summary>
        /// 加载Sprite,根据AssetReference创建Task,避免不同AssetReference的实例指向同个资源的问题
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="callback"></param>
        public void CreateSpriteTask(AssetReference reference, Action<AsyncOperationHandle<Sprite>> callback)
        {
            reference.LoadAssetAsyncIfValid(callback);
        }

        #endregion
    }
    
    public delegate void OnAssetLoaded(UnityEngine.Object asset, object userData);
    
    public delegate void OnPrefabLoaded(UnityEngine.GameObject go, object userData);

    public delegate void OnSpriteLoaded(UnityEngine.Sprite sprite, object userData);

}