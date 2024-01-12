using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using FishNet.Managing.Scened;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button clientButton;
    [SerializeField] private Button hostButton;

    [SerializeField] private ConnectionManager connectionManager;

    // Start is called before the first frame update
    void Start()
    {
        clientButton.onClick.AddListener(()=> {
            connectionManager.StartNetwork(false);
        });
        hostButton.onClick.AddListener(()=> {
            connectionManager.StartNetwork(true);
        });
    }

}
