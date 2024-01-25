using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;


public class PlayerController : NetworkBehaviour
{
    //object references
    private GameObject mainCamera;
    private GameManager gameManager;
    private PlayerMovement playerMovement;


    //movement variables
    private bool jump;
    private float xMovement;
    private float zMovement;
    private float xRotationChange;
    private float yRotationChange;

    //misc variables
    private bool lastFrameEscape = false;

    // Start is called before the first frame update
    public override void OnStartClient()
    {
        base.OnStartClient();
        //sorts out object references
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        mainCamera = Camera.main.gameObject;
        playerMovement = gameObject.GetComponent<PlayerMovement>();

        if (base.IsOwner && Owner.IsValid) {
            //sorts out the camera
            mainCamera.transform.position = new Vector3(transform.position.x, transform.position.y + 1, transform.position.z);
            mainCamera.transform.SetParent(transform.Find("Head"));

            //hides various player commonents that shoudl only be visible to other players
            transform.Find("Body").GetComponent<Renderer>().enabled = false;
            transform.Find("Head").GetComponent<Renderer>().enabled = false;
            transform.Find("Head/Visor").GetComponent<Renderer>().enabled = false;
            
            //subscibes to ontick
            base.TimeManager.OnTick += TimeManager_OnTick;
        }
        else {
            this.enabled = false;
        }
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        base.TimeManager.OnTick -= TimeManager_OnTick;
    }

    private void Update()
    {
        if (!base.ClientManager.Started) return; //return if the client hasn't started yet
        if (gameManager.currentGameState != GameManager.GameState.Playing 
        && gameManager.currentGameState != GameManager.GameState.Paused) return;// return if the game isn't running

        
        //escape logic is local and runs regardless of if the game is paused
        if ((Input.GetAxis("Escape") != 0f) && !lastFrameEscape) {
            gameManager.ToggleLocalPause();
        }
        lastFrameEscape = (Input.GetAxis("Escape") != 0f);

        if (gameManager.currentGameState != GameManager.GameState.Playing)
        {
            //if paused or waiting then don't move or rotate camera
            xMovement = 0;
            zMovement = 0;
            xRotationChange = 0;
            yRotationChange = 0;
            jump = false;
        } else
        {
            if (Input.GetAxis("Jump") != 0f){
                jump = true;
            } else {
                jump = false;
            }

            xMovement = Input.GetAxis("Horizontal");
            zMovement = Input.GetAxis("Vertical");
            xRotationChange = Input.GetAxis("Mouse Y"); // y to x
            yRotationChange = Input.GetAxis("Mouse X"); // and x to y

            //crosshair zoom
            if (Input.GetAxis("Scope") != 0f)
            {
                mainCamera.GetComponent<cameraController>().ZoomIn();
            }
            else
            {
                mainCamera.GetComponent<cameraController>().ZoomOut();
            }
        }

        playerMovement.Rotate(xRotationChange,yRotationChange);
    }


    private void TimeManager_OnTick() {
        if (!base.ClientManager.Started) return; //return if the client hasn't started yet
        if (gameManager.currentGameState != GameManager.GameState.Playing 
        && gameManager.currentGameState != GameManager.GameState.Paused) return;// return if the game isn't running

        playerMovement.Move(jump,xMovement,zMovement);
    }

}
