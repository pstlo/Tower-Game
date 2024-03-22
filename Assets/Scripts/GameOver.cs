using UnityEngine;

public class GameOver : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (!GameManager.Instance.HasGameStarted()) {return;}
        if (collision.gameObject.CompareTag("Player")) 
        {
            UIManager.Instance.ToggleGameOverUI(true);
            PlayerController winner = collision.gameObject.GetComponent<PlayerController>();
            UIManager.Instance.SetWinnerText(winner.GetName() + " wins!");
        }
    }
}
