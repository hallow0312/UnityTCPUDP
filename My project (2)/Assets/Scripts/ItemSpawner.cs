using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public int SpawnerId;
    public bool hasItem;
    public MeshRenderer itemModel;

    public float itemRotationSpeed = 50.0f;
    public float itemBobSpeed = 2.0f;
    private Vector3 basePosition;

    private void Update()
    {
        if (hasItem)
        {
            transform.Rotate(Vector3.up, itemRotationSpeed * Time.deltaTime, Space.World);
            transform.position = basePosition + new Vector3(0.0f, 0.25f*Mathf.Sin(Time.time * itemBobSpeed),0.0f);
        }
    }
    public void Initialize(int _spawnerId,bool _hasItem)
    {
        SpawnerId = _spawnerId;
        hasItem = _hasItem;
        itemModel.enabled = _hasItem;
        basePosition = transform.position;
    }
    public void ItemSpawned()
    {
        hasItem = true;
        itemModel.enabled = true;
    }
    public void ItemPickedUp()
    {
        hasItem = false;
        itemModel.enabled = false;
    }
}
