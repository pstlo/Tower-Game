using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class QuitGame : MonoBehaviour
{
    [SerializeField] private Button quitButton;

    void Start() {quitButton.onClick.AddListener(Quit);}

    public void Quit()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
