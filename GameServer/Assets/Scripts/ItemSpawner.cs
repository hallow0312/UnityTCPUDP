using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public static Dictionary<int, ItemSpawner> spawners = new Dictionary<int, ItemSpawner>();
    private static int nextSpawnerId = 1;

    public int spawnerId;
    public bool hasItem = false;
    void Start()
    {
        hasItem = false;
        spawnerId = nextSpawnerId++;
        spawners.Add(spawnerId, this);
        StartCoroutine(SpawnItem());
    }
    private void OnTriggerEnter(Collider other)
    {
        if (hasItem && other.CompareTag("Player"))
        {
            Player _player  =other.GetComponent<Player>();
            if(_player.AttemptPickupItem())
            {
                ItemPickUp(_player.id);
            }
        }
    }
    private IEnumerator SpawnItem()
    {
        yield return new WaitForSeconds(10.0f);
        hasItem = true;
        ServerSend.ItemSpawned(spawnerId);

    }
    private void ItemPickUp(int _byPlayer)
    {
        hasItem = false;
        ServerSend.ItemPickedUp(spawnerId, _byPlayer);

        StartCoroutine(SpawnItem());
    }
}
