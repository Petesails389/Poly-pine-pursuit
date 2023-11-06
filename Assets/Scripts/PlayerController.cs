using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private GameObject mainCamera;
    private PlayerMovement playerMovement;

    // Start is called before the first frame update
    void Start()
    {
        //find various objects and components
        mainCamera = GameObject.Find("Camera");
        playerMovement = GetComponent<PlayerMovement>();

        //sorts out the cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        mainCamera.GetComponent<cameraController>().Rotate(Input.GetAxis("Mouse Y"));
        playerMovement.Rotate(Input.GetAxis("Mouse X"));
        playerMovement.Move(Input.GetAxis("Horizontal"),Input.GetAxis("Vertical"));

        if (Input.GetAxis("Jump") != 0f) {
            playerMovement.Jump();
        }

    }
}
