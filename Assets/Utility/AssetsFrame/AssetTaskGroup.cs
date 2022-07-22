using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Utility.AssetsFrame
{
    /// <summary>
    /// 资源加载组，必然为异步加载
    /// </summary>
    public sealed class AssetTaskGroup : IDisposable
    {
        private class GroupInfo
        {
            public int Counter;
            public string AssetName;
            public AssetReference Reference;
            public OnAssetLoadHandle<Sprite> OnSpriteLoad;
            public OnAssetLoadHandle<Object> OnAssetLoad;
            public OnPrefabLoadHandle OnPrefabLoad;
            public Transform Parent;
            public bool IsPrefab;
            public bool IsSprite;
            public AsyncOperationHandle<GameObject> goHandle;
            public AsyncOperationHandle<Sprite> spriteHandle;
            public AsyncOperationHandle<Object> assetHandle;
        }

        private Action allLoaded;
        private Dictionary<int, GroupInfo> dict = new Dictionary<int, GroupInfo>();

        public int LeftCount => dict.Count;

        private int taskCounter = 1;

        public static AssetTaskGroup New()
        {
            return new AssetTaskGroup();
        }

        /// <summary>
        /// 新增加载任务
        /// </summary>
        /// <param name="assetName">资源名</param>
        /// <param name="onPrefabLoad">prefab回调</param>
        /// <param name="onAssetLoad">asset回调</param>
        /// <param name="parent">prefab挂载的Parent</param>
        /// <typeparam name="T">具体类型 GameObject/Sprite/Object</typeparam>
        /// <returns></returns>
        public AssetTaskGroup Add<T>(string assetName, OnAssetLoadHandle<Object> onAssetLoad = null,
            OnAssetLoadHandle<Sprite> onSpriteLoad = null,
            OnPrefabLoadHandle onPrefabLoad = null, Transform parent = null)
        {
            dict.Add(taskCounter, new GroupInfo()
            {
                Counter = taskCounter,
                AssetName = assetName,
                OnPrefabLoad = onPrefabLoad,
                OnAssetLoad = onAssetLoad,
                OnSpriteLoad = onSpriteLoad,
                Parent = parent,
                IsPrefab = typeof(T) == typeof(GameObject),
                IsSprite = typeof(T) == typeof(Sprite),
            });
            taskCounter++;
            return this;
        }

        /// <summary>
        /// 新增加载任务
        /// </summary>
        /// <param name="reference">资源引用</param>
        /// <param name="onPrefabLoad">prefab回调</param>
        /// <param name="onAssetLoad">asset回调</param>
        /// <param name="parent">prefab挂载的Parent</param>
        /// <typeparam name="T">具体类型 GameObject/Sprite/Object</typeparam>
        /// <returns></returns>
        public AssetTaskGroup Add<T>(AssetReference reference, OnAssetLoadHandle<Object> onAssetLoad = null,
            OnPrefabLoadHandle onPrefabLoad = null, Transform parent = null)
        {
            dict.Add(taskCounter, new GroupInfo()
            {
                Counter = taskCounter,
                Reference = reference,
                OnPrefabLoad = onPrefabLoad,
                OnAssetLoad = onAssetLoad,
                Parent = parent,
                IsPrefab = typeof(T) == typeof(GameObject),
                IsSprite = typeof(T) == typeof(Sprite),
            });
            taskCounter++;
            return this;
        }

        /// <summary>
        /// 全部任务完成后回调
        /// </summary>
        /// <param name="allLoadedCallback"></param>
        /// <returns></returns>
        public AssetTaskGroup OnAllLoaded(Action allLoadedCallback)
        {
            allLoaded = allLoadedCallback;
            return this;
        }

        /// <summary>
        /// 开始任务
        /// </summary>
        /// <returns></returns>
        public AssetTaskGroup Begin()
        {
            //非顺序加载
            var pairs = dict.ToArray();
            if (dict.Count == 0)
            {
                allLoaded?.Invoke();
            }
            else
            {
                LoadNonSequence(pairs);
            }

            return this;
        }

        private void LoadNonSequence(KeyValuePair<int, GroupInfo>[] pairs)
        {
            for (int i = 0; i < pairs.Length; i++)
            {
                var kvp = pairs[i];

                void CheckLoad(Object asset)
                {
                    OnNonSeqLoaded(kvp.Key);
                }

                if (kvp.Value.IsPrefab)
                {
                    AsyncOperationHandle<GameObject> goHandle = default;
                    if (string.IsNullOrEmpty(kvp.Value.AssetName))
                    {
                        goHandle = AssetsManager.Instance.CreatePrefabAsync(kvp.Value.Reference, CheckLoad,
                            kvp.Value.Parent);
                    }
                    else
                    {
                        goHandle = AssetsManager.Instance.CreatePrefabAsync(kvp.Value.AssetName, CheckLoad,
                            kvp.Value.Parent);
                    }

                    kvp.Value.goHandle = goHandle;
                }
                else if (kvp.Value.IsSprite)
                {
                    AsyncOperationHandle<Sprite> spriteHandle = default;
                    if (string.IsNullOrEmpty(kvp.Value.AssetName))
                    {
                        spriteHandle =
                            AssetsManager.Instance.CreateAssetAsync<Sprite>(kvp.Value.Reference, CheckLoad);
                    }
                    else
                    {
                        spriteHandle =
                            AssetsManager.Instance.CreateAssetAsync<Sprite>(kvp.Value.AssetName, CheckLoad);
                    }

                    kvp.Value.spriteHandle = spriteHandle;
                }
                else
                {
                    AsyncOperationHandle<Object> assetHandle = default;
                    if (string.IsNullOrEmpty(kvp.Value.AssetName))
                    {
                        assetHandle =
                            AssetsManager.Instance.CreateAssetAsync<Object>(kvp.Value.Reference, CheckLoad);
                    }
                    else
                    {
                        assetHandle =
                            AssetsManager.Instance.CreateAssetAsync<Object>(kvp.Value.AssetName, CheckLoad);
                    }

                    kvp.Value.assetHandle = assetHandle;
                }
            }
        }

        /// <summary>
        /// 非顺序加载回调
        /// </summary>
        /// <param name="counter"></param>
        /// <param name="go"></param>
        /// <param name="sprite"></param>
        /// <param name="asset"></param>
        private void OnNonSeqLoaded(int counter)
        {
            //全部加载完毕后才执行
            if (counter != dict.Count)
                return;
            //此时，最后一个Handle正在执行回调，尚未返回Handle,状态必然为Invalid
            //所以需要延迟一帧
            foreach (var key in dict.Keys)
            {
                var groupInfo = dict[key];
                if (groupInfo.IsPrefab)
                {
                    if (groupInfo.OnPrefabLoad == default(OnPrefabLoadHandle))
                    {
                        Debug.LogWarning(groupInfo.AssetName + " ...没有Prefab回调!");
                    }

                    groupInfo.OnPrefabLoad?.Invoke(groupInfo.goHandle);
                }
                else if (groupInfo.IsSprite)
                {
                    if (groupInfo.OnSpriteLoad == default(OnAssetLoadHandle<Sprite>))
                    {
                        Debug.LogWarning(groupInfo.AssetName + " ...没有Sprite回调!");
                    }

                    groupInfo.OnSpriteLoad?.Invoke(groupInfo.spriteHandle);
                }
                else
                {
                    if (groupInfo.OnAssetLoad == default(OnAssetLoadHandle<Object>))
                    {
                        Debug.LogWarning(groupInfo.AssetName + " ...没有Asset回调!");
                    }

                    groupInfo.OnAssetLoad?.Invoke(groupInfo.assetHandle);
                }
            }

            allLoaded?.Invoke();
            Dispose();
        }

        public void Dispose()
        {
            allLoaded = null;
            dict = null;
        }
    }

    public delegate void OnAssetLoadHandle<T>(AsyncOperationHandle<T> asset);

    public delegate void OnPrefabLoadHandle(AsyncOperationHandle<GameObject> go);
}