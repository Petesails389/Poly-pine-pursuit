using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private TMP_Text pauseText;

    [SerializeField] private Button quitButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionsButton;

    void Start() 
    {

        quitButton.onClick.AddListener(()=> { 
            GameObject.Find("GameManager").GetComponent<GameManager>().ClientStop();
        });

        resumeButton.onClick.AddListener(()=> {
            GameObject.Find("GameManager").GetComponent<GameManager>().ToggleLocalPause();
            //resumes the game if clicked
        });



    }

    public void SetPause(bool paused, bool resume = true, string msg = "Paused") {
        pausePanel.SetActive(paused);
        resumeButton.interactable = resume;
        pauseText.text = msg;
    }
}
