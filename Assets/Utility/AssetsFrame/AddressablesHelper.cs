using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Utility.Assetframe
{
    public static class AddressablesHelper
    {
        /// <summary>
        /// 在不卸载的情况下，对同一个资源调用LoadAssetAsync会报错。所以需要检测是否已存在Handle
        /// </summary>
        /// <param name="assetReference"></param>
        /// <param name="handle"></param>
        /// <param name="OnLoaded"></param>
        public static void LoadAssetAsyncIfValid(this AssetReference assetReference,
            out AsyncOperationHandle<Object> handle, Action<AsyncOperationHandle<Object>> OnLoaded = null)
        {
            AsyncOperationHandle op = assetReference.OperationHandle;

            if (assetReference.IsValid() && op.IsValid())
            {
                // Increase the usage counter & Convert.
                Addressables.ResourceManager.Acquire(op);
                handle = op.Convert<Object>();
                if (handle.IsDone)
                {
                    OnLoaded(handle);
                }
                else
                {
                    // Removed OnLoaded in-case it's already been added.
                    handle.Completed -= OnLoaded;
                    handle.Completed += OnLoaded;
                }
            }
            else
            {
                handle = assetReference.LoadAssetAsync<Object>();

                // Removed OnLoaded in-case it's already been added.
                handle.Completed -= OnLoaded;
                handle.Completed += OnLoaded;
            }
        }

        /// <summary>
        /// 在不卸载的情况下，对同一个资源调用LoadAssetAsync会报错。所以需要检测是否已存在Handle
        /// </summary>
        /// <param name="assetReference"></param>
        /// <param name="OnLoaded"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static AsyncOperationHandle<T> LoadAssetAsyncIfValid<T>(this AssetReference assetReference,
            Action<AsyncOperationHandle<T>> OnLoaded = null)
        {
            AsyncOperationHandle op = assetReference.OperationHandle;
            AsyncOperationHandle<T> handle = default(AsyncOperationHandle<T>);

            if (assetReference.IsValid() && op.IsValid())
            {
                // Increase the usage counter & Convert.
                Addressables.ResourceManager.Acquire(op);
                handle = op.Convert<T>();

                if (handle.IsDone)
                {
                    OnLoaded(handle);
                }
                else
                {
                    // Removed OnLoaded in-case it's already been added.
                    handle.Completed -= OnLoaded;
                    handle.Completed += OnLoaded;
                }
            }
            else
            {
                handle = assetReference.LoadAssetAsync<T>();

                // Removed OnLoaded in-case it's already been added.
                handle.Completed -= OnLoaded;
                handle.Completed += OnLoaded;
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