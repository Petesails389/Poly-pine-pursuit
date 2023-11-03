using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraController : MonoBehaviour
{
    [SerializeField] private float sensitivity;

    private float yRot;

    void Start() {
        yRot = transform.rotation.x;
    }

    // Rotates the camera up and down
    public void Rotate(float rotation)
    {
        float calculatedRotation = -1*rotation*sensitivity; // calcuates how much to rotate by
        yRot = Mathf.Clamp(yRot+calculatedRotation,-90f,90f); //calculates new roation
        Vector3 oldEuler = transform.rotation.eulerAngles; //gets the current rotation

        //aplies new rotation
        //Vector3 newEuler = new Vector3(yrot,oldEuler.y, oldEuler.z);
        //Quaternion newRotation = new Quaternion();
        //newRotation.eulerAngles = newEuler;
        transform.rotation = Quaternion.Euler(yRot,oldEuler.y, oldEuler.z); //aplies new rotation
        //transform.Rotate(new Vector3(-1*rotation*sensitivity,0,0));
    }
}
