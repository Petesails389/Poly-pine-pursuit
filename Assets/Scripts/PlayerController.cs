using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;

public struct MoveData : IReplicateData
{
    public bool Jump;
    public float XMovement;
    public float ZMovement;
    public float XRotation;
    public float YRotation;
    public MoveData (bool _jump, float _xMovement, float _zMovement, float _xRotation, float _yRotation) {
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

public struct ReconcileData : IReconcileData
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
    public Vector3 AngularVelocity;

    public ReconcileData(Vector3 _position, Quaternion _rotation, Vector3 _velocity, Vector3 _angularVelocity) {
        Position = _position;
        Rotation = _rotation;
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

public class PlayerController : NetworkBehaviour
{
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
        if (base.IsOwner) {
            //sorts out the camera
            mainCamera = Camera.main.gameObject;
            mainCamera.transform.position = new Vector3(transform.position.x, transform.position.y + 1, transform.position.z);
            mainCamera.transform.SetParent(transform.Find("Head"));

            //sorts out object references
            gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

            //hides various player commonents that shoudl only be visible to other players
            transform.Find("Body").GetComponent<Renderer>().enabled = false;
            transform.Find("Head").GetComponent<Renderer>().enabled = false;
            transform.Find("Head/Visor").GetComponent<Renderer>().enabled = false;

            //subscribes to time manager ontick and on post tick
            base.TimeManager.OnTick += TimeManager_OnTick;
            base.TimeManager.OnPostTick  += TimeManager_OnPostTick ;
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

            if (gameManager.IsPaused()) return; //return if the game is paused locally or globally

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
            ReconcileData rd = new ReconcileData(transform.position, transform.rotation, rb.velocity, rb.angularVelocity);
            Reconcile(rd);
        }
    }

    private MoveData BuildActions() {
        if (!base.IsOwner)
        return default;

        MoveData md = new MoveData(jump, xMovement, zMovement, xRotationChange, yRotationChange);
        //reset queued values
        jump = false;
        xMovement = 0;
        zMovement = 0;
        xRotationChange = 0;
        xRotationChange = 0;

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
    private float yRotation;

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
        //jump logic
        if (isGrounded && moveData.Jump) {
            GetComponent<Rigidbody>().AddForce(0,jumpForce,0,ForceMode.Impulse);
            isGrounded = false;
        }

        //move logic
        Vector3 X = transform.right*moveData.XMovement*speed; //x velocity based off user inputs
        Vector3 Y = transform.up * GetComponent<Rigidbody>().velocity.y; //y velocity remains the same as the rigid body thinks it should be
        Vector3 Z = transform.forward*moveData.ZMovement*speed; //z velocity based off user inputs
        GetComponent<Rigidbody>().velocity = X + Y + Z; //aply that velocity to the rigidbody
        //Debug.Log(X + Y + Z); //returns the correct velocity
        //Debug.Log(GetComponent<Rigidbody>().velocity); //does not return the correct velocity


        //rotation logic
        float calculatedRotationX = -1*moveData.XRotation*sensitivity; // calcuates how much to rotate by
        float calculatedRotationY = moveData.YRotation*sensitivity; // calcuates how much to rotate by

        xRotation = Mathf.Clamp(xRotation + calculatedRotationX,-90f,90f); //calculates new roation
        yRotation = yRotation + calculatedRotationY;
        
        Vector3 oldEuler = transform.Find("Head").rotation.eulerAngles; //gets the current rotation
        transform.Find("Head").rotation = Quaternion.Euler(xRotation,oldEuler.y,oldEuler.z);

        oldEuler = transform.rotation.eulerAngles; //gets the current rotation
        transform.rotation = Quaternion.Euler(oldEuler.x,yRotation,oldEuler.z);
    }

    [ReconcileV2]
    private void Reconcile(ReconcileData rd, Channel channel = Channel.Unreliable)
    {
        //Reset the client to the received position. It's okay to do this
        //even if there is no de-synchronization.
        transform.position = rd.Position;
        transform.rotation = rd.Rotation;
        transform.GetComponent<Rigidbody>().velocity = rd.Velocity;
        transform.GetComponent<Rigidbody>().angularVelocity = rd.AngularVelocity;
    }
}
