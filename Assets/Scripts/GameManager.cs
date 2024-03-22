using Unity.Collections;
using Unity.Netcode;
using UnityEngine;


public class GameManager : NetworkBehaviour
{
    public static GameManager Instance {get; private set;}

    public NetworkVariable<bool> canMove = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> showCountdown = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> countdownTimer = new NetworkVariable<float>(5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString128Bytes> countdownTimerText = new NetworkVariable<FixedString128Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private bool isStarting = false; // during countdown 
    private bool isStarted = false; // after countdown 
    private float startTextDuration = 2f;

    [SerializeField] private string startMessage = "Go!";


    private void Awake() { Instance = this; }

    void Update()
    {
        if (IsOwner) 
        { 
            canMove.Value = !(isStarting && gameStarted.Value);
            if (isStarting || isStarted) {updateStartTimer();}
        }

        UIManager.Instance.ToggleCountdownTimer(showCountdown.Value);
        if (showCountdown.Value) {UIManager.Instance.SetCountdownTimerText(countdownTimerText.Value.ToString());}
    }

    public void StartGame()
    {
        if (IsOwner)
        {
            GameObject[] players = GameLobby.Instance.GetPlayers();
            GameLobby.Instance.UpdatePlayerNames(players);
            GameLobby.Instance.RespawnAllPlayers(players); // this not workin 0.00005% the time

            countdownTimer.Value = 5f;
            gameStarted.Value = true;
            isStarting = true;
            Debug.Log("Game started");

            UIManager.Instance.ToggleStartUI(true);
            showCountdown.Value = true;
            UIManager.Instance.ToggleStartButton(false);
        }
    }

    public void EndGame()
    {
        if (!IsOwner) { return; }
        gameStarted.Value = false;
        isStarting = false;
        isStarted = false;
    }

    public bool HasGameStarted() { return gameStarted.Value; }

    public bool CanMove() { return canMove.Value; }


    private void updateStartTimer()
    {
        if (!IsOwner) {return;}
        if (countdownTimer.Value < -1 * startTextDuration)
        {
            showCountdown.Value = false;
            isStarted = false;
        }

        if (countdownTimer.Value <= 0f)
        {
            countdownTimerText.Value = startMessage;
            gameStarted.Value = true;
            isStarting = false;
            isStarted = true;
        }

        if (countdownTimer.Value > 0f) {countdownTimerText.Value = string.Format("{00}", Mathf.FloorToInt(countdownTimer.Value % 60f));}

        if (countdownTimer.Value >= -1 * startTextDuration) {countdownTimer.Value -= Time.deltaTime; }


    }

}
