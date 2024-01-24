using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using System;


public class PlayerController : NetworkBehaviour
{
    public struct MoveData : IReplicateData
    {
        public uint SentTick;
        public bool Jump;
        public float XMovement;
        public float ZMovement;
        public float XRotation;
        public float YRotation;   
        public MoveData(bool _jump, float _xMovement, float _zMovement, float _xRotation, float _yRotation, uint _sentTick) {
            Jump = _jump;
            XMovement = _xMovement;
            ZMovement = _zMovement;
            XRotation = _xRotation;
            YRotation = _yRotation;
            SentTick = _sentTick;
            _tick = 0;
        }
        
        /* Everything below this is required for
        * the interface. You do not need to implement
        * Dispose, it is there if you want to clean up anything
        * that may allocate when this structure is discarded. */
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    public struct MoveReconcileData : IReconcileData
    {
        public Vector3 Position;
        public Quaternion PlayerRotation;
        public float HeadRotation;
        public float VerticalVelocity;

        public MoveReconcileData(Vector3 _position, Quaternion _playerRotation, float _headRotation, float _verticalVelocity) {
            Position = _position;
            PlayerRotation = _playerRotation;
            HeadRotation = _headRotation;
            VerticalVelocity = _verticalVelocity;
            _tick = 0;
        }
        
        /* Everything below this is required for
        * the interface. You do not need to implement
        * Dispose, it is there if you want to clean up anything
        * that may allocate when this structure is discarded. */
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }
    //movement attributes
    [SerializeField] private float sensitivity;
    [SerializeField] private float speed;
    [SerializeField] private float jumpForce;

    //object references
    private GameObject mainCamera;
    private GameManager gameManager;
    private CharacterController characterController;

    //escape rising edge
    private bool lastFrameEscape = false;

    //movement variables
    private bool jump;
    private float xMovement;
    private float zMovement;
    private float xRotationChange;
    private float yRotationChange;
    //internal tracking
    private float verticalVelocity;
    private float xRotation;



    // Start is called before the first frame update
    public override void OnStartClient()
    {
        base.OnStartClient();
        //subscribes to time manager ontick and on post tick
        base.TimeManager.OnTick += TimeManager_OnTick;

        //sorts out object references
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        characterController = GetComponent<CharacterController>();


        if (base.IsOwner && Owner.IsValid && false) {
            //sorts out the camera
            mainCamera = Camera.main.gameObject;
            mainCamera.transform.position = new Vector3(transform.position.x, transform.position.y + 1, transform.position.z);
            //mainCamera.transform.SetParent(transform.Find("Head/GFX"));

            //hides various player commonents that shoudl only be visible to other players
            transform.Find("Body").GetComponent<Renderer>().enabled = false;
            transform.Find("Head/GFX").GetComponent<Renderer>().enabled = false;
            transform.Find("Head/GFX/Visor").GetComponent<Renderer>().enabled = false;
        }
    }
    
    
    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        if (base.TimeManager != null){
            //unsubscribe from timemanager ticks
            base.TimeManager.OnTick -= TimeManager_OnTick;
        }
    }

    private void Update()
    {
        if (base.IsOwner){
            //escape logic is local and runs regardless of if the game is paused
            if ((Input.GetAxis("Escape") != 0f) && !lastFrameEscape) {
                gameManager.ToggleLocalPause();
            }
            lastFrameEscape = (Input.GetAxis("Escape") != 0f);

            Debug.Log(transform.Find("Head").rotation.eulerAngles);

            if (gameManager.currentGameState == GameManager.GameState.Paused)
            {
                //if paused then don't move or rotate camera
                xMovement = 0;
                zMovement = 0;
                xRotationChange = 0;
                yRotationChange = 0;
            } else
            {
                if (Input.GetAxis("Jump") != 0f)
                {
                    jump = true;
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
        }
    }

    private void TimeManager_OnTick()
    {
        Move(BuildActions());
        if (base.IsServerInitialized)
        {
            MoveReconcileData rd = new MoveReconcileData(transform.position, transform.rotation, xRotation, verticalVelocity);
            Reconcile(rd);
        }
    }

    private MoveData BuildActions() {
        if (!base.IsOwner)
        return default;


        MoveData md;
        if (xMovement != 0 || zMovement != 0)
            md = new MoveData(jump, xMovement, zMovement, xRotationChange, yRotationChange, base.TimeManager.LocalTick);
        else
            md = new MoveData(jump, xMovement, zMovement, xRotationChange, yRotationChange, 0);

        //reset queued values
        jump = false;

        return md;
    }


    //replicate function that takes care of movement
    [ReplicateV2]
    private void Move(MoveData moveData, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable) {
        if (state == ReplicateState.Future) return; //return if the replicatestate is in the future
        if (gameManager.currentGameState == GameManager.GameState.Waiting) return; //return if waiting

        float delta = (float)base.TimeManager.TickDelta;// time since last tick

        //jump logic
        if (characterController.isGrounded) {
            if (moveData.Jump)
                verticalVelocity = jumpForce; //if jumping from the ground jump
            else
                verticalVelocity = 0; //if not jumping and on the ground don't fall
        } else {
            verticalVelocity += (Physics.gravity.y * delta); //if not grounded then fall
            if (verticalVelocity < -20f)
                verticalVelocity = -20f; // sets a terminal velocity
        }
        
        //move logic
        Vector3 X = transform.right*moveData.XMovement*speed; //x velocity based off user inputs
        Vector3 Y = transform.up * verticalVelocity*5; //y based off internally tracked y velocity
        Vector3 Z = transform.forward*moveData.ZMovement*speed; //z velocity based off user inputs
        characterController.Move((X + Y + Z) * delta); //move the player

        return;
        //rotation logic
        float calculatedRotationX = (float) (-1*moveData.XRotation*sensitivity*delta); // calcuates how much to rotate by
        float calculatedRotationY = (float) (moveData.YRotation*sensitivity*delta); // calcuates how much to rotate by

        Vector3 oldEuler = transform.Find("Head").rotation.eulerAngles; //gets the current rotation
        xRotation = Mathf.Clamp(xRotation + calculatedRotationX,-90f,90f); //calculates new x rotation
        transform.Find("Head").rotation = Quaternion.Euler(xRotation, oldEuler.y, oldEuler.z); //aplies the new x rotation

        oldEuler = transform.rotation.eulerAngles; //gets the current rotation
        float yRotation = oldEuler.y + calculatedRotationY; //calculates the new y rotation
        transform.rotation = Quaternion.Euler(oldEuler.x,yRotation,oldEuler.z); //apllies the new y rotation

        //anticheat stuff in here
        if (Math.Abs(transform.position.x) > gameManager.GetSize() + 2f
        || Math.Abs(transform.position.z) > gameManager.GetSize() + 2f
        || transform.position.y < 0 || transform.position.y > 150)
        {
            //prevents out of bounds and flying
            transform.position = new Vector3(0, 50, 0);
        }
    }

    [ReconcileV2]
    private void Reconcile(MoveReconcileData rd, Channel channel = Channel.Unreliable)
    {
        //Reset the client to the received position. It's okay to do this
        //even if there is no de-synchronization.
        transform.position = rd.Position;
        transform.rotation = rd.PlayerRotation;
        xRotation = rd.HeadRotation;
        verticalVelocity = rd.VerticalVelocity;
    }
}
