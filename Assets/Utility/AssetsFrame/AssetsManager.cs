using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Utility.Singleton;
using Object = UnityEngine.Object;

namespace Utility.AssetsFrame
{
    public sealed class AssetsManager : Singleton<AssetsManager>
    {
        public Action OnAssetsManagerReady
        {
            set;
            get;
        }

        #region Initialize

        private bool initialize = false;
        
        /// <summary>
        /// 整体资源管理器初始化
        /// </summary>
        /// <param name="onInitFinish">初始化完毕后回调</param>
        public void InitAssetManager(Action onInitFinish)
        {
            if (initialize)
            {
                return;
            }
            initialize = true;
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
        /// 加载预制体,异步
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="callback"></param>
        /// <param name="defaultParent"></param>
        public AsyncOperationHandle<GameObject> CreatePrefabAsync(string assetName, OnPrefabLoad callback,Transform defaultParent=null)
        {
            if (!ContainsAsset(assetName))
            {
                Debug.LogError("No such Asset:" + assetName);
                return default;
            }
            var handle= Addressables.InstantiateAsync(assetName,defaultParent);
            void CustomCallback( AsyncOperationHandle<GameObject> target)
            {
                callback?.Invoke(target.Result);
            }
            handle.Completed += CustomCallback;
            return handle;
        }    
        
        /// <summary>
        /// 加载预制体,同步
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="callback"></param>
        /// <param name="defaultParent"></param>
        public AsyncOperationHandle<GameObject> CreatePrefab(string assetName, OnPrefabLoad callback,Transform defaultParent=null)
        {
            if (!ContainsAsset(assetName))
            {
                Debug.LogError("No such Asset:" + assetName);
                return default;
            }
            var handle = Addressables.InstantiateAsync(assetName,defaultParent);
            handle.WaitForCompletion();
            callback?.Invoke(handle.Result);
            return handle;
        }    
        
        /// <summary>
        /// 加载预制体，异步
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="callback"></param>
        /// <param name="defaultParent"></param>
        public AsyncOperationHandle<GameObject> CreatePrefabAsync(AssetReference reference, OnPrefabLoad callback,Transform defaultParent=null)
        {
            var handle= reference.InstantiateAsync(defaultParent);
            void CustomCallback( AsyncOperationHandle<GameObject> target)
            {
                callback?.Invoke(target.Result);
            }
            handle.Completed += CustomCallback;
            return handle;
        }
        
        /// <summary>
        /// 加载预制体,同步
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="callback"></param>
        /// <param name="defaultParent"></param>
        public AsyncOperationHandle<GameObject> CreatePrefab(AssetReference reference, OnPrefabLoad callback,Transform defaultParent=null)
        {
            var handle= reference.InstantiateAsync(defaultParent);
            handle.WaitForCompletion();
            callback?.Invoke(handle.Result);
            return handle;
        }
        
        /// <summary>
        /// 加载非预制体,非Sprite资源,异步
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="callback"></param>
        public AsyncOperationHandle<T> CreateAssetAsync<T>(string assetName, OnAssetLoad<T> callback)where T:Object
        {
            if (!ContainsAsset(assetName))
            {
                Debug.LogError("No such Asset:" + assetName);
                return default;
            }
            //使用基类Object，可对自动适配多种类型的资源，但对于Sprite，Object会被识别为Texture类型，无法作为Sprite使用
            //所以之后创建了专门针对Sprite的加载方法
            var handle= Addressables.LoadAssetAsync<T>(assetName);
            void CustomCallback( AsyncOperationHandle<T> target)
            {
                callback?.Invoke(target.Result as T);
            }
            handle.Completed += CustomCallback;
            return handle;
        }
        
        /// <summary>
        /// 加载非预制体,非Sprite资源,同步
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="callback"></param>
        public AsyncOperationHandle<T> CreateAsset<T>(string assetName, OnAssetLoad<T> callback) where T:Object
        {
            if (!ContainsAsset(assetName))
            {
                Debug.LogError("No such Asset:" + assetName);
                return default;
            }
            //使用基类Object，可对自动适配多种类型的资源，但对于Sprite，Object会被识别为Texture类型，无法作为Sprite使用
            //所以之后创建了专门针对Sprite的加载方法
            var handle= Addressables.LoadAssetAsync<T>(assetName);
            handle.WaitForCompletion();
            callback?.Invoke(handle.Result as T);
            return handle;
        }

        /// <summary>
        /// 根据AssetReference创建Task,避免多次调用指向同一资源的AssetReference所造成的问题
        /// 异步
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="callback"></param>
        public AsyncOperationHandle<T> CreateAssetAsync<T>(AssetReference reference, OnAssetLoad<T> callback)
        {
            return reference.LoadAssetAsyncIfValid(callback);
        } 
        
        /// <summary>
        /// 根据AssetReference创建Task,避免多次调用指向同一资源的AssetReference所造成的问题
        /// 同步
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="callback"></param>
        public AsyncOperationHandle<T> CreateAsset<T>(AssetReference reference,  OnAssetLoad<T> callback)
        {
            return reference.LoadAssetIfValid(callback);
        }
        #endregion
    }

    public delegate void OnAssetLoad<T>(T asset);
    
    public delegate void OnPrefabLoad(GameObject go);

}