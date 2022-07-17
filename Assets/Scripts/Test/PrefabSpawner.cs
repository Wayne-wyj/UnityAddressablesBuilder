using System;
using System.Collections;
using System.Collections.Generic;
using InspectorHelper;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;
using Utility.Assetframe;

public class PrefabSpawner : MonoBehaviour
{
    public SpawnType Type;

    public AssetReference Asset;
    private List<GameObject> gos = new List<GameObject>();
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
                
                AssetsManager.Instance.CreatePrefabTask(assetName, (result, data) =>
                {
                    gos.Add(result);
                },defaultParent:transform);
                break;
            case SpawnType.AssetReference:
                var operation=  Asset.InstantiateAsync(transform.position, Quaternion.identity,transform);
                operation.Completed += (handle) =>
                {
                    gos.Add(handle.Result);
                };
                break;
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
            gos[i].transform.localPosition=Vector3.up * i * 1.25f;
        }
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        assetName = (Asset!=null && Asset.editorAsset != null) ? Asset.editorAsset.name + ".prefab" : string.Empty;
    }


#endif
}

public enum SpawnType
{
    AssetReference,
    AssetsManager,
}
