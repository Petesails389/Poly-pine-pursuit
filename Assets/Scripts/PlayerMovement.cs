using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet;

public class PlayerMovement : MonoBehaviour
{
    //movement attributes
    [SerializeField] private float sensitivity = 8;
    [SerializeField] private float speed = 12;
    [SerializeField] private float jumpForce = 3;

    //reference to game objects
    private GameManager gameManager;
    private CharacterController characterController;
    

    //internal tracking
    private float verticalVelocity;
    private float xRotation;

    // Start is called before the first frame update
    private void Start()
    {
        //sorts out object references
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        characterController = GetComponent<CharacterController>();
    }

    //movement function
    public void Move(bool jump, float xMovement, float zMovement) {
        float delta = (float)InstanceFinder.TimeManager.TickDelta;// time since last tick

        //jump logic
        if (characterController.isGrounded) {
            if (jump)
                verticalVelocity = jumpForce; //if jumping from the ground jump
            else
                verticalVelocity = 0; //if not jumping and on the ground don't fall
        } else {
            verticalVelocity += (Physics.gravity.y * delta); //if not grounded then fall
            if (verticalVelocity < -20f)
                verticalVelocity = -20f; // sets a terminal velocity
        }
        
        //move logic
        Vector3 X = transform.right*xMovement*speed; //x velocity based off user inputs
        Vector3 Y = transform.up * verticalVelocity*5; //y based off internally tracked y velocity
        Vector3 Z = transform.forward*zMovement*speed; //z velocity based off user inputs
        characterController.Move((X + Y + Z) * delta); //move the player

        //anticheat stuff in here
        if (Math.Abs(transform.position.x) > gameManager.GetSize() + 2f
        || Math.Abs(transform.position.z) > gameManager.GetSize() + 2f
        || transform.position.y < 0 || transform.position.y > 150)
        {
            //prevents out of bounds and flying
            transform.position = new Vector3(0, 50, 0);
        }
    }

    public void Rotate(float xRot, float yRot) {
        //rotation logic
        float calculatedRotationX = (float) (-1*xRot*sensitivity); // calcuates how much to rotate by 
        float calculatedRotationY = (float) (yRot*sensitivity); // calcuates how much to rotate by

        Vector3 oldEuler = transform.Find("Head").rotation.eulerAngles; //gets the current rotation
        xRotation = Mathf.Clamp(xRotation + calculatedRotationX,-90f,90f); //calculates new x rotation
        transform.Find("Head").rotation = Quaternion.Euler(xRotation, oldEuler.y, oldEuler.z); //aplies the new x rotation

        oldEuler = transform.rotation.eulerAngles; //gets the current rotation
        float yRotation = oldEuler.y + calculatedRotationY; //calculates the new y rotation
        transform.rotation = Quaternion.Euler(oldEuler.x,yRotation,oldEuler.z); //apllies the new y rotation
    }
}
