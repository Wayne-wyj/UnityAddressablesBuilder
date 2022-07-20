using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Utility.AssetsFrame
{
    public static class AddressablesHelper
    {
        /// <summary>
        /// 在不卸载的情况下，对同一个资源调用LoadAssetAsync会报错。所以需要检测是否已存在Handle
        /// 同一个Reference检测Handle结果，异步
        /// </summary>
        /// <param name="assetReference"></param>
        /// <param name="OnLoaded"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static AsyncOperationHandle<T> LoadAssetAsyncIfValid<T>(this AssetReference assetReference,OnAssetLoad<T> OnLoaded = null)
        {
            AsyncOperationHandle op = assetReference.OperationHandle;
            AsyncOperationHandle<T> handle = default(AsyncOperationHandle<T>);
            //如果已释放Handle,则永远不会进入
            void OnHandle( AsyncOperationHandle<T> target)
            {
                OnLoaded?.Invoke(target.Result);
            }
            if (assetReference.IsValid() && op.IsValid())
            {
                // 增加引用计数,转换为handle
                Addressables.ResourceManager.Acquire(op);
                handle = op.Convert<T>();

                if (handle.IsDone)
                {
                    OnLoaded?.Invoke(handle.Result);
                }
                else
                {
                    handle.Completed -= OnHandle;
                    handle.Completed += OnHandle;
                }
            }
            //已释放Handle或从未加载过时，需要重新加载
            else
            {
                handle = assetReference.LoadAssetAsync<T>();

                handle.Completed -= OnHandle;
                handle.Completed += OnHandle;
            }

            return handle;
        }
        
        /// <summary>
        /// 在不卸载的情况下，对同一个资源调用LoadAssetAsync会报错。所以需要检测是否已存在Handle
        /// 同一个Reference检测Handle结果，同步
        /// </summary>
        /// <param name="assetReference"></param>
        /// <param name="OnLoaded"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static AsyncOperationHandle<T> LoadAssetIfValid<T>(this AssetReference assetReference,OnAssetLoad<T> OnLoaded = null)
        {
            AsyncOperationHandle op = assetReference.OperationHandle;
            AsyncOperationHandle<T> handle = default(AsyncOperationHandle<T>);
            //如果已释放Handle,则永远不会进入
            if (assetReference.IsValid() && op.IsValid())
            {
                // 增加引用计数,转换为handle
                Addressables.ResourceManager.Acquire(op);
                handle = op.Convert<T>();
                if (!handle.IsDone)
                {
                    handle.WaitForCompletion();
                }
                OnLoaded?.Invoke(handle.Result);
            }
            //已释放Handle或从未加载过时，需要重新加载
            else
            {
                handle = assetReference.LoadAssetAsync<T>();
                if (!handle.IsDone)
                {
                    handle.WaitForCompletion();
                }
                OnLoaded?.Invoke(handle.Result);
            }
            return handle;
        }

        /// <summary>
        /// 是否可加载
        /// </summary>
        /// <param name="assetReference"></param>
        /// <returns></returns>
        public static bool IsUsable(this AssetReference assetReference)
        {
            return assetReference != null && !string.IsNullOrEmpty(assetReference.AssetGUID);
        }

        /// <summary>
        /// 是否指向同一资源
        /// </summary>
        /// <param name="assetReference"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool SameAsset(this AssetReference assetReference, AssetReference target)
        {
            return assetReference.IsUsable() && assetReference.AssetGUID == target.AssetGUID;
        }
    }
}