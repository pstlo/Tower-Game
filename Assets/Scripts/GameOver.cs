using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using TMPro;

public class GameOver : MonoBehaviour
{
    [SerializeField] private GameObject gameOverUI;

    void Start() {gameOverUI.SetActive(false);}

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player")) {gameOverUI.SetActive(true);}
    }
}
