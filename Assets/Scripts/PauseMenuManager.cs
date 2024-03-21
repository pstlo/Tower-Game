using UnityEngine;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance;

    [SerializeField] private GameObject pauseMenuUI;

    private void Awake() {Instance = this;}

    public void TogglePauseMenu(bool active) {pauseMenuUI.SetActive(active);}

    
}
