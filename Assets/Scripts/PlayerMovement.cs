using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float sensitivity;
    [SerializeField] private float speed;
    [SerializeField] private float jumpForce;
    [SerializeField] private Vector3 spawnPoint = new Vector3(50,50,50); //default respawn position roughly in the middle of a medium sized map for testting purposes

    private bool isGrounded;

    //this is triggered anytime the ground check touches anything 
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Ground") {
            isGrounded = true;
        }
    }

    // Rotates the camera up and down
    [ServerRpc]
    public void RotateServerRpc(float rotation)
    {
        float calculatedRotation = rotation*sensitivity; // calcuates how much to rotate by
        transform.Rotate(new Vector3(0,calculatedRotation,0));
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

    //respawns the player. is called at start and on player death
    [ServerRpc]
    public void RespawnServerRpc()
    {
        transform.position = spawnPoint;
    }
}
