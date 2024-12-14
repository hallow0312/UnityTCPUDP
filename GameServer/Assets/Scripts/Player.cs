using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int id;
    public string username;
    public Transform shootOrigin;
    public CharacterController controller;
    public float gravity = -9.81f;
    public float moveSpeed = 5f;
    public float jumpSpeed = 5f;

    public float throwForce = 600.0f;
    public float Hp;
    public float MaxHp=100f;

    public int itemAmount = 0;
    public int maxItemAmount = 3;

    private bool[] inputs;
    private float yVelocity = 0;

    private void Start()
    {
        //gravity *= Time.fixedDeltaTime * Time.fixedDeltaTime;
        //moveSpeed *= Time.fixedDeltaTime;
        //jumpSpeed *= Time.fixedDeltaTime;
    }

    public void Initialize(int _id, string _username)
    {
        id = _id;
        username = _username;

        inputs = new bool[5];
        Hp = MaxHp;
    }

    /// <summary>Processes player input and moves the player.</summary>
    public void Update()
    {
        CalculateDirection();
    }
    private void CalculateDirection()
    {
        if (Hp <= 0.0f) return;
        Vector2 _inputDirection = Vector2.zero;
        if (inputs[0])
        {
            _inputDirection.y += 1;
        }
        if (inputs[1])
        {
            _inputDirection.y -= 1;
        }
        if (inputs[2])
        {
            _inputDirection.x -= 1;
        }
        if (inputs[3])
        {
            _inputDirection.x += 1;
        }

        Move(_inputDirection);
    }
    /// <summary>Calculates the player's desired movement direction and moves him.</summary>
    /// <param name="_inputDirection"></param>
    private void Move(Vector2 _inputDirection)
    {
        Vector3 _moveDirection = transform.right * _inputDirection.x + transform.forward * _inputDirection.y;
        _moveDirection *= moveSpeed;

        if (controller.isGrounded)
        {
            yVelocity = 0f;
            if (inputs[4])
            {
                yVelocity = jumpSpeed;
            }
        }

        yVelocity += gravity * Time.fixedDeltaTime; // 중력 적용

        _moveDirection.y = yVelocity * Time.fixedDeltaTime; // y 축 움직임 조정

        //_moveDirection.y = yVelocity;
        controller.Move(_moveDirection);

        ServerSend.PlayerPosition(this);
        ServerSend.PlayerRotation(this);
    }

    /// <summary>Updates the player input with newly received input.</summary>
    /// <param name="_inputs">The new key inputs.</param>
    /// <param name="_rotation">The new rotation.</param>
    public void SetInput(bool[] _inputs, Quaternion _rotation)
    {
        inputs = _inputs;
        transform.rotation = _rotation;
    }
    public void Shoot(Vector3 _viewDirection)
    {
        if (Hp <= 0.0f) return;
        if(Physics.Raycast(shootOrigin.position,_viewDirection,out RaycastHit _hit ,24f))
        {
            if(_hit.collider.CompareTag("Player"))
            {
                _hit.collider.GetComponent<Player>().TakeDamage(50.0f);
            }
        }
    }
    public void TakeDamage(float _damage)
    {
        if(Hp<=0.0f)
        {
            return;
        }

        Hp -= _damage;

        if(Hp<=0.0f)
        {
            Hp = 0.0f;
            controller.enabled = false;
            transform.position = new Vector3(0.0f, 1.0f, 0.0f); //respawn place
            ServerSend.PlayerPosition(this);
            StartCoroutine(Respawn());
        }

        ServerSend.PlayerHealth(this);
    }
    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(5.0f);

        Hp = MaxHp;
        controller.enabled = true;
        ServerSend.PlayerRespawned(this);
    }

    public bool AttemptPickupItem()
    {
        if (itemAmount >= maxItemAmount) return false;
        itemAmount++;
        return true;
    }
    public void ThrowItem(Vector3 _viewDirection)
    {
        if (Hp <= 0.0f) return;
        if(itemAmount>0)
        {
            itemAmount--;
            NetworkManager.instance.InistatiateProjectile(shootOrigin).Initialize(_viewDirection, throwForce, id);
        }
    }
}