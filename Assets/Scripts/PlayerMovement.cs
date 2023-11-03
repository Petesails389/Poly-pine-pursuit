using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float sensitivity;

    // Rotates the camera up and down
    public void Rotate(float rotation)
    {
        float calculatedRotation = rotation*sensitivity; // calcuates how much to rotate by
        transform.Rotate(new Vector3(0,calculatedRotation,0));
    }
}
