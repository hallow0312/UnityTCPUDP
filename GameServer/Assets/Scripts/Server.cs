using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;

public class Server
{
    // 최대 플레이어 수 및 포트 번호를 나타내는 속성
    public static int MaxPlayers { get; private set; } // 최대 플레이어 수
    public static int Port { get; private set; } // 서버 포트 번호

    // 연결된 클라이언트를 관리하는 딕셔너리 및 패킷 핸들러
    public static Dictionary<int, Client> clients = new Dictionary<int, Client>(); // 클라이언트 목록
    public delegate void PacketHandler(int _fromClient, Packet _packet); // 패킷 처리용 델리게이트
    public static Dictionary<int, PacketHandler> packetHandlers; // 패킷 ID와 핸들러 연결

    // TCP 및 UDP 서버 객체
    public static TcpListener tcpListener; // TCP 서버 리스너
    private static UdpClient udpListener; // UDP 서버 리스너

    // 서버를 시작하는 메서드
    public static void Start(int _maxPlayer, int _port)
    {
        MaxPlayers = _maxPlayer; // 최대 플레이어 수 설정
        Port = _port; // 포트 번호 설정
        Debug.Log("Starting Server...");

        // 서버 초기화 (클라이언트 및 패킷 핸들러 설정)
        InitializeServerConnect();

        // TCP 서버 시작
        tcpListener = new TcpListener(IPAddress.Any, Port);
        tcpListener.Start();
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TcpConnectCallBack), null);

        // UDP 서버 시작
        udpListener = new UdpClient(Port);
        udpListener.BeginReceive(UDPReceiveCallBack, null);

        Debug.Log($"Server Connect on {Port}"); // 서버가 정상적으로 시작되었음을 알림
    }

    // UDP 데이터 수신 콜백
    private static void UDPReceiveCallBack(IAsyncResult _result)
    {
        try
        {
            // UDP 데이터를 보낸 클라이언트의 IP 엔드포인트
            IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);
            udpListener.BeginReceive(UDPReceiveCallBack, null); // 다음 수신 대기

            if (_data.Length < 4) // 데이터가 너무 짧을 경우 무시
            {
                return;
            }

            using (Packet _packet = new Packet(_data)) // 패킷 생성
            {
                int _clientId = _packet.ReadInt(); // 클라이언트 ID 읽기
                if (_clientId == 0) // 유효하지 않은 ID 무시
                {
                    return;
                }

                // 새 클라이언트라면 연결
                if (clients[_clientId].udp.endPoint == null)
                {
                    clients[_clientId].udp.Connect(_clientEndPoint);
                    return;
                }

                // 기존 클라이언트라면 데이터 처리
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

    // TCP 연결 수락 콜백
    public static void TcpConnectCallBack(IAsyncResult _result)
    {
        // 새로운 클라이언트 수락
        TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TcpConnectCallBack), null); // 다음 연결 대기

        Debug.Log($"Incoming connect from {_client.Client.RemoteEndPoint}...");

        // 빈 클라이언트 슬롯에 새 클라이언트를 배정
        for (int i = 1; i <= MaxPlayers; i++)
        {
            if (clients[i].tcp.socket == null) // 빈 슬롯 찾기
            {
                clients[i].tcp.Connect(_client); // 클라이언트 연결
                Debug.Log($"Client connected to slot {i}.");
                return;
            }
        }

        Debug.Log($"({_client.Client.RemoteEndPoint} failed to connect. Server full!!"); // 서버가 가득 찼음을 알림
    }

    // 서버 연결 초기화
    public static void InitializeServerConnect()
    {
        // 클라이언트 딕셔너리 초기화
        for (int i = 1; i <= MaxPlayers; i++)
        {
            clients.Add(i, new Client(i));
        }

        // 패킷 핸들러 초기화
        packetHandlers = new Dictionary<int, PacketHandler>()
        {
            {(int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived}, // 클라이언트 환영 메시지 처리
            {(int)ClientPackets.playerMovement, ServerHandle.PlayerMovement}, // 플레이어 이동 처리
            {(int)ClientPackets.playerShoot, ServerHandle.PlayerShoot}, // 플레이어 공격 처리
            {(int)ClientPackets.playerThrowItem, ServerHandle.PlayerThrowItem} // 아이템 던지기 처리
        };

        Debug.Log("Initialize Packets");
    }

    // UDP 데이터를 클라이언트로 전송
    public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet)
    {
        try
        {
            if (_clientEndPoint != null) // 유효한 엔드포인트인지 확인
            {
                udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Error Sending Data to {_clientEndPoint} via UDP  : {e}");
        }
    }

    // 서버 종료
    public static void Stop()
    {
        tcpListener.Stop(); // TCP 리스너 중지
        udpListener.Close(); // UDP 리스너 닫기
    }
}
