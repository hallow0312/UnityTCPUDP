using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
namespace GameServer
{
    class Server
    { 
        public static int MaxPlayers { get; private set; } //max player 
        public static int Port { get; private set; } //port number 
        public static Dictionary<int, Client1> clients = new Dictionary<int, Client1>();
        public delegate void PacketHandler(int _fromClient, Packet _packet);
        public static Dictionary<int, PacketHandler> packetHandlers;
        public static TcpListener tcpListener;
        private static UdpClient udpListener;
        public static void Start(int _maxPlayer,int _port)
        {
            MaxPlayers = _maxPlayer;
            Port = _port;
            Console.WriteLine("Starting Server...");
            InitializeServerConnect();
            tcpListener = new TcpListener(IPAddress.Any,Port);
            tcpListener.Start();

            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TcpConnectCallBack), null);

            udpListener = new UdpClient(Port);
            udpListener.BeginReceive(UDPReceiveCallBack, null);
            Console.WriteLine($"Server Connect on {Port}"); //server connect check
        }

        private static void UDPReceiveCallBack(IAsyncResult _result)
        {
            try
            {
                IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);
                udpListener.BeginReceive(UDPReceiveCallBack, null);
                if(_data.Length<4)
                {
                    return;
                }
                using(Packet _packet =new Packet(_data))
                {
                    int _clientId = _packet.ReadInt();
                    if(_clientId==0)
                    {
                        return;
                    }
                    if (clients[_clientId].udp.endPoint == null)
                    {
                        clients[_clientId].udp.Connect(_clientEndPoint);
                        return;
                    }
                    if (clients[_clientId].udp.endPoint.ToString()==_clientEndPoint.ToString())
                    {
                        clients[_clientId].udp.HandleData(_packet);
                    }
                }
             }
            catch(Exception ex) 
            {
                Console.WriteLine($"Error Receiving UDP data  : {ex} ");
            }
        }

        public static void TcpConnectCallBack(IAsyncResult _result)
        {
            TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TcpConnectCallBack), null);

            Console.WriteLine($"Incoming connect from {_client.Client.RemoteEndPoint}...");

            for (int i = 1; i <= MaxPlayers; i++)
            {
                //check 
                if (clients[i].tcp.socket == null)
                {
                    clients[i].tcp.Connect(_client);
                    Console.WriteLine($"Client connected to slot {i}.");
                    return;
                }
            }

            Console.WriteLine($"({_client.Client.RemoteEndPoint}failed to connect Server full!!");
        

        }
        public static void InitializeServerConnect()
        {
            for (int i = 1; i <= MaxPlayers; i++)
            {
                clients.Add(i, new Client1(i));
            }
            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                {(int)ClientPackets.welcomeReceived,ServerHandle.WelcomeReceived },
                {(int)ClientPackets.playerMovement,ServerHandle.PlayerMovement },


            };
            Console.WriteLine("Initialize Packets");
        }

        public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet)
        {
            try
            {
                if (_clientEndPoint != null)
                {
                    udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error Sending Data to {_clientEndPoint} via  UDP  : {e}");
            }
        }
    }
}

