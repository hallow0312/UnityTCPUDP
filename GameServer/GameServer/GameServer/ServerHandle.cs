using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
     class ServerHandle
    {
        public static void WelcomeReceived(int _fromClient, Packet packet)
        {
            int _clientIDCheck = packet.ReadInt();
            string _username = packet.ReadString();

            Console.WriteLine($"{Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully  and is now player{_fromClient}");
            if(_fromClient!= +_clientIDCheck)
            {
                Console.WriteLine($"Player\"{_username}\"(ID:{_fromClient})has assumed the wrong Client Id {_clientIDCheck}");
            }
            Server.clients[_fromClient].SendIntoGame(_username);
        }

       

        public static void PlayerMovement(int _fromClient, Packet _packet)
        {
            bool[] _inputs = new bool[_packet.ReadInt()];
            for(int i=0; i<_inputs.Length; i++)
            {
                _inputs[i]=_packet.ReadBool();
            }
            Quaternion _rotation = _packet.ReadQuaternion();
            Server.clients[_fromClient].player.SetInput(_inputs, _rotation);    
        }
    }
}
