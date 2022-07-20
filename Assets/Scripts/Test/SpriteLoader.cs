using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Utility.AssetsFrame;
using Utility.InspectorHelper;

namespace Test
{
    public class SpriteLoader : MonoBehaviour
    {
        public SpawnType SpawnType;
        public AssetsManagerLoadType LoadType;
        public Image Img;
        public bool Async;
        public AssetReference Asset;
        [SerializeField]
        [ReadOnly]
        private string assetName;
    
    

        private AsyncOperationHandle<Sprite> operation;

        public void Load()
        {
            if (Asset == null)
            {
                return;
            }

            switch (SpawnType)
            {
                case SpawnType.AssetsManager:
                    switch (LoadType)
                    {
                        case AssetsManagerLoadType.ByReference:
                            //使用AssetReference加载
                            if (Async)
                            {
                                operation=AssetsManager.Instance.CreateAssetAsync<Sprite>(Asset, result =>
                                {
                                    Img.sprite = result;
                                    //operation = handle;
                                });
                            }
                            else
                            {
                                operation=AssetsManager.Instance.CreateAsset<Sprite>(Asset, result =>
                                {
                                    Img.sprite = result;
                                    //operation = handle;
                                });
                            }
                          
                            break;
                        case AssetsManagerLoadType.ByName:
                            //使用name加载
                            if (Async)
                            {
                                operation=AssetsManager.Instance.CreateAssetAsync<Sprite>(assetName, (result) =>
                                {
                                    Img.sprite = result;
                                });
                            }
                            else
                            {
                                operation=AssetsManager.Instance.CreateAsset<Sprite>(assetName, (result) =>
                                {
                                    Img.sprite = result;
                                });
                            }
                            break;
                    }
                    break;
                case SpawnType.AssetReference:
                    //重复加载会报错!!
                    operation=  Asset.LoadAssetAsync<Sprite>();
                    if (Async)
                    {
                        operation.Completed += (handle) =>
                        {
                            Img.sprite = handle.Result;
                        };
                    }
                    else
                    {
                        operation.WaitForCompletion();
                        Img.sprite = operation.Result;
                    }
                  
                    break;
            }
        
        }
    
        public void ReleaseHandle()
        {
            if (!operation.IsValid())
            {
                return;
            }
            Addressables.Release(operation);
        }

        private void Awake()
        {
            AssetsManager.Instance.InitAssetManager(null);
        }

    
#if UNITY_EDITOR
        private void OnValidate()
        {
            assetName = (Asset!=null && Asset.editorAsset != null) ? Asset.editorAsset.name + ".png" : string.Empty;
        }


#endif
    }

    public enum AssetsManagerLoadType
    {
        ByName,
        ByReference,
    }
}