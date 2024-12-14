using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;

public class Client
{
    public static int dataBufferSize = 4096; // 데이터 버퍼 크기 (최대 4096바이트)

    public int id; // 클라이언트 ID
    public Player player; // 클라이언트가 조종하는 플레이어 객체
    public TCP tcp; // TCP 연결 관리 객체
    public UDP udp; // UDP 연결 관리 객체

    // 클라이언트 생성자, ID와 TCP/UDP 객체를 초기화
    public Client(int _clientId)
    {
        id = _clientId;
        tcp = new TCP(id);
        udp = new UDP(id);
    }

    // TCP 통신 클래스 (안정적인 데이터 전송 담당)
    public class TCP
    {
        public TcpClient socket; // TCP 소켓
        private readonly int id; // 클라이언트 ID
        private NetworkStream stream; // 네트워크 스트림
        private Packet receivedData; // 수신된 데이터를 처리하는 Packet 객체
        private byte[] receiveBuffer; // 수신 데이터를 저장하는 버퍼

        public TCP(int _id)
        {
            id = _id;
        }

        // TCP 연결 설정
        public void Connect(TcpClient _socket)
        {
            socket = _socket;
            socket.ReceiveBufferSize = dataBufferSize; // 수신 버퍼 크기 설정
            socket.SendBufferSize = dataBufferSize; // 송신 버퍼 크기 설정
            stream = socket.GetStream(); // 스트림 가져오기
            receiveBuffer = new byte[dataBufferSize]; // 버퍼 초기화
            receivedData = new Packet(); // Packet 초기화
            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallBack, null); // 비동기 읽기 시작

            // 서버에서 클라이언트에 환영 메시지 전송
            ServerSend.Welcome(id, "Welcome to the Server!");
        }

        // 데이터를 클라이언트에게 전송
        public void SendData(Packet _packet)
        {
            try
            {
                if (socket != null)
                {
                    stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null); // 비동기 데이터 전송
                }
            }
            catch (Exception e)
            {
                Debug.Log($"Error Sending Data To Player {id} via TCP : {e}");
            }
        }

        // 수신 데이터 처리
        private void ReceiveCallBack(IAsyncResult _result)
        {
            try
            {
                int _byteLength = stream.EndRead(_result); // 수신한 바이트 길이
                if (_byteLength <= 0)
                {
                    Server.clients[id].DisConnect(); // 데이터 없음 -> 연결 해제
                    return;
                }

                byte[] _data = new byte[_byteLength];
                Array.Copy(receiveBuffer, _data, _byteLength); // 수신된 데이터를 복사

                // 수신된 데이터를 처리하고, 비동기 읽기를 다시 시작
                receivedData.Reset(HandleData(_data));
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallBack, null);
            }
            catch (Exception e)
            {
                Debug.Log($"Error receiving TCP data: {e}");
                Server.clients[id].DisConnect(); // 오류 발생 시 연결 해제
            }
        }

        // 수신된 데이터 처리
        private bool HandleData(byte[] _data)
        {
            int _packetLength = 0;
            receivedData.SetBytes(_data); // 데이터를 Packet 객체에 설정

            // 패킷 길이 확인
            if (receivedData.UnreadLength() >= 4)
            {
                _packetLength = receivedData.ReadInt();
                if (_packetLength <= 0) return true;
            }

            // 패킷이 올바른 길이를 가지는 경우 데이터 처리
            while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
            {
                byte[] _packetData = receivedData.ReadBytes(_packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_packetData))
                    {
                        int _packetId = _packet.ReadInt();
                        Server.packetHandlers[_packetId](id, _packet); // 패킷 처리
                    }
                });

                // 다음 패킷 길이 확인
                _packetLength = 0;
                if (receivedData.UnreadLength() >= 4)
                {
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0) return true;
                }
            }

            // 남은 데이터가 없거나 패킷 길이가 1 이하일 경우 처리 완료
            return _packetLength <= 1;
        }

        // TCP 연결 해제
        public void DisConnect()
        {
            socket.Close();
            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }
    }

    // UDP 통신 클래스 (빠른 데이터 전송 담당)
    public class UDP
    {
        public IPEndPoint endPoint; // 클라이언트의 UDP 엔드포인트
        private int id; // 클라이언트 ID

        public UDP(int _id)
        {
            id = _id;
        }

        // UDP 연결 설정
        public void Connect(IPEndPoint _endPoint)
        {
            endPoint = _endPoint;
        }

        // 데이터를 클라이언트에게 전송
        public void SendData(Packet _packet)
        {
            Server.SendUDPData(endPoint, _packet);
        }

        // 수신 데이터 처리
        public void HandleData(Packet _packet)
        {
            int packetLength = _packet.ReadInt(); // 패킷 길이 읽기
            byte[] packetBytes = _packet.ReadBytes(packetLength); // 패킷 데이터 읽기
            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet _packet = new Packet(packetBytes))
                {
                    int _packetId = _packet.ReadInt();
                    Server.packetHandlers[_packetId](id, _packet); // 패킷 처리
                }
            });
        }

        // UDP 연결 해제
        public void DisConnect()
        {
            endPoint = null;
        }
    }

    // 게임에 클라이언트를 추가하고 초기화
    public void SendIntoGame(string _playerName)
    {
        player = NetworkManager.instance.InstantiatePlayer(); // 플레이어 생성
        player.Initialize(id, _playerName); // 플레이어 초기화

        // 기존 플레이어 정보를 새로운 클라이언트에 전송
        foreach (Client _client in Server.clients.Values)
        {
            if (_client.player != null)
            {
                if (_client.id != id)
                {
                    ServerSend.SpawnPlayer(id, _client.player);
                }
            }
        }

        // 새로운 플레이어 정보를 기존 클라이언트에 전송
        foreach (Client _client in Server.clients.Values)
        {
            if (_client.player != null)
            {
                ServerSend.SpawnPlayer(_client.id, player);
            }
        }

        // 아이템 스포너 상태를 클라이언트에 전송
        foreach (ItemSpawner _itemSpawner in ItemSpawner.spawners.Values)
        {
            ServerSend.CreateItemSpawner(id, _itemSpawner.spawnerId, _itemSpawner.transform.position, _itemSpawner.hasItem);
        }
    }

    // 클라이언트 연결 해제
    private void DisConnect()
    {
        Debug.Log($"{tcp.socket.Client.RemoteEndPoint} : disconnected"); // 로그 출력
        ThreadManager.ExecuteOnMainThread(() =>
        {
            UnityEngine.Object.Destroy(player.gameObject); // 플레이어 객체 삭제
            player = null;
        });

        tcp.DisConnect(); // TCP 연결 해제
        udp.DisConnect(); // UDP 연결 해제
        ServerSend.DisConnected(id); // 서버에 클라이언트 연결 해제 알림
    }
}
