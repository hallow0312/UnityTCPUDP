using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

/// <summary>
/// Ŭ���̾�Ʈ ��Ʈ��ũ ����� ����ϴ� Ŭ����
/// TCP�� UDP�� ���� ������ ������ �ۼ����� ����
/// </summary>
public class Client : MonoBehaviour
{
    public static Client instance; // �̱��� ������ ���� �ν��Ͻ�
    public static int dataBufferSize = 4096; // ������ ���� ũ�� (4KB)

    public string ip = "127.0.0.1"; // ���� IP �ּ�
    public int port = 26950; // ���� ��Ʈ ��ȣ
    public int myId = 0; // Ŭ���̾�Ʈ�� ���� ID
    public TCP tcp; // TCP ��� ��ü
    public UDP udp; // UDP ��� ��ü

    private bool isConnected = false; // ���� ���� Ȯ�� ����
    private delegate void PacketHandler(Packet _packet); // ��Ŷ ó�� �޼��� ��������Ʈ
    private static Dictionary<int, PacketHandler> packetHandlers; // ��Ŷ ID�� ó�� �޼��� ����

    // ���ø����̼� ���� �� ���� ����
    private void OnApplicationQuit()
    {
        DisConnect();
    }

    /// <summary>
    /// Ŭ���̾�Ʈ ���� ���� ó��
    /// </summary>
    private void DisConnect()
    {
        if (isConnected)
        {
            isConnected = false;
            tcp.socket.Close(); // TCP ���� ����
            udp.socket.Close(); // UDP ���� ����
            Debug.Log("DisConnect");
        }
    }

    // �̱��� �ν��Ͻ� �ʱ�ȭ
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

    // TCP�� UDP ��ü �ʱ�ȭ
    private void Start()
    {
        tcp = new TCP();
        udp = new UDP();
    }

    /// <summary>
    /// ������ ���� ����
    /// </summary>
    public void ConnectToServer()
    {
        InitializeClientData(); // ��Ŷ �ڵ鷯 �ʱ�ȭ
        isConnected = true;
        tcp.Connect(); // TCP ���� �õ�
    }

    /// <summary>
    /// TCP ��� Ŭ����
    /// ������ �������� ������ ���� ������ �ۼ���
    /// </summary>
    public class TCP
    {
        public TcpClient socket; // TCP Ŭ���̾�Ʈ ����

        private NetworkStream stream; // ������ ��Ʈ��
        private Packet receivedData; // ���� ������ ����
        private byte[] receiveBuffer; // ���� ����

        /// <summary>
        /// ������ TCP ���� ����
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

        // TCP ���� �Ϸ� �� ȣ��
        private void ConnectCallback(IAsyncResult _result)
        {
            socket.EndConnect(_result);

            if (!socket.Connected)
            {
                return;
            }

            stream = socket.GetStream(); // ������ ��Ʈ�� ����
            receivedData = new Packet(); // ���� ������ ��Ŷ �ʱ�ȭ

            // ������ ���� ���
            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
        }

        // ������ �۽�
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

        // ������ ���� �ݹ�
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

        // TCP ���� ����
        private void DisConnect()
        {
            instance.DisConnect();
            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }

        // ���� ������ ó��
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
                        packetHandlers[_packetId](_packet); // ��Ŷ ó��
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

    //UDP �� �� �ż�����ǿ����� TCP  �ż������ ���Ұ� ����ϱ⿡ �ּ� ����

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
    // ��Ŷ ó�� �ڵ鷯 �ʱ�ȭ
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