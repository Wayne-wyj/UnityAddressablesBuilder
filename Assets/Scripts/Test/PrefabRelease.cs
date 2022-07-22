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
    public class PrefabRelease : MonoBehaviour
    {
        public AssetReference Asset;
        public bool ManuallyInstantiate = false;

        private List<AsyncOperationHandle<GameObject>> handles = new List<AsyncOperationHandle<GameObject>>();
        private List<GameObject> gos = new List<GameObject>();

        public void Load()
        {
            if (Asset == null)
            {
                return;
            }

            if (ManuallyInstantiate)
            {
                //只添加一个handle，其余手动生成
                handles.Add(AssetsManager.Instance.CreatePrefabAsync(Asset, go =>
                {
                    gos.Add(go);
                    for (int i = 0; i < 4; i++)
                    {
                        gos.Add(Instantiate(go,transform));
                    }
                },transform));
            }
            else
            {
                //全部都采用Addressable生成
                for (int i = 0; i < 5; i++)
                {
                    handles.Add(AssetsManager.Instance.CreatePrefabAsync(Asset, go =>
                    {
                        gos.Add(go);
                    },transform));
                }
            }
        }

        public void ReleaseHandle()
        {
            for (int i = 0; i < handles.Count; i++)
            {
                Addressables.Release(handles[i]);
            }
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

    }

}