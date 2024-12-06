using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class CameraManager : MonoBehaviour
{
    public PlayerManager player;
    public float sensivity = 100f;
    public float clampAngle = 85f;

    private float verticalRotation;
    private float horizontalRotation;

    private void Start()
    {
        verticalRotation = transform.localEulerAngles.x;
        horizontalRotation = transform.localEulerAngles.y;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorMode();
        }
        if(Cursor.lockState==CursorLockMode.Locked)
        {
            Look();
        }
   
        Debug.DrawRay(transform.position, transform.forward * 2, Color.red);
    }
    private void Look()
    {
        float _mouseVertical = -Input.GetAxis("Mouse Y");
        float _mouseHorizontal = Input.GetAxis("Mouse X");

        verticalRotation += _mouseVertical * sensivity * Time.deltaTime;
        horizontalRotation += _mouseHorizontal * sensivity * Time.deltaTime;

        verticalRotation = Mathf.Clamp(verticalRotation, -clampAngle, clampAngle);

        transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
        player.transform.rotation = Quaternion.Euler(0, horizontalRotation, 0);

    }
    private void ToggleCursorMode()
    {
        Cursor.visible=!Cursor.visible;
        if(Cursor.lockState==CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState=CursorLockMode.None;
        }
    }

}
