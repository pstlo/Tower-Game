using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance {get; private set;}

    // Auth UI
    [SerializeField] private GameObject authenticateUI;
    [SerializeField] private Button authenticateButton;
    [SerializeField] private TMP_InputField enterUsernameField;

    // Network UI
    [SerializeField] private GameObject networkUI;
    [SerializeField] private Button hostButton;
    [SerializeField] private TMP_InputField lobbyNameField;
    [SerializeField] private TMP_InputField maxPlayersField;
    [SerializeField] private Toggle isPrivateField; 
    [SerializeField] private Button clientButton;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private Button quickJoinButton;

    // Pause UI
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private Button changePlayerNameButton;
    [SerializeField] private TMP_InputField changePlayerNameField;
    [SerializeField] private Button leaveButton;

    // Start UI
    [SerializeField] private GameObject startGameUI;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TMP_Text startTimerText;
    [SerializeField] private TMP_Text joinCodeText;

    // Game over UI
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private TMP_Text winnerText;

    // In Game UI
    [SerializeField] private TMP_Text towerCameraIndicator;

    
    void Start()
    {
        Instance = this;

        authenticateButton.onClick.AddListener(() => GameLobby.Instance.Authenticate(enterUsernameField.text == "" ? "DefaultUserName" : enterUsernameField.text));
        hostButton.onClick.AddListener(() => GameLobby.Instance.CreateLobby(
            lobbyNameField.text == "" ? "DefaultLobbyName" : lobbyNameField.text, 
            maxPlayersField.text == "" ? 4 : int.Parse(maxPlayersField.text), 
            isPrivateField.isOn));

        clientButton.onClick.AddListener(() => GameLobby.Instance.JoinLobbyByCode(joinCodeInputField.text));
        quickJoinButton.onClick.AddListener(() => GameLobby.Instance.QuickJoinLobby());
        leaveButton.onClick.AddListener(() => GameLobby.Instance.LeaveLobby());
        startGameButton.onClick.AddListener(() => GameManager.Instance.StartGame());
        changePlayerNameButton.onClick.AddListener(() => GameLobby.Instance.UpdatePlayerName(changePlayerNameField.text));

        ToggleAuthenticateUI(true);
        ToggleGameOverUI(false);
        ToggleJoinCode(false);
        ToggleNetworkUI(false);
        ToggleStartUI(false);
        ToggleCountdownTimer(false);
        TogglePauseUI(false);       
    }


    public void SetJoinCodeText(string joinCode) {joinCodeText.text = joinCode;}
    public void SetWinnerText(string winner) {winnerText.text = winner;}
    public void SetCountdownTimerText(string timer) {startTimerText.text = timer;}

    public void ToggleAuthenticateUI(bool active) {authenticateUI.SetActive(active);}
    public void ToggleJoinCode(bool active) {joinCodeText.gameObject.SetActive(active);}
    public void ToggleStartUI(bool active) {startGameUI.SetActive(active);}
    public void ToggleStartButton(bool active) {startGameButton.gameObject.SetActive(active);}
    public void ToggleCountdownTimer(bool active) {startTimerText.gameObject.SetActive(active);}
    public void ToggleNetworkUI(bool active) {networkUI.SetActive(active);}
    public void TogglePauseUI(bool active) {pauseMenuUI.SetActive(active);}
    public void ToggleGameOverUI(bool active) {gameOverUI.SetActive(active);}
    public void ToggleTowerCameraIndicator(bool active) {towerCameraIndicator.gameObject.SetActive(active);}
}
