using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerSend : MonoBehaviour
{
    public static void Welcome(int _toClient, string msg)
    {
        using (Packet _packet = new Packet((int)ServerPackets.welcome))
        {
            _packet.Write(msg);
            _packet.Write(_toClient);

            SendTCPData(_toClient, _packet);
        }
    }
    private static void SendUDPData(int toClient, Packet packet)
    {
        packet.WriteLength();
        Server.clients[toClient].udp.SendData(packet);
    }
    private static void SendTCPData(int toClient, Packet packet)
    {
        packet.WriteLength();
        Server.clients[toClient].tcp.SendData(packet);
    }
    private static void SendUDPDataToAll(Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.clients[i].udp.SendData(packet);
        }

    }
    private static void SendUDPDataToAll(int exceptClient, Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            if (i != exceptClient)
            {
                Server.clients[i].udp.SendData(packet);
            }
        }

    }
    private static void SendTCPDataToAll(Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.clients[i].tcp.SendData(packet);
        }

    }
    private static void SendTCPDataToAll(int exceptClient, Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            if (i != exceptClient)
            {
                Server.clients[i].tcp.SendData(packet);
            }
        }

    }


    public static void SpawnPlayer(int _toClient, Player player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnPlayer))
        {
            _packet.Write(player.id);
            _packet.Write(player.username);
            _packet.Write(player.transform.position);
            _packet.Write(player.transform.rotation);

            SendTCPData(_toClient, _packet);

        }
    }

    public static void PlayerPosition(Player player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerPosition))
        {
            _packet.Write(player.id);
            _packet.Write(player.transform.position);
            SendUDPDataToAll(_packet);
        }
    }

    public static void PlayerRotation(Player player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerRotation))
        {
            _packet.Write(player.id);
            _packet.Write(player.transform.rotation);
            SendUDPDataToAll(player.id, _packet);
        }
    }

    public static void DisConnected(int _playerId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerDisconnected))
        {
            _packet.Write(_playerId);
            SendTCPDataToAll(_packet);
        }
    }
    public static void PlayerHealth(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerHp))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.Hp);
            SendTCPDataToAll(_packet);
        }
    }
    public static void PlayerRespawned(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerRespawn))
        {
            _packet.Write(_player.id);
            SendTCPDataToAll(_packet) ;
        }
    }

    public static void CreateItemSpawner(int _toClient ,int _spawnerId , Vector3 _pos, bool _hasItem)
    {
        using (Packet _packet = new Packet((int)ServerPackets.CreateItemSpawner))
        { 
            _packet.Write(_spawnerId);
            _packet.Write(_pos);
            _packet.Write(_hasItem);
            SendTCPData(_toClient, _packet);

        }

    }
    public static void ItemSpawned(int _spawnId)
    {
        using (Packet _packet= new Packet((int)ServerPackets.ItemSpawned))
        {
            _packet.Write(_spawnId);
            SendTCPDataToAll(_packet) ;
        }
    }
    public static void ItemPickedUp(int _spawnerId, int _byPlayer)
    {
        using (Packet _packet = new Packet((int)ServerPackets.ItemPickedUp))
        {
            _packet.Write(_spawnerId);
            _packet.Write(_byPlayer);

            SendTCPDataToAll(_packet);
        }
    }
    public static void SpawnProjectile(Projectile _projectile, int _thrownByPlayer)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnProjectile))
        {
            _packet.Write(_projectile.id);
            _packet.Write(_projectile.transform.position);
            _packet.Write(_thrownByPlayer);

            SendTCPDataToAll(_packet);
        }
    }

    public static void ProjectilePosition(Projectile _projectile)
    {
        using (Packet _packet = new Packet((int)ServerPackets.projectilePosition))
        {
            _packet.Write(_projectile.id);
            _packet.Write(_projectile.transform.position);

            SendTCPDataToAll(_packet);
        }
    }

    public static void ProjectileExploded(Projectile _projectile)
    {
        using (Packet _packet = new Packet((int)ServerPackets.projectileExploded))
        {
            _packet.Write(_projectile.id);
            _packet.Write(_projectile.transform.position);

            SendTCPDataToAll(_packet);
        }
    }
}

