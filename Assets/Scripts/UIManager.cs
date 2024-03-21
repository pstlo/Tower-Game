using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private GameObject authenticateUI;
    [SerializeField] private Button authenticateButton;
    [SerializeField] private TMP_InputField enterUsernameField;

    [SerializeField] private GameObject networkUI;
    [SerializeField] private Button hostButton;
    [SerializeField] private TMP_InputField lobbyNameField;
    [SerializeField] private TMP_InputField maxPlayersField;
    [SerializeField] private Toggle isPrivateField; 
    [SerializeField] private Button clientButton;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private Button quickJoinButton;

    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private Button changePlayerNameButton;
    [SerializeField] private TMP_InputField changePlayerNameField;
    [SerializeField] private Button leaveButton;

    [SerializeField] private GameObject startGameUI;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TMP_Text joinCodeText;

    

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
        startGameButton.onClick.AddListener(() => GameLobby.Instance.StartGame());
        changePlayerNameButton.onClick.AddListener(() => GameLobby.Instance.UpdatePlayerName(changePlayerNameField.text));
 
        ToggleJoinCode(false);
        ToggleNetworkUI(false);
        ToggleAuthenticateUI(true);
        ToggleStartUI(false);
        TogglePauseUI(false);

       
    }

    public void SetJoinCode(string joinCode) {joinCodeText.text = joinCode;}
    public void ToggleJoinCode(bool active) {joinCodeText.gameObject.SetActive(active);}
    public void ToggleStartUI(bool active) {startGameUI.SetActive(active);}
    public void ToggleAuthenticateUI(bool active) {authenticateUI.SetActive(active);}
    public void ToggleNetworkUI(bool active) {networkUI.SetActive(active);}
    public void TogglePauseUI(bool active) {pauseMenuUI.SetActive(active);}
}
