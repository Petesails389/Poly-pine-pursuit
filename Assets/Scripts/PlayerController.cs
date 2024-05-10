using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Component.Prediction;
using FishNet.Transporting;
using System;

public class PlayerController : NetworkBehaviour
{
    [SerializeField]
    [Tooltip("X/Z being forward/backwards and straifing and Y being jump velocity.")] 
    private Vector3 moveSpeed = new Vector3(15,15,15);
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float sensitivity = 15f;

    private bool lastFrameEscape;
    private bool jump;
    private bool isGrounded = false;
    private Vector2 headRotation = new Vector2();

    //OBJECT/component REFERENCES
    private GameManager gameManager;
    private Rigidbody rb;
    private NetworkCollision networkCollision;

    private void Awake()
    {
        networkCollision = GetComponent<NetworkCollision>(); //find network collider
        // Subscribe to the desired collision event
        networkCollision.OnEnter += NetworkCollisionEnter;
        networkCollision.OnExit += NetworkCollisionExit;
    }

    public override void OnStartNetwork()
    {
        base.TimeManager.OnTick += TimeManager_OnTick;
    }

    public override void OnStopNetwork()
    {
        base.TimeManager.OnTick -= TimeManager_OnTick;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner) return; //don't do anything unless this is the owned player

        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>(); //find game manager
        rb = gameObject.GetComponent<Rigidbody>();

        //sorts out the camera
        GameObject mainCamera = Camera.main.gameObject;
        mainCamera.transform.position = new Vector3(transform.position.x, transform.position.y + 0.7f, transform.position.z);
        mainCamera.transform.SetParent(transform);
        mainCamera.transform.SetParent(transform.Find("Head"));
    }

    private void NetworkCollisionEnter(Collider other)
    {  
        //if it hit the floor then it's grounded
        if (other.tag == "Ground") isGrounded = true;
    }

    private void NetworkCollisionExit(Collider other)
    {
        //if it had been hitting the floor and isn't anymore then it's no longer grounded
        if (other.tag == "Ground") isGrounded = false;
    }

    private void Update()
    {
        if (!base.IsOwner || gameManager.currentGameState == GameManager.GameState.Paused) return;
        
        //escape logic
        if ((Input.GetAxis("Escape") != 0f) && !lastFrameEscape) {
            gameManager.ToggleLocalPause();
        }
        lastFrameEscape = (Input.GetAxis("Escape") != 0f);

        float yaw = Input.GetAxis("Mouse X"); //rotation of the camera
        float pitch = Input.GetAxis("Mouse Y"); //rotation of the camera

        //calculate rotations
        float calculatedRotationY = (float) (yaw*sensitivity);
        float calculatedRotationX = (float) (pitch*sensitivity);
        calculatedRotationX = Mathf.Clamp(headRotation.x - calculatedRotationX,-90f,90f) - headRotation.x; //clamp the pitch
        headRotation += new Vector2(calculatedRotationX, calculatedRotationY);
        
        //apply the rotations
        transform.rotation = Quaternion.Euler(0, headRotation.y, 0);
        Vector3 oldEuler = transform.Find("Head").rotation.eulerAngles;
        transform.Find("Head").rotation = Quaternion.Euler(headRotation.x, oldEuler.y, oldEuler.z);
    }

    private void TimeManager_OnTick() 
    {
        if (!base.IsOwner || gameManager.currentGameState == GameManager.GameState.Paused) return;
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

         //get velocity
        Vector3 velocity = rb.velocity;
        //calculate speed
        float speed = (float) Math.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z);

        //move the player in x and z
        float x = horizontal * (moveSpeed.x - speed);
        float z = vertical * (moveSpeed.z - speed);
        Vector3 forces = new Vector3(x, 0, z) * acceleration;
        rb.AddRelativeForce(forces);

        //jump logic
        if (Input.GetAxis("Jump") != 0f && isGrounded)
        {
            rb.velocity = new Vector3(velocity.x, moveSpeed.y, velocity.z);
        }

        //Add gravity to make the object fall faster.
        //rb.AddForce(Physics.gravity * 4f);
    }

}
