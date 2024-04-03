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
    [SerializeField] private GameObject tower;
    [SerializeField] private GameObject boulderPrefab;
    [SerializeField] private GameObject stepPrefab;


    // BOULDER SPAWNING
    
    private Vector3 cylinderCenter;
    private float boulderSpawnRadius;
    private float outerBoulderSpawnRadius;
    private float boulderSpawnHeight;
    private float minSpawnDelay = 1f;
    private float maxSpawnDelay = 5f;


    void Start() 
    {
        Instance = this;

        // Boulder spawn area
        cylinderCenter = tower.transform.position;
        cylinderCenter.y = 0;
        boulderSpawnHeight = tower.transform.localScale.y;
        boulderSpawnRadius = tower.transform.localScale.x;
        outerBoulderSpawnRadius = boulderSpawnRadius + stepPrefab.transform.localScale.x * 0.8f;
    }

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
            GameLobby.Instance.RespawnAllPlayers();
            GameLobby.Instance.UpdatePlayerNames();

            countdownTimer.Value = 5f;
            gameStarted.Value = true;
            isStarting = true;
            Debug.Log("Game started");

            UIManager.Instance.ToggleStartUI(true);
            showCountdown.Value = true;
            UIManager.Instance.ToggleStartButton(false);
            InvokeRepeating(nameof(RandomSpawnBoulder), Random.Range(minSpawnDelay, maxSpawnDelay), Random.Range(minSpawnDelay, maxSpawnDelay));
        }
    }

    public void EndGame()
    {
        if (!IsOwner) { return; }
        gameStarted.Value = false;
        isStarting = false;
        isStarted = false;

        CancelInvoke(nameof(RandomSpawnBoulder));
        UIManager.Instance.ToggleGameOverUI(false);
        
        // DESPAWN ALL BOULDERS
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


    public void SpawnBoulder(Vector3 position)
    {
        if (IsOwner)
        {
            GameObject boulder = Instantiate(boulderPrefab, position, Quaternion.identity);
            NetworkObject networkObject = boulder.GetComponent<NetworkObject>();
            if (networkObject != null) {networkObject.Spawn();}
        }
    }

    public void RandomSpawnBoulder()
{
    if (IsOwner)
    {
        Vector2 randomPointOnBase = Random.insideUnitCircle * (boulderSpawnRadius + outerBoulderSpawnRadius); 
        Vector3 spawnPosition = new Vector3(randomPointOnBase.x, 0f, randomPointOnBase.y) + cylinderCenter;
        Vector3 towerCenterToSpawn = spawnPosition - cylinderCenter;
        towerCenterToSpawn.y = 0f;
        if (towerCenterToSpawn.magnitude <= tower.transform.localScale.x / 2f)
        {
            towerCenterToSpawn = towerCenterToSpawn.normalized * (tower.transform.localScale.x / 2f + 0.1f);
            spawnPosition = cylinderCenter + towerCenterToSpawn;
        }
        float randomHeight = Random.Range(0f, boulderSpawnHeight);
        spawnPosition.y = randomHeight;
        SpawnBoulder(spawnPosition);
    }
}
}
