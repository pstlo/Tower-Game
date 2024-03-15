using UnityEngine;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance;

    [SerializeField] private GameObject pauseMenuUI;

    private void Awake()
    {
        Instance = this;
    }

    public void TogglePauseMenu(bool active)
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(active); // Activate or deactivate the pause menu UI
        }
    }

    
}
