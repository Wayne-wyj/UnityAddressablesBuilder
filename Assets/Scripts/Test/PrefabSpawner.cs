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
        public AssetReference Asset;

        public List<AssetReference> GroupAssets = new List<AssetReference>();

        private List<GameObject> gos = new List<GameObject>();
        [SerializeField] [ReadOnly] private string assetName;

        private AsyncOperationHandle<GameObject> operation;
        private int frameCounter = 0;

        
        void OnGameObjectHandle(AsyncOperationHandle<GameObject> handle)
        {
            gos.Add(handle.Result);
        }

        public void Load()
        {
            if (Asset == null)
            {
                return;
            }

            int tempCounter = frameCounter;
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
                                    group.Add<GameObject>(asset, onPrefabLoad: OnGameObjectHandle,
                                        parent: transform);
                                }

                                group.Begin();
                            }
                            else
                            {
                                if (Async)
                                {
                                    operation = AssetsManager.Instance.CreatePrefabAsync(Asset, go =>
                                    {
                                        gos.Add(go);
                                        Debug.Log("AssetsManager -- Reference :" + (Async ? "异步  " : "同步  ") + "耗时 :" +
                                                  (frameCounter - tempCounter) + "帧");
                                    }, defaultParent: transform);
                                }
                                else
                                {
                                    operation = AssetsManager.Instance.CreatePrefab(Asset, go =>
                                    {
                                        gos.Add(go);
                                        Debug.Log("AssetsManager -- Reference :" + (Async ? "异步  " : "同步  ") + "耗时 :" +
                                                  (frameCounter - tempCounter) + "帧");
                                    }, defaultParent: transform);
                                }

                               
                            }

                            break;
                        case AssetsManagerLoadType.ByName:
                            if (ByGroup)
                            {
                                AssetTaskGroup group = new AssetTaskGroup();
                                foreach (var asset in GroupAssets)
                                {
                                    group.Add<GameObject>(asset, onPrefabLoad: (handle) =>
                                        {
                                            gos.Add(handle.Result);
                                        },
                                        parent: transform);
                                }

                                group.Begin();
                            }
                            else
                            {
                                if (Async)
                                {
                                    operation = AssetsManager.Instance.CreatePrefabAsync(assetName, go =>
                                        {
                                            gos.Add(go);
                                            Debug.Log("AssetsManager -- Name :" + (Async ? "异步  " : "同步  ") + "耗时 :" +
                                                      (frameCounter - tempCounter) + "帧");
                                        }, defaultParent: transform);
                                }
                                else
                                {
                                    operation = AssetsManager.Instance.CreatePrefab(assetName, go =>
                                        {
                                            gos.Add(go);
                                            Debug.Log("AssetsManager -- Name :" + (Async ? "异步  " : "同步  ") + "耗时 :" +
                                                      (frameCounter - tempCounter) + "帧");
                                        },
                                        defaultParent: transform);
                                }
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
                            Debug.Log("AssetReference :" + (Async ? "异步  " : "同步  ") + "耗时 :" +   (frameCounter - tempCounter) + "帧");
                            gos.Add(handle.Result);
                        };
                    }
                    else
                    {
                        operation.WaitForCompletion();
                        gos.Add(operation.Result);
                        Debug.Log("AssetReference :" + (Async ? "异步  " : "同步  ") + "耗时 :" +   (frameCounter - tempCounter) + "帧");
                    }
                    break;
            }
        }

        private void Awake()
        {
            AssetsManager.Instance.InitAssetManager(null);
        }

        private void Update()
        {
            frameCounter++;
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