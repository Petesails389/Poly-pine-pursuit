using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Networking;

public class NetworkDisable : NetworkBehaviour
{

    //Class to disable movement and camera components for network object that don't belong to the current player

    [SerializeField] private GameObject cam;

    // Start is called before the first frame update
    void Start()
    {
        if (IsOwner) return;//doesn't do anything if the player is owned

        cam.SetActive(false);
        cam.gameObject.tag = "Untagged";
        GetComponent<PlayerController>().enabled = false;
    }
}
