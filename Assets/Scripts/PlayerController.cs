using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
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

        if (IsOwner) {
            playerMovement.RespawnServerRpc();
        }
    }

    // Update is called once per frame
    void Update()
    {
        mainCamera.GetComponent<cameraController>().Rotate(Input.GetAxis("Mouse Y"));
        playerMovement.RotateServerRpc(Input.GetAxis("Mouse X"));
        playerMovement.MoveServerRpc(Input.GetAxis("Horizontal"),Input.GetAxis("Vertical"));

        if (Input.GetAxis("Jump") != 0f) {
            playerMovement.JumpServerRpc();
        }

    }
}
