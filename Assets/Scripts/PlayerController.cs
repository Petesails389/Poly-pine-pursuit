using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private GameObject mainCamera;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = GameObject.Find("Camera");

        //sorts out the cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        mainCamera.GetComponent<cameraController>().Rotate(Input.GetAxis("Mouse Y"));
        GetComponent<PlayerMovement>().Rotate(Input.GetAxis("Mouse X"));
    }
}
