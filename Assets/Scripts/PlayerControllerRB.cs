using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Component.Prediction;
using FishNet.Transporting;
using System;

public class PlayerControllerRB : NetworkBehaviour
{
    public struct MoveData : IReplicateData
    {
        public bool Jump;
        public float Horizontal;
        public float Vertical;
        public float Yaw;
        public float Pitch;
        public MoveData(bool jump, float horizontal, float vertical, float yaw, float pitch)
        {
            Jump = jump;
            Horizontal = horizontal;
            Vertical = vertical;
            Yaw = yaw;
            Pitch = pitch;
            _tick = 0;
        }

        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    public struct ReconcileData : IReconcileData
    {
        public RigidbodyState RigidbodyState;
        public PredictionRigidbody PredictionRigidbody;
        public Quaternion Rotation;
        
        public ReconcileData(PredictionRigidbody pr, Quaternion _rotation)
        {
            RigidbodyState = new RigidbodyState(pr.Rigidbody);
            PredictionRigidbody = pr;
            Rotation = _rotation;
            _tick = 0;
        }

        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    [SerializeField]
    [Tooltip("X/Z being forward/backwards and straifing and Y being jump velocity.")] 
    private Vector3 moveSpeed = new Vector3(15,15,15);
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float _sensitivity = 15f;

    private PredictionRigidbody PredictionRigidbody { get; set; } = new();
    private bool _jump;
    private bool lastFrameEscape;
    private bool isGrounded = false;

    //OBJECT/component REFERENCES
    private GameManager gameManager;
    private NetworkCollision networkCollision;

    private void Awake()
    {
        PredictionRigidbody.Initialize(GetComponent<Rigidbody>()); //innit prediction RB

        networkCollision = GetComponent<NetworkCollision>(); //find network collider
        // Subscribe to the desired collision event
        networkCollision.OnEnter += NetworkCollisionEnter;
        networkCollision.OnExit += NetworkCollisionExit;
    }

    public override void OnStartNetwork()
    {
        base.TimeManager.OnTick += TimeManager_OnTick;
        base.TimeManager.OnPostTick += TimeManager_OnPostTick;
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner) return; //don't do anything unless this is the owned player

        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>(); //find game manager

        //sorts out the camera
        GameObject mainCamera = Camera.main.gameObject;
        mainCamera.transform.position = new Vector3(transform.position.x, transform.position.y + 0.7f, transform.position.z);
        mainCamera.transform.SetParent(transform);
        mainCamera.transform.SetParent(transform.Find("Prediction"));
    }

    public override void OnStopNetwork()
    {
        base.TimeManager.OnTick -= TimeManager_OnTick;
        base.TimeManager.OnPostTick -= TimeManager_OnPostTick;
    }

    private void Update()
    {
        if (base.IsOwner)
        {
            //escape logic
            if ((Input.GetAxis("Escape") != 0f) && !lastFrameEscape) {
                gameManager.ToggleLocalPause();
            }
            lastFrameEscape = (Input.GetAxis("Escape") != 0f);
            
            //get jump input
            if (Input.GetAxis("Jump") != 0f)
                _jump = true;
        }
    }

    private void TimeManager_OnTick()
    {
        Move(BuildMoveData());
    }

    private void TimeManager_OnPostTick()
    {
        CreateReconcile();
    }

    private void NetworkCollisionEnter(Collider other)
    {  
        //if it hit the floor then it's grounded
        if (other.tag == "Ground") isGrounded = true;
        if (IsOwner || IsServerInitialized) Debug.Log(isGrounded, gameObject);
    }

    private void NetworkCollisionExit(Collider other)
    {
        //if it had been hitting the floor and isn't anymore then it's no longer grounded
        if (other.tag == "Ground") isGrounded = false;
        if (IsOwner || IsServerInitialized) Debug.Log(isGrounded, gameObject);
    }
    
    private MoveData BuildMoveData()
    {
        if (!base.IsOwner || gameManager.currentGameState == GameManager.GameState.Paused)
            return default;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        float yaw = Input.GetAxis("Mouse X"); //rotation of the camera
        float pitch = Input.GetAxis("Mouse Y"); //rotation of the camera
        MoveData md = new MoveData(_jump, horizontal, vertical, yaw, pitch);
        _jump = false;

        return md;
    }

    public override void CreateReconcile() 
    {
        /* The base.IsServer check is not required but does save a little
        * performance by not building the reconcileData if not server. */
        if (IsServerInitialized)
        {
            ReconcileData rd = new ReconcileData(PredictionRigidbody, transform.rotation);
            Reconciliation(rd);
        }
    }

    [Replicate]
    private void Move(MoveData md, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
    {
        //get velocity
        Vector3 velocity = GetComponent<Rigidbody>().velocity;
        //calculate speed
        float speed = (float) Math.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z);

        //rotate the player
        Vector3 oldEuler = transform.rotation.eulerAngles;
        float calculatedRotationY = (float) (md.Yaw*_sensitivity); // calcuates how much to rotate by
        float yHeadRotation = oldEuler.y + calculatedRotationY;
        transform.rotation = Quaternion.Euler(oldEuler.x, yHeadRotation, oldEuler.z);

        //move the player in x and z
        float x = md.Horizontal * (moveSpeed.x - speed);
        float z = md.Vertical * (moveSpeed.z - speed);
        Vector3 forces = new Vector3(x, 0, z) * acceleration;
        PredictionRigidbody.AddRelativeForce(forces);

        //jump logic
        if (md.Jump && isGrounded)
        {
            //isGrounded = false;
            GetComponent<Rigidbody>().velocity = new Vector3(velocity.x, moveSpeed.y, velocity.z);
            //PredictionRigidbody.Velocity(xyz);
            if (IsOwner || IsServerInitialized) Debug.Log("jump");
        }

        //Add gravity to make the object fall faster.
        PredictionRigidbody.AddForce(Physics.gravity * 4f);
        //Simulate the added forces.
        PredictionRigidbody.Simulate();
    }

    [Reconcile]
    private void Reconciliation(ReconcileData rd, Channel channel = Channel.Unreliable)
    {
        //Sets state of transform and rigidbody.
        Rigidbody rb = PredictionRigidbody.Rigidbody;
        rb.SetState(rd.RigidbodyState);
        //Applies reconcile information from predictionrigidbody.
        PredictionRigidbody.Reconcile(rd.PredictionRigidbody);

        //reconcile rotation
        transform.rotation = rd.Rotation;
    }
}
