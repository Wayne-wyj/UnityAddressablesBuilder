using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;
using Utility.Assetframe;

public class Spawner : MonoBehaviour
{
    public SpawnType Type;
    public AssetReference Asset;
    private List<GameObject> gos = new List<GameObject>();

    public void StartSpawn()
    {
        Invoke("Spawn",0f);
    }

    private void Spawn()
    {
        if (Asset == null)
        {
            return;
        }

        switch (Type)
        {
            case SpawnType.AssetManager:
                
                AssetsManager.Instance.CreatePrefabTask(Asset.editorAsset.name+".prefab", (result, data) =>
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
}

public enum SpawnType
{
    AssetReference,
    AssetManager,
}
