using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using TMPro;
using Unity.Networking.Transport.Relay;
using System.Collections.Generic;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class GameLobby : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private GameObject mainCamera;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private GameObject networkUI;
    [SerializeField] private GameObject startGameUI;
    [SerializeField] private Button startGameButton;
    [SerializeField] private GameObject authenticateUI;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button quickJoinButton;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private Button authenticateButton;
    [SerializeField] private TMP_InputField enterUsernameField;
    [SerializeField] private Button changePlayerNameButton;
    [SerializeField] private TMP_InputField playerNameField;
    [SerializeField] private TMP_InputField lobbyNameField;
    [SerializeField] private TMP_InputField maxPlayersField;
    [SerializeField] private Toggle isPrivateField;
    [SerializeField] private TMP_Text joinCodeText;


    public static GameLobby Instance { get; private set; }
    private const string KEY_RELAY_CODE = "RelayCode";
    public const string KEY_PLAYER_NAME = "PlayerName";
    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float refreshLobbyListTimer = 5f;
    private float heartBeatTimer;
    private float updateTimer;
    public string playerName;


    private async void Start()
    {
        Instance = this;
        await UnityServices.InitializeAsync();
        authenticateButton.onClick.AddListener(() => Authenticate(enterUsernameField.text == "" ? "DefaultUserName" : enterUsernameField.text));
        hostButton.onClick.AddListener(() => CreateLobby(
            lobbyNameField.text == "" ? "DefaultLobbyName" : lobbyNameField.text, 
            maxPlayersField.text == "" ? 4 : int.Parse(maxPlayersField.text), 
            isPrivateField.isOn)
        );
        clientButton.onClick.AddListener(() => JoinLobbyByCode(joinCodeInputField.text));
        quickJoinButton.onClick.AddListener(QuickJoinLobby);
        leaveButton.onClick.AddListener(LeaveLobby);
        startGameButton.onClick.AddListener(StartGame);
        changePlayerNameButton.onClick.AddListener(() => UpdatePlayerName(playerNameField.text));
        joinCodeText.gameObject.SetActive(false);
        networkUI.gameObject.SetActive(false);
        authenticateUI.SetActive(true);
        startGameUI.SetActive(false);
        if (PauseMenuManager.Instance != null) {PauseMenuManager.Instance.TogglePauseMenu(false);}
    }

    private void Update() 
    {
        HandleRefreshLobbyList();
        HandleHeartBeat();
        HandleLobbyPolling();
    }

    public async void Authenticate(string playerName) 
    {
        /*EVENTUALLY
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(playerName);
        await UnityServices.InitializeAsync(initializationOptions);*/

        this.playerName = playerName;
        AuthenticationService.Instance.SignedIn += () => {
            Debug.Log("Signed in " + playerName + ", ID " + AuthenticationService.Instance.PlayerId);
            RefreshLobbyList();
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
        authenticateUI.SetActive(false);
        networkUI.SetActive(true);
    }

    private async Task<string> CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            //Debug.Log("Relay code: " + relayCode);
            networkUI.SetActive(false);
            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartHost();
            return relayCode;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return null;
        }
    }

    private async void JoinRelay(string relayCode)
    {
        try
        {
            Debug.Log("Joining relay" + relayCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayCode);
            
            if (joinAllocation != null)
            {
                RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
                NetworkManager.Singleton.StartClient();
                networkUI.SetActive(false);
            }
            else {Debug.LogError("Invalid relay: " + relayCode);}
        }
        catch (RelayServiceException e) {Debug.LogError("Error joining relay: " + e.Message);}
    }

    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate)
    {
        try
        {
            string relayCode = await CreateRelay();
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {{ KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayCode)}}
            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);
            hostLobby = lobby;
            joinedLobby = hostLobby;            
            string lobbyCode = lobby.LobbyCode;
            Debug.Log("Created " + lobbyName + " for " + maxPlayers + " players. " + lobby.Id + " " + lobbyCode);
            DisplayJoinCode(lobbyCode);
            startGameUI.SetActive(true);
        }
        catch (LobbyServiceException e) { Debug.Log(e); }
    }
    
     public async void JoinLobbyByCode(string lobbyCode) 
     {
        Player player = GetPlayer();
        Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, new JoinLobbyByCodeOptions {Player = player});
        joinedLobby = lobby;
        JoinRelay(joinedLobby.Data[KEY_RELAY_CODE].Value);
    }

    public async void QuickJoinLobby() 
    {
        try 
        {
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();
            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            joinedLobby = lobby;
            JoinRelay(joinedLobby.Data[KEY_RELAY_CODE].Value);
        } 
        catch (LobbyServiceException e) {Debug.Log(e);}
    }
    
    public async void LeaveLobby() {
        if (joinedLobby != null) {
            try {
                Debug.Log("Leaving lobby");
                if (IsLobbyHost()) {startGameUI.SetActive(false);}
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                NetworkManager.Singleton.Shutdown();
            } 
            catch (LobbyServiceException e) {Debug.Log(e);}
        }
        else {Debug.Log("Lobby is null");}

        joinCodeText.gameObject.SetActive(false);
        networkUI.gameObject.SetActive(true);
        PauseMenuManager.Instance.TogglePauseMenu(false); 
        mainCamera.SetActive(true);
        joinedLobby = null;
    }   

    private void DisplayJoinCode(string joinCode)
    {
        joinCodeText.text = joinCode;
        joinCodeText.gameObject.SetActive(true);
    }

    public async void UpdatePlayerName(string playerName) 
    {   
        try {
            this.playerName = playerName;
            await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
            Debug.Log("Changed player name to " + playerName);}
        catch (AuthenticationException e) {Debug.Log(e);}

        /* EVENTUALLY
        if (joinedLobby != null) {
            try {
                UpdatePlayerOptions options = new UpdatePlayerOptions();
                options.Data = new Dictionary<string, PlayerDataObject>() {
                    {
                        KEY_PLAYER_NAME, new PlayerDataObject(
                            visibility: PlayerDataObject.VisibilityOptions.Public,
                            value: playerName)
                    }
                };
                string playerId = AuthenticationService.Instance.PlayerId;
                Lobby lobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, playerId, options);
                joinedLobby = lobby;
                
            } 
            catch (LobbyServiceException e) {Debug.Log(e);}
        }*/
    }

    private Player GetPlayer()
    {
        return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject> {{KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName)}});
    }

    private async void HandleHeartBeat()
    {
        if (IsLobbyHost())
        {
            heartBeatTimer -= Time.deltaTime;
            if (heartBeatTimer < 0f)
            {
                float heartBeatTimerMax = 15f;
                heartBeatTimer = heartBeatTimerMax;
                await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    private async void HandleLobbyPolling()
    {
        if (joinedLobby != null)
        {
            updateTimer -= Time.deltaTime;
            if (updateTimer < 0f)
            {
                float updateTimerMax = 2f;
                updateTimer = updateTimerMax; 
                joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);

                if (!IsPlayerInLobby())
                {
                    Debug.Log("Left lobby");
                    joinedLobby = null;
                }
            }
        }
    }

    private void HandleRefreshLobbyList() {
        if (UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn) {
            refreshLobbyListTimer -= Time.deltaTime;
            if (refreshLobbyListTimer < 0f) {
                float refreshLobbyListTimerMax = 5f;
                refreshLobbyListTimer = refreshLobbyListTimerMax;
                RefreshLobbyList();
            }
        }
    }

    public async void RefreshLobbyList() {
        try {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;
            options.Filters = new List<QueryFilter> {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0")
            };
            options.Order = new List<QueryOrder> {
                new QueryOrder(
                    asc: false,
                    field: QueryOrder.FieldOptions.Created)
            };
            QueryResponse lobbyListQueryResponse = await Lobbies.Instance.QueryLobbiesAsync();
        } 
        catch (LobbyServiceException e) {Debug.Log(e);}
    }


    private bool IsPlayerInLobby() {
        if (joinedLobby != null && joinedLobby.Players != null) {
            foreach (Player player in joinedLobby.Players) {
                if (player.Id == AuthenticationService.Instance.PlayerId) {return true;}
            }
        }
        return false;
    }
    
    private bool IsLobbyHost() {return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;}
    
    public void RespawnAllPlayers()
    {
        List<GameObject> activePlayerInstances = GetActivePlayerInstances();
        float horizontalSpacing = 2.0f;
        Vector3 currentPosition = respawnPoint.position;
        Quaternion spawnRotation = respawnPoint.rotation;

        foreach (GameObject playerInstance in activePlayerInstances)
        {
            PlayerController playerController = playerInstance.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.spawnPoint.position = currentPosition;
                playerController.spawnPoint.rotation = spawnRotation;
                currentPosition += Vector3.right * horizontalSpacing;
                if (playerController.GetComponent<NetworkObject>().IsOwnedByServer) {playerController.Respawn();}
                else {playerController.RespawnRpc();}
            }
        }
    }

    private List<GameObject> GetActivePlayerInstances()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        List<GameObject> activePlayerInstances = new List<GameObject>();

        foreach (GameObject player in players)
        {
            if (player.activeSelf) {activePlayerInstances.Add(player);}
        }
        return activePlayerInstances;
    }


    private void StartGame()
    {
        RespawnAllPlayers();
        Debug.Log("Game started");
        startGameUI.SetActive(false);
    }
}
