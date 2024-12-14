using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;

public class Client
{
    public static int dataBufferSize = 4096; // ������ ���� ũ�� (�ִ� 4096����Ʈ)

    public int id; // Ŭ���̾�Ʈ ID
    public Player player; // Ŭ���̾�Ʈ�� �����ϴ� �÷��̾� ��ü
    public TCP tcp; // TCP ���� ���� ��ü
    public UDP udp; // UDP ���� ���� ��ü

    // Ŭ���̾�Ʈ ������, ID�� TCP/UDP ��ü�� �ʱ�ȭ
    public Client(int _clientId)
    {
        id = _clientId;
        tcp = new TCP(id);
        udp = new UDP(id);
    }

    // TCP ��� Ŭ���� (�������� ������ ���� ���)
    public class TCP
    {
        public TcpClient socket; // TCP ����
        private readonly int id; // Ŭ���̾�Ʈ ID
        private NetworkStream stream; // ��Ʈ��ũ ��Ʈ��
        private Packet receivedData; // ���ŵ� �����͸� ó���ϴ� Packet ��ü
        private byte[] receiveBuffer; // ���� �����͸� �����ϴ� ����

        public TCP(int _id)
        {
            id = _id;
        }

        // TCP ���� ����
        public void Connect(TcpClient _socket)
        {
            socket = _socket;
            socket.ReceiveBufferSize = dataBufferSize; // ���� ���� ũ�� ����
            socket.SendBufferSize = dataBufferSize; // �۽� ���� ũ�� ����
            stream = socket.GetStream(); // ��Ʈ�� ��������
            receiveBuffer = new byte[dataBufferSize]; // ���� �ʱ�ȭ
            receivedData = new Packet(); // Packet �ʱ�ȭ
            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallBack, null); // �񵿱� �б� ����

            // �������� Ŭ���̾�Ʈ�� ȯ�� �޽��� ����
            ServerSend.Welcome(id, "Welcome to the Server!");
        }

        // �����͸� Ŭ���̾�Ʈ���� ����
        public void SendData(Packet _packet)
        {
            try
            {
                if (socket != null)
                {
                    stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null); // �񵿱� ������ ����
                }
            }
            catch (Exception e)
            {
                Debug.Log($"Error Sending Data To Player {id} via TCP : {e}");
            }
        }

        // ���� ������ ó��
        private void ReceiveCallBack(IAsyncResult _result)
        {
            try
            {
                int _byteLength = stream.EndRead(_result); // ������ ����Ʈ ����
                if (_byteLength <= 0)
                {
                    Server.clients[id].DisConnect(); // ������ ���� -> ���� ����
                    return;
                }

                byte[] _data = new byte[_byteLength];
                Array.Copy(receiveBuffer, _data, _byteLength); // ���ŵ� �����͸� ����

                // ���ŵ� �����͸� ó���ϰ�, �񵿱� �б⸦ �ٽ� ����
                receivedData.Reset(HandleData(_data));
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallBack, null);
            }
            catch (Exception e)
            {
                Debug.Log($"Error receiving TCP data: {e}");
                Server.clients[id].DisConnect(); // ���� �߻� �� ���� ����
            }
        }

        // ���ŵ� ������ ó��
        private bool HandleData(byte[] _data)
        {
            int _packetLength = 0;
            receivedData.SetBytes(_data); // �����͸� Packet ��ü�� ����

            // ��Ŷ ���� Ȯ��
            if (receivedData.UnreadLength() >= 4)
            {
                _packetLength = receivedData.ReadInt();
                if (_packetLength <= 0) return true;
            }

            // ��Ŷ�� �ùٸ� ���̸� ������ ��� ������ ó��
            while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
            {
                byte[] _packetData = receivedData.ReadBytes(_packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_packetData))
                    {
                        int _packetId = _packet.ReadInt();
                        Server.packetHandlers[_packetId](id, _packet); // ��Ŷ ó��
                    }
                });

                // ���� ��Ŷ ���� Ȯ��
                _packetLength = 0;
                if (receivedData.UnreadLength() >= 4)
                {
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0) return true;
                }
            }

            // ���� �����Ͱ� ���ų� ��Ŷ ���̰� 1 ������ ��� ó�� �Ϸ�
            return _packetLength <= 1;
        }

        // TCP ���� ����
        public void DisConnect()
        {
            socket.Close();
            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }
    }

    // UDP ��� Ŭ���� (���� ������ ���� ���)
    public class UDP
    {
        public IPEndPoint endPoint; // Ŭ���̾�Ʈ�� UDP ��������Ʈ
        private int id; // Ŭ���̾�Ʈ ID

        public UDP(int _id)
        {
            id = _id;
        }

        // UDP ���� ����
        public void Connect(IPEndPoint _endPoint)
        {
            endPoint = _endPoint;
        }

        // �����͸� Ŭ���̾�Ʈ���� ����
        public void SendData(Packet _packet)
        {
            Server.SendUDPData(endPoint, _packet);
        }

        // ���� ������ ó��
        public void HandleData(Packet _packet)
        {
            int packetLength = _packet.ReadInt(); // ��Ŷ ���� �б�
            byte[] packetBytes = _packet.ReadBytes(packetLength); // ��Ŷ ������ �б�
            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet _packet = new Packet(packetBytes))
                {
                    int _packetId = _packet.ReadInt();
                    Server.packetHandlers[_packetId](id, _packet); // ��Ŷ ó��
                }
            });
        }

        // UDP ���� ����
        public void DisConnect()
        {
            endPoint = null;
        }
    }

    // ���ӿ� Ŭ���̾�Ʈ�� �߰��ϰ� �ʱ�ȭ
    public void SendIntoGame(string _playerName)
    {
        player = NetworkManager.instance.InstantiatePlayer(); // �÷��̾� ����
        player.Initialize(id, _playerName); // �÷��̾� �ʱ�ȭ

        // ���� �÷��̾� ������ ���ο� Ŭ���̾�Ʈ�� ����
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

        // ���ο� �÷��̾� ������ ���� Ŭ���̾�Ʈ�� ����
        foreach (Client _client in Server.clients.Values)
        {
            if (_client.player != null)
            {
                ServerSend.SpawnPlayer(_client.id, player);
            }
        }

        // ������ ������ ���¸� Ŭ���̾�Ʈ�� ����
        foreach (ItemSpawner _itemSpawner in ItemSpawner.spawners.Values)
        {
            ServerSend.CreateItemSpawner(id, _itemSpawner.spawnerId, _itemSpawner.transform.position, _itemSpawner.hasItem);
        }
    }

    // Ŭ���̾�Ʈ ���� ����
    private void DisConnect()
    {
        Debug.Log($"{tcp.socket.Client.RemoteEndPoint} : disconnected"); // �α� ���
        ThreadManager.ExecuteOnMainThread(() =>
        {
            UnityEngine.Object.Destroy(player.gameObject); // �÷��̾� ��ü ����
            player = null;
        });

        tcp.DisConnect(); // TCP ���� ����
        udp.DisConnect(); // UDP ���� ����
        ServerSend.DisConnected(id); // ������ Ŭ���̾�Ʈ ���� ���� �˸�
    }
}
