using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;


public class PlayerController : NetworkBehaviour
{
    public struct MoveData : IReplicateData
    {
        public bool Jump;
        public float XMovement;
        public float ZMovement;
        public float XRotation;
        public float YRotation;
        public MoveData(bool _jump, float _xMovement, float _zMovement, float _xRotation, float _yRotation) {
            Jump = _jump;
            XMovement = _xMovement;
            ZMovement = _zMovement;
            XRotation = _xRotation;
            YRotation = _yRotation;
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
        public bool IsGrounded;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;

        public MoveReconcileData(Vector3 _position, Quaternion _playerRotation, float _headRotation, bool _isGrounded, Vector3 _velocity, Vector3 _angularVelocity) {
            Position = _position;
            PlayerRotation = _playerRotation;
            HeadRotation = _headRotation;
            IsGrounded = _isGrounded;
            Velocity = _velocity;
            AngularVelocity = _angularVelocity;
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

    private GameObject mainCamera;
    private GameManager gameManager;

    //escape rising edge
    private bool lastFrameEscape = false;

    private bool jump;
    private float xMovement;
    private float zMovement;
    private float xRotationChange;
    private float yRotationChange;

    // Start is called before the first frame update
    public override void OnStartClient()
    {
        base.OnStartClient();
        //subscribes to time manager ontick and on post tick
        base.TimeManager.OnTick += TimeManager_OnTick;
        base.TimeManager.OnPostTick  += TimeManager_OnPostTick;

        //sorts out object references
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        if (base.IsOwner && Owner.IsValid) {
            //sorts out the camera
            mainCamera = Camera.main.gameObject;
            mainCamera.transform.position = new Vector3(transform.position.x, transform.position.y + 1, transform.position.z);
            mainCamera.transform.SetParent(transform);

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
            base.TimeManager.OnPostTick  -= TimeManager_OnPostTick ;
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

            if (gameManager.currentGameState == GameManager.GameState.Paused) return; //return if the game is paused

            if (Input.GetAxis("Jump") != 0f) {
                jump = true;
            }
            xMovement = Input.GetAxis("Horizontal");
            zMovement = Input.GetAxis("Vertical");
            xRotationChange = Input.GetAxis("Mouse Y"); // y to x
            yRotationChange = Input.GetAxis("Mouse X"); // and x to y

            //crosshair zoom
            if (Input.GetAxis("Scope") != 0f) {
                mainCamera.GetComponent<cameraController>().ZoomIn();
            }
            else {
                mainCamera.GetComponent<cameraController>().ZoomOut();
            }
        }
    }

    private void TimeManager_OnTick()
    {
        Move(BuildActions());
    }

    private void TimeManager_OnPostTick(){
        if (base.IsServerInitialized)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            MoveReconcileData rd = new MoveReconcileData(transform.position, transform.rotation, xRotation, isGrounded, rb.velocity, rb.angularVelocity);
            Reconcile(rd);
        }
    }

    private MoveData BuildActions() {
        if (!base.IsOwner)
        return default;

        MoveData md = new MoveData(jump, xMovement, zMovement, xRotationChange, yRotationChange);
        //reset queued values
        jump = false;

        return md;
    }

    /// <summary>
    /// movement script follows
    /// </summary>

    [SerializeField] private float sensitivity;
    [SerializeField] private float speed;
    [SerializeField] private float jumpForce;

    private bool isGrounded = true;
    private float xRotation;

    //this is triggered anytime the ground check touches anything 
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Ground") {
            isGrounded = true;
        }
    }

    //replicate function that takes care of movement
    [ReplicateV2]
    private void Move(MoveData moveData, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable) {
        if (gameManager.currentGameState == GameManager.GameState.Waiting) return; //return if waiting
        //jump logic
        if (isGrounded && moveData.Jump) {
            GetComponent<Rigidbody>().AddForce(0,jumpForce,0,ForceMode.Impulse);
            isGrounded = false;
        }

        return;
        //move logic
        Vector3 X = transform.right*moveData.XMovement*speed; //x velocity based off user inputs
        Vector3 Y = transform.up * GetComponent<Rigidbody>().velocity.y; //y velocity remains the same as the rigid body thinks it should be
        Vector3 Z = transform.forward*moveData.ZMovement*speed; //z velocity based off user inputs
        GetComponent<Rigidbody>().velocity = X + Y + Z; //apply that velocity to the rigidbody

        //rotation logic
        float calculatedRotationX = (float) (-1*moveData.XRotation*sensitivity); // calcuates how much to rotate by
        float calculatedRotationY = (float) (moveData.YRotation*sensitivity); // calcuates how much to rotate by

        Vector3 oldEuler = transform.Find("Head").rotation.eulerAngles; //gets the current rotation
        xRotation = Mathf.Clamp(xRotation + calculatedRotationX,-90f,90f); //calculates new rotation
        transform.Find("Head").rotation = Quaternion.Euler(xRotation,oldEuler.y,oldEuler.z); //aplies the new rotation

        oldEuler = transform.rotation.eulerAngles; //gets the current rotation
        float yRotation = oldEuler.y + calculatedRotationY; //calculates the new rotation
        transform.rotation = Quaternion.Euler(oldEuler.x,yRotation,oldEuler.z); //apllies the new rotation
    }

    [ReconcileV2]
    private void Reconcile(MoveReconcileData rd, Channel channel = Channel.Unreliable)
    {
        //Reset the client to the received position. It's okay to do this
        //even if there is no de-synchronization.
        transform.position = rd.Position;
        transform.rotation = rd.PlayerRotation;
        //xRotation = rd.HeadRotation;
        isGrounded = rd.IsGrounded;
        transform.GetComponent<Rigidbody>().velocity = rd.Velocity;
        transform.GetComponent<Rigidbody>().angularVelocity = rd.AngularVelocity;
    }
}
