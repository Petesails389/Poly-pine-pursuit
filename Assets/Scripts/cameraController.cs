using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraController : MonoBehaviour
{
    [SerializeField] private float fov;
    [SerializeField] private float zoom; 

    private float xRot;

    void Start() {
        xRot = transform.rotation.x;
        ZoomOut();
    }

    public void ZoomIn() {
        Camera.main.fieldOfView = fov/zoom;
    }
    
    public void ZoomOut() {
        Camera.main.fieldOfView = fov;
    }
}
