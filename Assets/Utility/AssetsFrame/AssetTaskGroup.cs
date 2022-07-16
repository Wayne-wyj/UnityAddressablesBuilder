using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utility.Assetframe
{
    public sealed class AssetTaskGroup : IDisposable
    {
        private struct GroupInfo
        {
            public int counter;
            public string asset;
            public OnAssetLoaded callback;
            public object curUserData;
            public Transform curParent;
            public bool IsPrefab;
        }

        /// <summary>
        /// 顺序加载还是同时加载
        /// </summary>
        private bool m_sequence = false;

        private Action m_allLoaded;
        private Dictionary<int, GroupInfo> m_dict = new Dictionary<int, GroupInfo>();

        public int LeftCount => m_dict.Count;

        private int m_taskCounter = 0;

        public AssetTaskGroup()
        {
        }

        public static AssetTaskGroup New()
        {
            return new AssetTaskGroup();
        }

        public AssetTaskGroup Add<T>(string assetName, OnAssetLoaded onLoaded, object userData = null, Transform parent = null)
        {
            m_dict.Add(m_taskCounter, new GroupInfo()
            {
                counter = m_taskCounter,
                asset = assetName,
                callback = onLoaded,
                curUserData = userData,
                curParent = parent,
                IsPrefab = typeof(T) == typeof(GameObject),
            });
            m_taskCounter++;
            return this;
        }

        public AssetTaskGroup SetIsSequence(bool sequence)
        {
            m_sequence = sequence;
            return this;
        }

        public AssetTaskGroup OnAllLoaded(Action allLoadedCallback)
        {
            m_allLoaded = allLoadedCallback;
            return this;
        }

        public AssetTaskGroup Begin()
        {
            if (!m_sequence)
            {
                if (m_dict.Count == 0)
                {
                    m_allLoaded?.Invoke();
                }
                else
                {
                    //非顺序加载
                    foreach (var kvp in m_dict)
                    {
                        if (kvp.Value.IsPrefab)
                            AssetsManager.Instance.CreatePrefabTask(kvp.Value.asset, OnNoneSeqLoaded, kvp.Key, kvp.Value.curParent);
                        else
                            AssetsManager.Instance.CreateAssetTask(kvp.Value.asset, OnNoneSeqLoaded, kvp.Key);
                    }
                }
            }
            else
            {
                LoadBySequence();
            }
            return this;
        }

        /// <summary>
        /// 非顺序加载回调
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="userData"></param>
        private void OnNoneSeqLoaded(UnityEngine.Object asset, object userData)
        {
            int counter = (int)userData;
            m_dict[counter].callback(asset, m_dict[counter].curUserData);
            m_dict.Remove(counter);
            if (m_dict.Count == 0)
            {
                m_allLoaded?.Invoke();
                Dispose();
            }
        }

        private void LoadBySequence()
        {
            foreach (var kvp in m_dict)
            {
                if (kvp.Value.IsPrefab)
                    AssetsManager.Instance.CreatePrefabTask(kvp.Value.asset, OnSeqLoaded, kvp.Key, kvp.Value.curParent);
                else
                    AssetsManager.Instance.CreateAssetTask(kvp.Value.asset, OnSeqLoaded, kvp.Key);
                return;
            }
        }

        /// <summary>
        /// 按顺序加载
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="userData"></param>
        private void OnSeqLoaded(UnityEngine.Object asset, object userData)
        {
            int counter = (int)userData;
            m_dict[counter].callback(asset, m_dict[counter].curUserData);
            m_dict.Remove(counter);

            if (m_dict.Count != 0)
            {
                LoadBySequence();
            }
            else
            {
                m_allLoaded?.Invoke();
                Dispose();
            }
        }

        public void Dispose()
        {
            m_allLoaded = null;
            m_dict = null;
        }
    }
}