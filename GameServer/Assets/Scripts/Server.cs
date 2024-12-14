using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;

public class Server
{
    // �ִ� �÷��̾� �� �� ��Ʈ ��ȣ�� ��Ÿ���� �Ӽ�
    public static int MaxPlayers { get; private set; } // �ִ� �÷��̾� ��
    public static int Port { get; private set; } // ���� ��Ʈ ��ȣ

    // ����� Ŭ���̾�Ʈ�� �����ϴ� ��ųʸ� �� ��Ŷ �ڵ鷯
    public static Dictionary<int, Client> clients = new Dictionary<int, Client>(); // Ŭ���̾�Ʈ ���
    public delegate void PacketHandler(int _fromClient, Packet _packet); // ��Ŷ ó���� ��������Ʈ
    public static Dictionary<int, PacketHandler> packetHandlers; // ��Ŷ ID�� �ڵ鷯 ����

    // TCP �� UDP ���� ��ü
    public static TcpListener tcpListener; // TCP ���� ������
    private static UdpClient udpListener; // UDP ���� ������

    // ������ �����ϴ� �޼���
    public static void Start(int _maxPlayer, int _port)
    {
        MaxPlayers = _maxPlayer; // �ִ� �÷��̾� �� ����
        Port = _port; // ��Ʈ ��ȣ ����
        Debug.Log("Starting Server...");

        // ���� �ʱ�ȭ (Ŭ���̾�Ʈ �� ��Ŷ �ڵ鷯 ����)
        InitializeServerConnect();

        // TCP ���� ����
        tcpListener = new TcpListener(IPAddress.Any, Port);
        tcpListener.Start();
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TcpConnectCallBack), null);

        // UDP ���� ����
        udpListener = new UdpClient(Port);
        udpListener.BeginReceive(UDPReceiveCallBack, null);

        Debug.Log($"Server Connect on {Port}"); // ������ ���������� ���۵Ǿ����� �˸�
    }

    // UDP ������ ���� �ݹ�
    private static void UDPReceiveCallBack(IAsyncResult _result)
    {
        try
        {
            // UDP �����͸� ���� Ŭ���̾�Ʈ�� IP ��������Ʈ
            IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);
            udpListener.BeginReceive(UDPReceiveCallBack, null); // ���� ���� ���

            if (_data.Length < 4) // �����Ͱ� �ʹ� ª�� ��� ����
            {
                return;
            }

            using (Packet _packet = new Packet(_data)) // ��Ŷ ����
            {
                int _clientId = _packet.ReadInt(); // Ŭ���̾�Ʈ ID �б�
                if (_clientId == 0) // ��ȿ���� ���� ID ����
                {
                    return;
                }

                // �� Ŭ���̾�Ʈ��� ����
                if (clients[_clientId].udp.endPoint == null)
                {
                    clients[_clientId].udp.Connect(_clientEndPoint);
                    return;
                }

                // ���� Ŭ���̾�Ʈ��� ������ ó��
                if (clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString())
                {
                    clients[_clientId].udp.HandleData(_packet);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"Error Receiving UDP data  : {ex} ");
        }
    }

    // TCP ���� ���� �ݹ�
    public static void TcpConnectCallBack(IAsyncResult _result)
    {
        // ���ο� Ŭ���̾�Ʈ ����
        TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TcpConnectCallBack), null); // ���� ���� ���

        Debug.Log($"Incoming connect from {_client.Client.RemoteEndPoint}...");

        // �� Ŭ���̾�Ʈ ���Կ� �� Ŭ���̾�Ʈ�� ����
        for (int i = 1; i <= MaxPlayers; i++)
        {
            if (clients[i].tcp.socket == null) // �� ���� ã��
            {
                clients[i].tcp.Connect(_client); // Ŭ���̾�Ʈ ����
                Debug.Log($"Client connected to slot {i}.");
                return;
            }
        }

        Debug.Log($"({_client.Client.RemoteEndPoint} failed to connect. Server full!!"); // ������ ���� á���� �˸�
    }

    // ���� ���� �ʱ�ȭ
    public static void InitializeServerConnect()
    {
        // Ŭ���̾�Ʈ ��ųʸ� �ʱ�ȭ
        for (int i = 1; i <= MaxPlayers; i++)
        {
            clients.Add(i, new Client(i));
        }

        // ��Ŷ �ڵ鷯 �ʱ�ȭ
        packetHandlers = new Dictionary<int, PacketHandler>()
        {
            {(int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived}, // Ŭ���̾�Ʈ ȯ�� �޽��� ó��
            {(int)ClientPackets.playerMovement, ServerHandle.PlayerMovement}, // �÷��̾� �̵� ó��
            {(int)ClientPackets.playerShoot, ServerHandle.PlayerShoot}, // �÷��̾� ���� ó��
            {(int)ClientPackets.playerThrowItem, ServerHandle.PlayerThrowItem} // ������ ������ ó��
        };

        Debug.Log("Initialize Packets");
    }

    // UDP �����͸� Ŭ���̾�Ʈ�� ����
    public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet)
    {
        try
        {
            if (_clientEndPoint != null) // ��ȿ�� ��������Ʈ���� Ȯ��
            {
                udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Error Sending Data to {_clientEndPoint} via UDP  : {e}");
        }
    }

    // ���� ����
    public static void Stop()
    {
        tcpListener.Stop(); // TCP ������ ����
        udpListener.Close(); // UDP ������ �ݱ�
    }
}
