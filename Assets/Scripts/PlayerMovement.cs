using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float sensitivity;
    [SerializeField] private float speed;
    [SerializeField] private float jumpForce;

    private bool isGrounded;

    //this is triggered anytime the ground check touches anything 
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Ground") {
            isGrounded = true;
        }
    }

    // Rotates the camera up and down
    public void Rotate(float rotation)
    {
        float calculatedRotation = rotation*sensitivity; // calcuates how much to rotate by
        transform.Rotate(new Vector3(0,calculatedRotation,0));
    }

    //move function takes in values between 1 and -1
    public void Move(float x,float z)
    {
        Vector3 X = transform.right*x*speed;
        Vector3 Y = transform.up * GetComponent<Rigidbody>().velocity.y;
        Vector3 Z = transform.forward*z*speed;
        GetComponent<Rigidbody>().velocity = X + Y + Z;
    }

    //adds an impulse force up into the air to allow the player to jump
    public void Jump() {
        if (isGrounded) {
            GetComponent<Rigidbody>().AddForce(0,jumpForce,0,ForceMode.Impulse);
            isGrounded = false;
        }
    }
}
