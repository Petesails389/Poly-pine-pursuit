using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using System;


public class PlayerControllerCC : NetworkBehaviour
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
        public float VerticalVelocity;
        public Quaternion Rotation;
        public float XHeadRotation;
        public float YHeadRotation;

        public MoveReconcileData(Vector3 _position, float _verticalVelocity, Quaternion _rotation, float _XHeadRotation, float _YHeadRotation) {
            Position = _position;
            VerticalVelocity = _verticalVelocity;
            Rotation = _rotation;
            XHeadRotation = _XHeadRotation;
            YHeadRotation = _YHeadRotation;
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

    //reconciliation variables
    private float verticalVelocity;
    private float xHeadRotation;
    private float yHeadRotation;


    // Start is called before the first frame update
    public override void OnStartClient()
    {
        base.OnStartClient();
        //subscribes to time manager ontick and on post tick
        base.TimeManager.OnTick += TimeManager_OnTick;

        //sorts out object references
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        characterController = GetComponent<CharacterController>();


        if (base.IsOwner && Owner.IsValid) {
            //sorts out the camera
            mainCamera = Camera.main.gameObject;
            mainCamera.transform.position = new Vector3(transform.position.x, transform.position.y + 1, transform.position.z);
            mainCamera.transform.SetParent(transform);
            mainCamera.transform.SetParent(transform.Find("Prediction/Head"));

            //hides various player commonents that shoudl only be visible to other players
            transform.Find("Prediction/Body").GetComponent<Renderer>().enabled = false;
            transform.Find("Prediction/Head/GFX").GetComponent<Renderer>().enabled = false;
            transform.Find("Prediction/Head/Visor").GetComponent<Renderer>().enabled = false;
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
        if (!base.IsOwner) return;

        //escape logic is local and runs regardless of if the game is paused
        if ((Input.GetAxis("Escape") != 0f) && !lastFrameEscape) {
            gameManager.ToggleLocalPause();
        }
        lastFrameEscape = (Input.GetAxis("Escape") != 0f);

        //reset all values
        xMovement = 0;
        zMovement = 0;
        xRotationChange = 0;
        yRotationChange = 0;

        if (gameManager.currentGameState == GameManager.GameState.Paused) return;

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


    private void TimeManager_OnTick()
    {
        Move(BuildActions());
        /* The base.IsServer check is not required but does save a little
        * performance by not building the reconcileData if not server. */
        CreateReconcile();
    }

    public override void CreateReconcile()
    {
        if (base.IsServerStarted)
        {
            MoveReconcileData rd = new MoveReconcileData(transform.position, verticalVelocity, transform.rotation, xHeadRotation, yHeadRotation);
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
        jump = false;

        return md;
    }


    //replicate function that takes care of movement
    [Replicate]
    private void Move(MoveData moveData, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable) {
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


        //rotation logic
        float calculatedRotationX = (float) (-1*moveData.XRotation*sensitivity*delta); // calcuates how much to rotate by
        float calculatedRotationY = (float) (moveData.YRotation*sensitivity*delta); // calcuates how much to rotate by

        Vector3 oldEuler = transform.Find("Prediction/Head").rotation.eulerAngles; //gets the current rotation
        xHeadRotation = Mathf.Clamp(xHeadRotation + calculatedRotationX,-90f,90f); //calculates new x rotation
        transform.Find("Prediction/Head").rotation = Quaternion.Euler(xHeadRotation, oldEuler.y, oldEuler.z); //apllies the new x rotation

        oldEuler = transform.rotation.eulerAngles; //gets the current rotation
        yHeadRotation = oldEuler.y + calculatedRotationY; //calculates the new y rotation
        transform.rotation = Quaternion.Euler(oldEuler.x, yHeadRotation, oldEuler.z); //apllies the new y rotation

        //anticheat stuff in here
        if (Math.Abs(transform.position.x) > gameManager.GetSize() + 2f
        || Math.Abs(transform.position.z) > gameManager.GetSize() + 2f
        || transform.position.y < 0 || transform.position.y > 150)
        {
            //prevents out of bounds and flying
            transform.position = new Vector3(0, 50, 0);
        }
    }

    [Reconcile]
    private void Reconcile(MoveReconcileData rd, Channel channel = Channel.Unreliable)
    {
        //Reset the client to the received position. It's okay to do this
        //even if there is no de-synchronization.
        transform.position = rd.Position;
        verticalVelocity = rd.VerticalVelocity;
        transform.rotation = rd.Rotation;
        xHeadRotation = rd.XHeadRotation;
        yHeadRotation = rd.YHeadRotation;
    }
}
