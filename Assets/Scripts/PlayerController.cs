using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    private GameObject mainCamera;
    private PlayerMovement playerMovement;
    private GameManager gameManager;

    //escape rising edge
    private bool lastFrameEscape = false;

    // Start is called before the first frame update
    public override void OnNetworkSpawn()
    {

        //find various objects and components
        mainCamera = GameObject.Find("Camera");
        playerMovement = GetComponent<PlayerMovement>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        //sorts out the cursor
        CursorLockUpdate();

        if (IsOwner) {
            playerMovement.RespawnServerRpc();
        }
        base.OnNetworkSpawn();
    }


    // Update is called once per frame
    void Update()
    {
        //pause logic, including rising edge
        if ((Input.GetAxis("Escape") != 0f) && !lastFrameEscape) {
            gameManager.ToggleLocalPause();
        }
        lastFrameEscape = (Input.GetAxis("Escape") != 0f);

        if (gameManager.IsPaused()) return; //return if paused

        //crosshair zoom
        if (Input.GetAxis("Scope") != 0f) {
            mainCamera.GetComponent<cameraController>().ZoomIn();
        }
        else {
            mainCamera.GetComponent<cameraController>().ZoomOut();
        }

    }

    // Used for movement code
    private void FixedUpdate() {
        if (gameManager.IsPaused()) return; //return if paused

        mainCamera.GetComponent<cameraController>().Rotate(Input.GetAxis("Mouse Y"));
        playerMovement.RotateServerRpc(Input.GetAxis("Mouse X"));
        playerMovement.MoveServerRpc(Input.GetAxis("Horizontal"),Input.GetAxis("Vertical"));

        if (Input.GetAxis("Jump") != 0f) {
            playerMovement.JumpServerRpc();
        }
    }

    public void CursorLockUpdate() {
        if (gameManager.IsPaused()) {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        } else {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
