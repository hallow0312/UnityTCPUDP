using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace GameServer
{
    class Player
    {
        public int id;
        public string username; //유저네임 

        public Vector3 position;
        public Quaternion rotation; //차원 벡터공간에서의 회전 queternion

        private float MoveSpeed = 5f / Constants.TICS_PER_SEC;
        private bool[] inputs;
        public Player(int _id, string _username , Vector3  _spawnPosition)
        {
            id = _id;
            username = _username;   
            position = _spawnPosition;
            rotation = Quaternion.Identity;
            inputs = new bool[4];
        }
        public void Update()
        {
            Vector2 InputDirection = Vector2.Zero;
            if (inputs[0])
            {
                InputDirection.Y += 1;
            }
            if (inputs[1])
            {
                InputDirection.Y -= 1;
            }
            if (inputs[2])
            {
                InputDirection.X -= 1;
            }
            if (inputs[3])
            {
                InputDirection.X -= 1;
            }
            Move(InputDirection);
        }
        private void Move(Vector2 _inputDirection)
        {
            Vector3 forward = Vector3.Transform(new Vector3(0, 0, 1), rotation);
            Vector3 _right = Vector3.Normalize(Vector3.Cross(forward, new Vector3(0, 1, 0)));
            Vector3 moveDirection = _right * _inputDirection.X + forward * _inputDirection.Y;
            position += moveDirection * moveDirection;

            ServerSend.PlayerPosition(this);
            ServerSend.PlayerRotation(this);

        }
        public void SetInputs(bool[] _inputs, Quaternion _rotation)
        {
            inputs = _inputs;
            rotation = _rotation;
        }
    }

}
