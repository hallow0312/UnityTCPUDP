using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerSend : MonoBehaviour
{
    // 클라이언트에 환영 메시지 전송
    public static void Welcome(int _toClient, string msg)
    {
        using (Packet _packet = new Packet((int)ServerPackets.welcome)) // 패킷 생성
        {
            _packet.Write(msg); // 메시지 작성
            _packet.Write(_toClient); // 클라이언트 ID 작성

            SendTCPData(_toClient, _packet); // TCP를 통해 패킷 전송
        }
    }

    // 특정 클라이언트에 UDP 데이터 전송
    private static void SendUDPData(int toClient, Packet packet)
    {
        packet.WriteLength(); // 패킷 길이 기록
        Server.clients[toClient].udp.SendData(packet); // UDP 전송
    }

    // 특정 클라이언트에 TCP 데이터 전송
    private static void SendTCPData(int toClient, Packet packet)
    {
        packet.WriteLength(); // 패킷 길이 기록
        Server.clients[toClient].tcp.SendData(packet); // TCP 전송
    }

    // 모든 클라이언트에 UDP 데이터 전송
    private static void SendUDPDataToAll(Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++) // 연결된 모든 클라이언트 반복
        {
            Server.clients[i].udp.SendData(packet);
        }
    }

    // 특정 클라이언트를 제외한 모든 클라이언트에 UDP 데이터 전송
    private static void SendUDPDataToAll(int exceptClient, Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            if (i != exceptClient) // 제외된 클라이언트는 건너뜀
            {
                Server.clients[i].udp.SendData(packet);
            }
        }
    }

    // 모든 클라이언트에 TCP 데이터 전송
    private static void SendTCPDataToAll(Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.clients[i].tcp.SendData(packet);
        }
    }

    // 특정 클라이언트를 제외한 모든 클라이언트에 TCP 데이터 전송
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

    // 특정 클라이언트에 플레이어 스폰 정보 전송
    public static void SpawnPlayer(int _toClient, Player player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnPlayer))
        {
            _packet.Write(player.id); // 플레이어 ID
            _packet.Write(player.username); // 플레이어 이름
            _packet.Write(player.transform.position); // 플레이어 위치
            _packet.Write(player.transform.rotation); // 플레이어 회전

            SendTCPData(_toClient, _packet); // TCP 전송
        }
    }

    // 모든 클라이언트에 플레이어 위치 정보 전송
    public static void PlayerPosition(Player player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerPosition))
        {
            _packet.Write(player.id);
            _packet.Write(player.transform.position);

            SendUDPDataToAll(_packet); // UDP 전송
        }
    }

    // 특정 클라이언트를 제외한 모든 클라이언트에 플레이어 회전 정보 전송
    public static void PlayerRotation(Player player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerRotation))
        {
            _packet.Write(player.id);
            _packet.Write(player.transform.rotation);

            SendUDPDataToAll(player.id, _packet);
        }
    }

    // 모든 클라이언트에 플레이어 연결 해제 정보 전송
    public static void DisConnected(int _playerId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerDisconnected))
        {
            _packet.Write(_playerId);
            SendTCPDataToAll(_packet);
        }
    }

    // 모든 클라이언트에 플레이어 체력 정보 전송
    public static void PlayerHealth(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerHp))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.Hp);

            SendTCPDataToAll(_packet);
        }
    }

    // 모든 클라이언트에 플레이어 리스폰 정보 전송
    public static void PlayerRespawned(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerRespawn))
        {
            _packet.Write(_player.id);
            SendTCPDataToAll(_packet);
        }
    }

    // 특정 클라이언트에 아이템 스포너 생성 정보 전송
    public static void CreateItemSpawner(int _toClient, int _spawnerId, Vector3 _pos, bool _hasItem)
    {
        using (Packet _packet = new Packet((int)ServerPackets.CreateItemSpawner))
        {
            _packet.Write(_spawnerId);
            _packet.Write(_pos);
            _packet.Write(_hasItem);

            SendTCPData(_toClient, _packet);
        }
    }

    // 모든 클라이언트에 아이템 스폰 정보 전송
    public static void ItemSpawned(int _spawnId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.ItemSpawned))
        {
            _packet.Write(_spawnId);
            SendTCPDataToAll(_packet);
        }
    }

    // 모든 클라이언트에 아이템 획득 정보 전송
    public static void ItemPickedUp(int _spawnerId, int _byPlayer)
    {
        using (Packet _packet = new Packet((int)ServerPackets.ItemPickedUp))
        {
            _packet.Write(_spawnerId);
            _packet.Write(_byPlayer);

            SendTCPDataToAll(_packet);
        }
    }

    // 모든 클라이언트에 투사체 생성 정보 전송
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

    // 모든 클라이언트에 투사체 위치 정보 전송
    public static void ProjectilePosition(Projectile _projectile)
    {
        using (Packet _packet = new Packet((int)ServerPackets.projectilePosition))
        {
            _packet.Write(_projectile.id);
            _packet.Write(_projectile.transform.position);

            SendTCPDataToAll(_packet);
        }
    }

    // 모든 클라이언트에 투사체 폭발 정보 전송
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
