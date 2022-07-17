using System;
using System.Collections;
using System.Collections.Generic;
using InspectorHelper;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Utility.Assetframe;

public class SpriteLoader : MonoBehaviour
{
    public SpawnType Type;
    public bool LoadByReferrence;
    public Image Img;
    public AssetReference Asset;
    [SerializeField]
    [ReadOnly]
    private string assetName;
    
    public void Load()
    {
        if (Asset == null)
        {
            return;
        }

        switch (Type)
        {
            case SpawnType.AssetsManager:
                if (LoadByReferrence)
                {
                    AssetsManager.Instance.CreateSpriteTask(Asset, (handle) =>
                    {
                        Img.sprite = handle.Result;
                    });
                }
                else
                {
                    AssetsManager.Instance.CreateSpriteTask(assetName, (result, data) =>
                    {
                        Img.sprite = result;
                    });
                }
                break;
            case SpawnType.AssetReference:
                //重复加载会报错!!
                var operation=  Asset.LoadAssetAsync<Sprite>();
                operation.Completed += (handle) =>
                {
                    Img.sprite = handle.Result;
                };
                break;
        }
        
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
