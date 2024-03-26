using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using Unity.Networking.Transport.Relay;
using System.Collections.Generic;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class GameLobby : MonoBehaviour
{
    public static GameLobby Instance { get; private set; }

    [SerializeField] private Transform respawnPoint;
    [SerializeField] private GameObject mainCamera;

    private const string KEY_RELAY_CODE = "RelayCode";
    private const string KEY_PLAYER_NAME = "PlayerName";

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
    }

    private void Update() 
    {
        HandleRefreshLobbyList();
        HandleHeartBeat();
        HandleLobbyPolling();
    }

    public async void Authenticate(string playerName) 
    {   
        this.playerName = playerName;
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(playerName);
        await UnityServices.InitializeAsync(initializationOptions);

        AuthenticationService.Instance.SignedIn += () => {
            Debug.Log("Signed in " + playerName + ", ID " + AuthenticationService.Instance.PlayerId);
            RefreshLobbyList();
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
        UIManager.Instance.ToggleAuthenticateUI(false);
        UIManager.Instance.ToggleNetworkUI(true);
    }

    private async Task<string> CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            UIManager.Instance.ToggleNetworkUI(false);
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
                UIManager.Instance.ToggleNetworkUI(false);
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
            UIManager.Instance.SetJoinCodeText(lobbyCode);
            UIManager.Instance.ToggleJoinCode(true);
            UIManager.Instance.ToggleStartUI(true);
            UIManager.Instance.ToggleStartButton(true);
        }
        catch (LobbyServiceException e) {Debug.Log(e);}
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
    
    public async void LeaveLobby() 
    {
        if (joinedLobby != null) 
        {
            try 
            {
                Debug.Log("Leaving lobby");
                if (IsLobbyHost()) {GameManager.Instance.EndGame();}
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                if (NetworkManager.Singleton != null) {NetworkManager.Singleton.Shutdown();}
            } 
            catch (LobbyServiceException e) {Debug.Log(e);}
        }

        UIManager.Instance.ToggleStartUI(false);
        UIManager.Instance.ToggleCountdownTimer(false);
        UIManager.Instance.ToggleJoinCode(false);
        UIManager.Instance.TogglePauseUI(false);
        UIManager.Instance.ToggleNetworkUI(true);
        mainCamera.SetActive(true);
        joinedLobby = null;
    }   


    public async void UpdatePlayerName(string playerName) 
    {   
        try {
            this.playerName = playerName;
            await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
            Debug.Log("Changed player name to " + playerName);
        }
        catch (AuthenticationException e) {Debug.Log(e);}

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
        }
    }

    private Player GetPlayer() {return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject> {{KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName)}});}

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
        if (joinedLobby != null && joinedLobby.Players != null) 
        {
            foreach (Player player in joinedLobby.Players) 
            {
                if (player.Id == AuthenticationService.Instance.PlayerId) {return true;}
            }
        }
        return false;
    }
    
    private bool IsLobbyHost() {return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;}
    
    public void RespawnAllPlayers()
    {
        float horizontalSpacing = 2.0f;
        Vector3 currentPosition = respawnPoint.position;
        Quaternion spawnRotation = respawnPoint.rotation;

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Value.PlayerObject != null)
            {
                PlayerController controller = client.Value.PlayerObject.GetComponent<PlayerController>();
                controller.SetSpawn(currentPosition, spawnRotation);
                controller.RespawnRpc();
                currentPosition += Vector3.right * horizontalSpacing;
                Debug.Log("Respawned player " + client.Value.ClientId);
            }
        }
    }

    public void UpdatePlayerNames()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            PlayerController controller = client.Value.PlayerObject.GetComponent<PlayerController>();
            controller.UpdateName();
        }
    }
    
}
