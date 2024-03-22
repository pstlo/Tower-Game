using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance {get; private set;}
    public NetworkVariable<bool> canMove = new NetworkVariable<bool>(true,NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(true,NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private bool isStarting = false; // during countdown 
    private bool isStarted = false; // after countdown 
    private float startCountdownTime = 5f;
    private float startTextDuration = 2f;
    private float countdownTimer;
    [SerializeField] private string startMessage = "Go!";

    private void Awake() {Instance = this;}

    void Update() 
    {
        if (isStarting || isStarted) {updateStartTimerRpc();}
        if (IsOwner) {canMove.Value = !(isStarting && gameStarted.Value);}
    }

    public void StartGame() 
    {
        //if (!IsOwner) return; // maybe idk
        GameObject[] players = GameLobby.Instance.GetPlayers();
        GameLobby.Instance.UpdatePlayerNames(players);
        GameLobby.Instance.RespawnAllPlayers(players); // this not workin all the time?
        
        
        UIManager.Instance.ToggleStartUI(true);
        UIManager.Instance.ToggleCountdownTimer(true);
        UIManager.Instance.ToggleStartButton(false);
        

        countdownTimer = startCountdownTime;
        gameStarted.Value = true;
        isStarting = true;
        Debug.Log("Game started");
    }

    public void EndGame() 
    {
        if (!IsOwner) {return;}
        gameStarted.Value = false;
        isStarting = false;
        isStarted = false;
    }

    public bool HasGameStarted() {return gameStarted.Value;}

    public bool CanMove() {return canMove.Value;}

    [Rpc(SendTo.Everyone)]
    private void updateStartTimerRpc() 
    {
        if (countdownTimer < -1 * startTextDuration) 
        {
            UIManager.Instance.ToggleCountdownTimer(false);
            isStarted = false;
        }  
        
        if (countdownTimer <= 0f)
        {
            UIManager.Instance.SetCountdownTimerText(startMessage);
            if (IsOwner)
            {
                gameStarted.Value = true;
                isStarting = false;
                isStarted = true;
            }
        }

        if (countdownTimer > 0f)
        {
            int secLeft = Mathf.FloorToInt(countdownTimer % 60f);
            string timerString = string.Format("{00}",secLeft);
            UIManager.Instance.SetCountdownTimerText(timerString);
        }
        
        if (countdownTimer >= -1 * startTextDuration) {countdownTimer -= Time.deltaTime;}
    }
}
