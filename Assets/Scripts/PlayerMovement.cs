using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Connection;
using FishNet.Object;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float sensitivity;
    [SerializeField] private float speed;
    [SerializeField] private float jumpForce;

    private bool isGrounded;
    private float xRotation;
    private float yRotation;

    //this is triggered anytime the ground check touches anything 
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Ground") {
            isGrounded = true;
        }
    }

    // Rotates the camera up and down
    [ServerRpc]
    public void RotateServerRpc(float y, float x)
    {
        float calculatedRotationX = -1*x*sensitivity; // calcuates how much to rotate by
        float calculatedRotationY = y*sensitivity; // calcuates how much to rotate by

        xRotation = Mathf.Clamp(xRotation + calculatedRotationX,-90f,90f); //calculates new roation
        yRotation = yRotation + calculatedRotationY;
        
        Vector3 oldEuler = transform.Find("Head").rotation.eulerAngles; //gets the current rotation
        transform.Find("Head").rotation = Quaternion.Euler(xRotation,oldEuler.y,oldEuler.z);

        oldEuler = transform.rotation.eulerAngles; //gets the current rotation
        transform.rotation = Quaternion.Euler(oldEuler.x,yRotation,oldEuler.z);
    }

    //move function takes in values between 1 and -1
    [ServerRpc]
    public void MoveServerRpc(float x,float z)
    {
        Vector3 X = transform.right*x*speed;
        Vector3 Y = transform.up * GetComponent<Rigidbody>().velocity.y;
        Vector3 Z = transform.forward*z*speed;
        GetComponent<Rigidbody>().velocity = X + Y + Z;
    }

    //adds an impulse force up into the air to allow the player to jump
    [ServerRpc]
    public void JumpServerRpc() {
        if (isGrounded) {
            GetComponent<Rigidbody>().AddForce(0,jumpForce,0,ForceMode.Impulse);
            isGrounded = false;
        }
    }
}
