﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace GameServer
{
    class Client1
    {
        public static int dataBufferSize = 4096; //data size
       
        public int id;
        public Player player;
        public TCP tcp;
        public UDP udp;
        public Client1(int _clientId)
        {
            id = _clientId;
            tcp = new TCP(id);
            udp = new UDP(id);
        }
        public class TCP
        {
            public TcpClient socket;

            private readonly int id;
            private NetworkStream stream;
            private Packet receivedData;
            private byte[] receiveBuffer;

            public TCP(int _id)
            {
                id = _id;
            }

            public void Connect(TcpClient _socket)
            {
                socket = _socket;
                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;
                stream = socket.GetStream();
                receiveBuffer = new byte[dataBufferSize];
                receivedData = new Packet();
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallBack, null);

                ServerSend.Welcome(id, "Welcom to the Server!");
            }
            public void SendData(Packet _packet)
            {
                try
                {
                    if (socket != null)
                    {
                        stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error Sending Data To Player {id} via tcp : {e}");
                }
            }
            private void ReceiveCallBack(IAsyncResult _result)
            {
                try
                {
                    int _byteLength = stream.EndRead(_result);
                    if (_byteLength <= 0)
                    {
                        Server.clients[id].DisConnect();
                        return;
                    }
                    byte[] _data = new byte[_byteLength];
                    Array.Copy(receiveBuffer, _data, _byteLength);

                    receivedData.Reset(HandleData(_data));
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallBack, null);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error receiving TCP data {e}");
                    Server.clients[id].DisConnect();
                }
            }

            private bool HandleData(byte[] _data)
            {
                int _packetLength = 0;
                receivedData.SetBytes(_data);
                if (receivedData.UnreadLength() >= 4)
                {
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0) return true;

                }
                while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
                {
                    byte[] _packetData = receivedData.ReadBytes(_packetLength);
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        using (Packet _packet = new Packet(_packetData))
                        {
                            int _packetId = _packet.ReadInt();
                           Server.packetHandlers[_packetId](id,_packet);
                        }
                    });
                    _packetLength = 0;
                    if (receivedData.UnreadLength() >= 4)
                    {
                        _packetLength = receivedData.ReadInt();
                        if (_packetLength <= 0) return true;

                    }
                }
                if (_packetLength <= 1) return true;

                return false;

            }

            public void DisConnect()
            {
                socket.Close();
                stream = null;
                receivedData = null;
                receiveBuffer = null;
                socket = null;
            }

        }
        public class UDP
        {
            public IPEndPoint endPoint;
            private int id;
            public UDP(int _id)
            {
                id = _id;
            }
            public void Connect(IPEndPoint _endPoint)
            {
                endPoint = _endPoint;
               
            }
            public void SendData(Packet _packet)
            {
                Server.SendUDPData(endPoint, _packet);
            }
            public void HandleData(Packet _packet)
            {
                int packetLength = _packet.ReadInt();
                byte[] packetBytes = _packet.ReadBytes(packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(packetBytes))
                    {
                        int _packetId= _packet.ReadInt();
                        Server.packetHandlers[_packetId](id,_packet);
                    }
                });
            }
            public void DisConnect()
            {
                 endPoint = null;
            }
        }

        public void SendIntoGame(string  _playerName)
        {
            player =new Player(id, _playerName,new Vector3(0,0,0));

            foreach(Client1 _client in Server.clients.Values)
            {
                if (_client.player!=null)
                {
                    if(_client.id!=id)
                    {
                        ServerSend.SpawnPlayer(id,_client.player);
                    }
                }

            }
            foreach(Client1 _client in Server.clients.Values)
            {
                if (_client.player!=null)
                {
                    ServerSend.SpawnPlayer(_client.id, player); 
                }
            }
        }

        private void DisConnect()
        {
            Console.WriteLine($"{tcp.socket.Client.RemoteEndPoint} : disconnected");

            player = null;
            tcp.DisConnect();
            udp.DisConnect();
        }
    }
}
