using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraController : MonoBehaviour
{
    [SerializeField] private float sensitivity;
    [SerializeField] private float fov;
    [SerializeField] private float zoom; 

    private float xRot;

    void Start() {
        xRot = transform.rotation.x;
        ZoomOut();
    }

    // Rotates the camera up and down
    public void Rotate(float rotation)
    {
        float calculatedRotation = -1*rotation*sensitivity; // calcuates how much to rotate by
        xRot = Mathf.Clamp(xRot+calculatedRotation,-90f,90f); //calculates new roation
        Vector3 oldEuler = transform.rotation.eulerAngles; //gets the current rotation

        //aplies new rotation
        transform.rotation = Quaternion.Euler(xRot,oldEuler.y, oldEuler.z);
    }

    public void ZoomIn() {
        Camera.main.fieldOfView = fov/zoom;
    }
    
    public void ZoomOut() {
        Camera.main.fieldOfView = fov;
    }
}
