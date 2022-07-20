using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace Utility.AssetsFrame
{
    public sealed class AssetTaskGroup : IDisposable
    {
        private struct GroupInfo
        {
            public int Counter;
            public string AssetName;
            public AssetReference Reference;
            public OnAssetLoad<Object> OnAssetLoad;
            public OnPrefabLoad OnPrefabLoad;
            public Transform Parent;
            public bool IsPrefab;
            public bool IsSprite;
        }

        /// <summary>
        /// 顺序加载还是同时加载
        /// </summary>
        private bool sequence = false;

        /// <summary>
        /// 是否异步加载
        /// </summary>
        private bool async = false;

        private Action allLoaded;
        private Dictionary<int, GroupInfo> dict = new Dictionary<int, GroupInfo>();

        public int LeftCount => dict.Count;

        private int taskCounter = 0;

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
        public AssetTaskGroup Add<T>(string assetName, OnAssetLoad<Object> onAssetLoad = null,
            OnPrefabLoad onPrefabLoad = null, Transform parent = null)
        {
            dict.Add(taskCounter, new GroupInfo()
            {
                Counter = taskCounter,
                AssetName = assetName,
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
        /// 新增加载任务
        /// </summary>
        /// <param name="reference">资源引用</param>
        /// <param name="onPrefabLoad">prefab回调</param>
        /// <param name="onAssetLoad">asset回调</param>
        /// <param name="parent">prefab挂载的Parent</param>
        /// <typeparam name="T">具体类型 GameObject/Sprite/Object</typeparam>
        /// <returns></returns>
        public AssetTaskGroup Add<T>(AssetReference reference, OnAssetLoad<Object> onAssetLoad = null,
            OnPrefabLoad onPrefabLoad = null, Transform parent = null)
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
        /// 设置是否顺序加载
        /// </summary>
        /// <param name="isSequence"></param>
        /// <returns></returns>
        public AssetTaskGroup SetIsSequence(bool isSequence)
        {
            this.sequence = isSequence;
            return this;
        }

        /// <summary>
        /// 设置是否异步加载
        /// </summary>
        /// <param name="isAsync"></param>
        /// <returns></returns>
        public AssetTaskGroup SetIsAsync(bool isAsync)
        {
            this.async = isAsync;
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
            if (!sequence)
            {
                if (dict.Count == 0)
                {
                    allLoaded?.Invoke();
                }
                else
                {
                    for (int i = 0; i < pairs.Length; i++)
                    {
                        var kvp = pairs[i];

                        void OnLoadPrefab(GameObject go)
                        {
                            OnNoneSeqLoaded<GameObject>(kvp.Key, go: go);
                        }

                        void OnLoadAsset(Object asset)
                        {
                            OnNoneSeqLoaded<Object>(kvp.Key, asset: asset);
                        }

                        if (kvp.Value.IsPrefab)
                        {
                            if (string.IsNullOrEmpty(kvp.Value.AssetName))
                            {
                                if (async)
                                    AssetsManager.Instance.CreatePrefabAsync(kvp.Value.Reference, OnLoadPrefab,
                                        kvp.Value.Parent);
                                else
                                    AssetsManager.Instance.CreatePrefab(kvp.Value.Reference, OnLoadPrefab,
                                        kvp.Value.Parent);
                            }
                            else
                            {
                                if (async)
                                    AssetsManager.Instance.CreatePrefabAsync(kvp.Value.AssetName, OnLoadPrefab,
                                        kvp.Value.Parent);
                                else
                                    AssetsManager.Instance.CreatePrefab(kvp.Value.AssetName, OnLoadPrefab,
                                        kvp.Value.Parent);
                            }
                        }
                        else if (kvp.Value.IsSprite)
                        {
                            if (string.IsNullOrEmpty(kvp.Value.AssetName))
                            {
                                if (async)
                                    AssetsManager.Instance.CreateAssetAsync<Sprite>(kvp.Value.Reference, OnLoadAsset);
                                else
                                    AssetsManager.Instance.CreateAsset<Sprite>(kvp.Value.Reference, OnLoadAsset);
                            }
                            else
                            {
                                if (async)
                                    AssetsManager.Instance.CreateAssetAsync<Sprite>(kvp.Value.AssetName, OnLoadAsset);
                                else
                                    AssetsManager.Instance.CreateAsset<Sprite>(kvp.Value.AssetName, OnLoadAsset);
                            }
                        }
                        else
                        {
                            if (async)
                                AssetsManager.Instance.CreateAssetAsync<Object>(kvp.Value.AssetName, OnLoadAsset);
                            else
                                AssetsManager.Instance.CreateAsset<Object>(kvp.Value.AssetName, OnLoadAsset);
                        }
                    }
                }
            }
            //顺序
            else
            {
                LoadBySequence();
            }

            return this;
        }

        /// <summary>
        /// 非顺序加载回调
        /// </summary>
        /// <param name="counter"></param>
        /// <param name="go"></param>
        /// <param name="sprite"></param>
        /// <param name="asset"></param>
        private void OnNoneSeqLoaded<T>(int counter, GameObject go = null, T asset = null) where T : Object
        {
            var groupInfo = dict[counter];
            if (groupInfo.IsPrefab)
            {
                if (groupInfo.OnPrefabLoad == default(OnPrefabLoad))
                {
                    Debug.LogWarning(groupInfo.AssetName + " ...没有Prefab回调!");
                }

                groupInfo.OnPrefabLoad?.Invoke(go);
            }
            else
            {
                if (groupInfo.OnAssetLoad == default(OnAssetLoad<Object>))
                {
                    Debug.LogWarning(groupInfo.AssetName + " ...没有Asset回调!");
                }

                groupInfo.OnAssetLoad?.Invoke(asset);
            }

            dict.Remove(counter);
            if (dict.Count == 0)
            {
                allLoaded?.Invoke();
                Dispose();
            }
        }

        /// <summary>
        /// 顺序加载
        /// </summary>
        private void LoadBySequence()
        {
            var pairs = dict.ToArray();
            for (int i = 0; i < pairs.Length; i++)
            {
                var kvp = pairs[i];
                void OnLoadPrefab(GameObject go)
                {
                    OnSeqLoaded(kvp.Key, go: go);
                }

                void OnLoadAsset(Object asset)
                {
                    OnSeqLoaded(kvp.Key, asset: asset);
                }

                if (kvp.Value.IsPrefab)
                {
                    if (string.IsNullOrEmpty(kvp.Value.AssetName))
                    {
                        if (async)
                            AssetsManager.Instance.CreatePrefabAsync(kvp.Value.Reference, OnLoadPrefab,
                                kvp.Value.Parent);
                        else
                            AssetsManager.Instance.CreatePrefab(kvp.Value.Reference, OnLoadPrefab, kvp.Value.Parent);
                    }
                    else
                    {
                        if (async)
                            AssetsManager.Instance.CreatePrefabAsync(kvp.Value.AssetName, OnLoadPrefab,
                                kvp.Value.Parent);
                        else
                            AssetsManager.Instance.CreatePrefab(kvp.Value.AssetName, OnLoadPrefab,
                                kvp.Value.Parent);
                    }
                }
                else if (kvp.Value.IsSprite)
                {
                    if (string.IsNullOrEmpty(kvp.Value.AssetName))
                    {
                        if (async)
                            AssetsManager.Instance.CreateAssetAsync<Sprite>(kvp.Value.Reference, OnLoadAsset);
                        else
                            AssetsManager.Instance.CreateAsset<Sprite>(kvp.Value.Reference, OnLoadAsset);
                    }
                    else
                    {
                        if (async)
                            AssetsManager.Instance.CreateAssetAsync<Sprite>(kvp.Value.AssetName, OnLoadAsset);
                        else
                            AssetsManager.Instance.CreateAsset<Sprite>(kvp.Value.AssetName, OnLoadAsset);
                    }
                }
                else
                {
                    if (async)
                        AssetsManager.Instance.CreateAssetAsync<Object>(kvp.Value.AssetName, OnLoadAsset);
                    else
                        AssetsManager.Instance.CreateAsset<Object>(kvp.Value.AssetName, OnLoadAsset);
                }

                return;
            }
        }

        /// <summary>
        /// 顺序加载回调
        /// </summary>
        /// <param name="counter"></param>
        /// <param name="go"></param>
        /// <param name="sprite"></param>
        /// <param name="asset"></param>
        private void OnSeqLoaded(int counter, GameObject go = null, Object asset = null)
        {
            var groupInfo = dict[counter];
            if (groupInfo.IsPrefab)
            {
                if (groupInfo.OnPrefabLoad == default(OnPrefabLoad))
                {
                    Debug.LogWarning(groupInfo.AssetName + " ...没有Prefab回调!");
                }

                groupInfo.OnPrefabLoad?.Invoke(go);
            }
            else
            {
                if (groupInfo.OnAssetLoad == default)
                {
                    Debug.LogWarning(groupInfo.AssetName + " ...没有Asset回调!");
                }

                groupInfo.OnAssetLoad?.Invoke(asset);
            }

            dict.Remove(counter);
            if (dict.Count != 0)
            {
                LoadBySequence();
            }
            else
            {
                allLoaded?.Invoke();
                Dispose();
            }
        }

        public void Dispose()
        {
            allLoaded = null;
            dict = null;
        }
    }
}