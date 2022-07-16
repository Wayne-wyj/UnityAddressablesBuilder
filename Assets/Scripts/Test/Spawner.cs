using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class Spawner : MonoBehaviour
{
    public AssetReference Asset;
    public List<GameObject> GOs = new List<GameObject>();
    public float Delay = 3;

    public void StartSpawn()
    {
        Invoke("Spawn",Delay);
    }

    private void Spawn()
    {
        if (Asset == null)
        {
            return;
        }
        var operation=  Asset.InstantiateAsync(transform.position, Quaternion.identity,transform);
        operation.Completed += (handle) =>
        {
            GOs.Add(handle.Result);
        };
    }

    private void Update()
    {
        for (int i = GOs.Count - 1; i >= 0; i--)
        {
            if (GOs[i] == null)
            {
                GOs.RemoveAt(i);
                continue;
            }
            GOs[i].transform.localPosition=Vector3.up * i * 1.25f;
        }
    }
}
