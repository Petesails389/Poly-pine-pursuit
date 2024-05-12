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
    [SerializeField] private float sprintSpeed = 12;
    [SerializeField] private float moveSpeed = 7.5f;
    [SerializeField] private float crouchSpeed = 3;
    [SerializeField] private float jumpSpeed = 15;
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
        //networkCollision.OnEnter += NetworkCollisionEnter;
        //networkCollision.OnExit += NetworkCollisionExit;
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

    private void OnCollisionEnter(Collision other)
    {  
        //if it hit the floor then it's grounded
        if (other.collider.tag == "Ground") isGrounded = true;
        Debug.Log(other);
    }

    private void OnCollisionExit(Collision other)
    {
        //if it had been hitting the floor and isn't anymore then it's no longer grounded
        if (other.collider.tag == "Ground") isGrounded = false;
        Debug.Log(other);
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
        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);

        //decide on walk speed
        float speed = moveSpeed;
        if (Input.GetAxis("Crouch") != 0f) {
            speed = crouchSpeed;
        } else if (Input.GetAxis("Sprint") != 0f) {
            speed = sprintSpeed;
        }

        //move the player
        rb.velocity = transform.TransformDirection(new Vector3(horizontal * speed, rb.velocity.y, vertical * speed));

        //jump logic
        if (Input.GetAxis("Jump") != 0f && isGrounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, jumpSpeed, rb.velocity.z);
        }

        //Add gravity to make the object fall faster.
        rb.AddForce(Physics.gravity * 3f);
    }

}
