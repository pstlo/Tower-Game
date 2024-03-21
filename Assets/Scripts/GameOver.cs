using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using TMPro;

public class GameOver : MonoBehaviour
{
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private TMP_Text winnerText;
    bool gameOver = false;

    void Start() 
    {
        gameOverUI.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (gameOver) {return;}
        if (collision.gameObject.CompareTag("Player")) 
        {
            gameOver = true;
            gameOverUI.SetActive(true);
            PlayerController winner = collision.gameObject.GetComponent<PlayerController>();
            winnerText.text = winner.GetName() + " wins";
        }
    }
}
