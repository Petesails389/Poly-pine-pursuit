using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    private GameObject mainCamera;
    private PlayerMovement playerMovement;

    //pause logic bools
    private bool lastFrameEscape = false;
    private bool paused = false;

    // Start is called before the first frame update
    void Start()
    {

        //find various objects and components
        mainCamera = GameObject.Find("Camera");
        playerMovement = GetComponent<PlayerMovement>();

        //sorts out the cursor
        CursorLockUpdate();

        if (IsOwner) {
            playerMovement.RespawnServerRpc();
        }
    }

    // Update is called once per frame
    void Update()
    {
        //pause logic, including rising edge
        if ((Input.GetAxis("Escape") != 0f) && !lastFrameEscape) {
            PauseToggle();
        }
        lastFrameEscape = (Input.GetAxis("Escape") != 0f);

        if (paused) return; //return if paused
        
        mainCamera.GetComponent<cameraController>().Rotate(Input.GetAxis("Mouse Y"));
        playerMovement.RotateServerRpc(Input.GetAxis("Mouse X"));
        playerMovement.MoveServerRpc(Input.GetAxis("Horizontal"),Input.GetAxis("Vertical"));

        if (Input.GetAxis("Jump") != 0f) {
            playerMovement.JumpServerRpc();
        }

    }

    private void PauseToggle() {
        paused = !paused;
        CursorLockUpdate();
    }

    private void CursorLockUpdate() {
        if (paused) {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        } else {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
