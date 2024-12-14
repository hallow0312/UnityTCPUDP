using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

/// <summary>
/// 클라이언트 네트워크 통신을 담당하는 클래스
/// TCP와 UDP를 통해 서버와 데이터 송수신을 수행
/// </summary>
public class Client : MonoBehaviour
{
    public static Client instance; // 싱글턴 패턴을 위한 인스턴스
    public static int dataBufferSize = 4096; // 데이터 버퍼 크기 (4KB)

    public string ip = "127.0.0.1"; // 서버 IP 주소
    public int port = 26950; // 서버 포트 번호
    public int myId = 0; // 클라이언트의 고유 ID
    public TCP tcp; // TCP 통신 객체
    public UDP udp; // UDP 통신 객체

    private bool isConnected = false; // 연결 상태 확인 변수
    private delegate void PacketHandler(Packet _packet); // 패킷 처리 메서드 델리게이트
    private static Dictionary<int, PacketHandler> packetHandlers; // 패킷 ID와 처리 메서드 매핑

    // 애플리케이션 종료 시 연결 해제
    private void OnApplicationQuit()
    {
        DisConnect();
    }

    /// <summary>
    /// 클라이언트 연결 종료 처리
    /// </summary>
    private void DisConnect()
    {
        if (isConnected)
        {
            isConnected = false;
            tcp.socket.Close(); // TCP 소켓 종료
            udp.socket.Close(); // UDP 소켓 종료
            Debug.Log("DisConnect");
        }
    }

    // 싱글턴 인스턴스 초기화
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    // TCP와 UDP 객체 초기화
    private void Start()
    {
        tcp = new TCP();
        udp = new UDP();
    }

    /// <summary>
    /// 서버에 연결 시작
    /// </summary>
    public void ConnectToServer()
    {
        InitializeClientData(); // 패킷 핸들러 초기화
        isConnected = true;
        tcp.Connect(); // TCP 연결 시도
    }

    /// <summary>
    /// TCP 통신 클래스
    /// 서버와 안정적인 연결을 통해 데이터 송수신
    /// </summary>
    public class TCP
    {
        public TcpClient socket; // TCP 클라이언트 소켓

        private NetworkStream stream; // 데이터 스트림
        private Packet receivedData; // 받은 데이터 저장
        private byte[] receiveBuffer; // 수신 버퍼

        /// <summary>
        /// 서버와 TCP 연결 설정
        /// </summary>
        public void Connect()
        {
            socket = new TcpClient
            {
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize
            };

            receiveBuffer = new byte[dataBufferSize];
            socket.BeginConnect(instance.ip, instance.port, ConnectCallback, socket);
        }

        // TCP 연결 완료 시 호출
        private void ConnectCallback(IAsyncResult _result)
        {
            socket.EndConnect(_result);

            if (!socket.Connected)
            {
                return;
            }

            stream = socket.GetStream(); // 데이터 스트림 열기
            receivedData = new Packet(); // 받은 데이터 패킷 초기화

            // 데이터 수신 대기
            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
        }

        // 데이터 송신
        public void SendData(Packet _packet)
        {
            try
            {
                if (socket != null)
                {
                    stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                }
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error sending data to server via TCP: {_ex}");
            }
        }

        // 데이터 수신 콜백
        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                int _byteLength = stream.EndRead(_result);
                if (_byteLength <= 0)
                {
                    instance.DisConnect();
                    return;
                }

                byte[] _data = new byte[_byteLength];
                Array.Copy(receiveBuffer, _data, _byteLength);

                receivedData.Reset(HandleData(_data));
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch
            {
                DisConnect();
            }
        }

        // TCP 연결 해제
        private void DisConnect()
        {
            instance.DisConnect();
            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }

        // 받은 데이터 처리
        private bool HandleData(byte[] _data)
        {
            int _packetLength = 0;
            receivedData.SetBytes(_data);

            if (receivedData.UnreadLength() >= 4)
            {
                _packetLength = receivedData.ReadInt();
                if (_packetLength <= 0)
                {
                    return true;
                }
            }

            while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
            {
                byte[] _packetBytes = receivedData.ReadBytes(_packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_packetBytes))
                    {
                        int _packetId = _packet.ReadInt();
                        packetHandlers[_packetId](_packet); // 패킷 처리
                    }
                });

                _packetLength = 0;
                if (receivedData.UnreadLength() >= 4)
                {
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0)
                    {
                        return true;
                    }
                }
            }

            if (_packetLength <= 1)
            {
                return true;
            }

            return false;
        }
    }

    //UDP 의 각 매서드들의역할은 TCP  매서드들의 역할과 비슷하기에 주석 생략

    public class UDP
    {
        public UdpClient socket;
        public IPEndPoint endPoint;

        public UDP()
        {
            endPoint = new IPEndPoint(IPAddress.Parse(instance.ip), instance.port);
        }

        public void Connect(int _localPort)
        {
            socket = new UdpClient(_localPort);

            socket.Connect(endPoint);
            socket.BeginReceive(ReceiveCallback, null);

            using (Packet _packet = new Packet())
            {
                SendData(_packet);
            }
        }

        public void SendData(Packet _packet)
        {
            try
            {
                _packet.InsertInt(instance.myId);
                if (socket != null)
                {
                    socket.BeginSend(_packet.ToArray(), _packet.Length(), null, null);
                }
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error sending data to server via UDP: {_ex}");
            }
        }

        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                byte[] _data = socket.EndReceive(_result, ref endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                if (_data.Length < 4)
                {
                    instance.DisConnect();
                    return;
                }

                HandleData(_data);
            }
            catch
            {
                DisConnect();
            }
        }
        private void DisConnect()
        {
            instance.DisConnect();
            endPoint = null;
            socket = null;
        }
        private void HandleData(byte[] _data)
        {
            using (Packet _packet = new Packet(_data))
            {
                int _packetLength = _packet.ReadInt();
                _data = _packet.ReadBytes(_packetLength);
            }

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet _packet = new Packet(_data))
                {
                    int _packetId = _packet.ReadInt();
                    packetHandlers[_packetId](_packet);
                }
            });
        }
    }
    // 패킷 처리 핸들러 초기화
    private void InitializeClientData()
    {
        packetHandlers = new Dictionary<int, PacketHandler>()
        {
            { (int)ServerPackets.welcome, ClientHandle.Welcome },
            { (int)ServerPackets.spawnPlayer, ClientHandle.SpawnPlayer },
            { (int)ServerPackets.playerPosition, ClientHandle.PlayerPosition },
            { (int)ServerPackets.playerRotation, ClientHandle.PlayerRotation },
            {(int)ServerPackets.playerDisconnected,ClientHandle.PlayerDisconnected },
            {(int)ServerPackets.playerHp,ClientHandle.PlayerHealth },
            {(int)ServerPackets.playerRespawn,ClientHandle.PlayerRespawned },
            {(int)ServerPackets.CreateItemSpawner,ClientHandle.CreateItemSpawner},
            {(int)ServerPackets.itemSpawned,ClientHandle.ItemSpawned},
            {(int)ServerPackets.ItemPickedUp,ClientHandle.ItemPickedUp},
            {(int)ServerPackets.spawnProjectile,ClientHandle.SpawnProjectile},
            {(int)ServerPackets.projectilePosition,ClientHandle.ProjectilePosition},
            {(int)ServerPackets.projectileExploded,ClientHandle.ProjectileExploded},


        };
        Debug.Log("Initialized packets.");
    }
}