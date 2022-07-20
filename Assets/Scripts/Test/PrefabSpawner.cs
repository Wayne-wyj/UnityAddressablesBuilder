using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;
using Utility.AssetsFrame;
using Utility.InspectorHelper;

namespace Test
{
    public class PrefabSpawner : MonoBehaviour
    {
        public SpawnType SpawnType;
        public AssetsManagerLoadType AssetsManagerLoadType;
        public bool Async;
        public bool ByGroup;
        public bool GroupSequence;
        public AssetReference Asset;

        public List<AssetReference> GroupAssets = new List<AssetReference>();

        private List<GameObject> gos = new List<GameObject>();
        [SerializeField] [ReadOnly] private string assetName;

        private AsyncOperationHandle<GameObject> operation;

        public void Load()
        {
            if (Asset == null)
            {
                return;
            }

            float start = Time.realtimeSinceStartup;
            float end = start;
            switch (SpawnType)
            {
                case SpawnType.AssetsManager:
                    switch (AssetsManagerLoadType)
                    {
                        case AssetsManagerLoadType.ByReference:
                            if (ByGroup)
                            {
                                AssetTaskGroup group = new AssetTaskGroup();
                                foreach (var asset in GroupAssets)
                                {
                                    group.Add<GameObject>(asset, onPrefabLoad: (go) => { gos.Add(go); },
                                        parent: transform);
                                }

                                group.SetIsAsync(Async).SetIsSequence(GroupSequence).Begin();
                            }
                            else
                            {
                                if (Async)
                                {
                                    operation = AssetsManager.Instance.CreatePrefabAsync(Asset, go =>
                                    {
                                        end = Time.realtimeSinceStartup;
                                        gos.Add(go);
                                    }, defaultParent: transform);
                                }
                                else
                                {
                                    operation = AssetsManager.Instance.CreatePrefab(Asset, go =>
                                    {
                                        end = Time.realtimeSinceStartup;
                                        gos.Add(go);
                                    }, defaultParent: transform);
                                }

                                Debug.Log("AssetsManager -- Reference :" + (Async ? "异步  " : "同步  ") + "耗时 :" +
                                          (end - start));
                            }

                            break;
                        case AssetsManagerLoadType.ByName:
                            if (ByGroup)
                            {
                                AssetTaskGroup group = new AssetTaskGroup();
                                foreach (var asset in GroupAssets)
                                {
                                    group.Add<GameObject>(asset, onPrefabLoad: (go) => { gos.Add(go); },
                                        parent: transform);
                                }

                                group.SetIsAsync(Async).SetIsSequence(GroupSequence).Begin();
                            }
                            else
                            {
                                if (Async)
                                {
                                    operation = AssetsManager.Instance.CreatePrefabAsync(assetName,
                                        go => { gos.Add(go); }, defaultParent: transform);
                                }
                                else
                                {
                                    operation = AssetsManager.Instance.CreatePrefab(assetName, go => { gos.Add(go); },
                                        defaultParent: transform);
                                }

                                Debug.Log(
                                    "AssetsManager -- Name :" + (Async ? "异步  " : "同步  ") + "耗时 :" + (end - start));
                            }

                            break;
                    }

                    break;
                case SpawnType.AssetReference:
                    operation = Asset.InstantiateAsync(transform.position, Quaternion.identity, transform);
                    if (Async)
                    {
                        operation.Completed += (handle) =>
                        {
                            end = Time.realtimeSinceStartup;
                            gos.Add(handle.Result);
                        };
                    }
                    else
                    {
                        operation.WaitForCompletion();
                        end = Time.realtimeSinceStartup;
                        gos.Add(operation.Result);
                    }
                    Debug.Log("AssetReference :" + (Async ? "异步  " : "同步  ") + "耗时 :" + (end - start));
                    break;
            }
        }

        public void ReleaseInstances()
        {
            foreach (var go in gos)
            {
                Addressables.ReleaseInstance(go);
            }

            gos.Clear();
        }

        public void ReleaseHandle()
        {
            Addressables.Release(operation);
        }

        private void Awake()
        {
            AssetsManager.Instance.InitAssetManager(null);
        }

        private void Update()
        {
            for (int i = gos.Count - 1; i >= 0; i--)
            {
                if (gos[i] == null)
                {
                    gos.RemoveAt(i);
                    continue;
                }

                gos[i].transform.localPosition = Vector3.up * i * 1.25f;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            assetName = (Asset != null && Asset.editorAsset != null)
                ? Asset.editorAsset.name + ".prefab"
                : string.Empty;
        }


#endif
    }

    public enum SpawnType
    {
        AssetReference,
        AssetsManager,
    }
}