using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Connection;
using FishNet.Object;


public class PlayerController : NetworkBehaviour
{
    private GameObject mainCamera;
    private PlayerMovement playerMovement;
    private GameManager gameManager;

    //escape rising edge
    private bool lastFrameEscape = false;

    // Start is called before the first frame update
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner) {
            //sorts out the camera
            mainCamera = Camera.main.gameObject;
            mainCamera.transform.position = new Vector3(transform.position.x, transform.position.y + 1, transform.position.z);
            mainCamera.transform.SetParent(transform.Find("Head"));

            //sorts out object references
            playerMovement = GetComponent<PlayerMovement>();
            gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

            //hides various player commonents that shoudl only be visible to other players
            transform.Find("Body").GetComponent<Renderer>().enabled = false;
            transform.Find("Head").GetComponent<Renderer>().enabled = false;
            transform.Find("Head/Visor").GetComponent<Renderer>().enabled = false;
        }
        else {
            GetComponent<PlayerController>().enabled = false;
        }
    }


    // PausedUpdateCall is called by the GameManager when in playing or paused GameState
    // this is called before UpdateCall
    public void PausedUpdateCall()
    {
        //pause logic, including rising edge
        if ((Input.GetAxis("Escape") != 0f) && !lastFrameEscape) {
            gameManager.ToggleLocalPause();
        }
        lastFrameEscape = (Input.GetAxis("Escape") != 0f);
    }

    // UpdateCall is called by the GameManager when in playing GameState
    public void UpdateCall() 
    {
        //crosshair zoom
        if (Input.GetAxis("Scope") != 0f) {
            mainCamera.GetComponent<cameraController>().ZoomIn();
        }
        else {
            mainCamera.GetComponent<cameraController>().ZoomOut();
        }

    }

    // MovementUpdateCall is called by GameManager during the fixed update when in playing GameState
    public void MovementUpdateCall() {
        playerMovement.RotateServerRpc(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y"));
        playerMovement.MoveServerRpc(Input.GetAxis("Horizontal"),Input.GetAxis("Vertical"));

        if (Input.GetAxis("Jump") != 0f) {
            playerMovement.JumpServerRpc();
        }
    }
}
