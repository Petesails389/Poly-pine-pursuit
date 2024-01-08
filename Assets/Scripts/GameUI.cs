using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class GameUI : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    [SerializeField] private Button quitButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionsButton;

    void Start() 
    {

        quitButton.onClick.AddListener(()=> { 
            GameObject.Find("GameManager").GetComponent<GameManager>().Quit();
        });

        resumeButton.onClick.AddListener(()=> {
            GameObject.Find("GameManager").GetComponent<GameManager>().ToggleLocalPause();
            //resumes the game if clicked
        });



    }

    public void SetPause(bool state) {
        pausePanel.SetActive(state);
    }
}
