using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transform objectTransform;
    private void FixedUpdate()
    {
        SendInputToServer();
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Mouse0))
        {
            ClientSend.PlayerShoot(objectTransform.forward);
        }
        if(Input.GetKeyDown(KeyCode.Mouse1))
        {
            ClientSend.PlayerThrowItem(objectTransform.forward);
        }
    }
    private void SendInputToServer()
    {
        bool[] _inputs = new bool[]
        {   
            Input.GetKey(KeyCode.W),
            Input.GetKey(KeyCode.S),
            Input.GetKey(KeyCode.A),
            Input.GetKey(KeyCode.D),
            Input.GetKey(KeyCode.Space),
            
        };

        ClientSend.PlayerMovement(_inputs);
    }
}