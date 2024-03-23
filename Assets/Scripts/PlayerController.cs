using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{    
    [SerializeField] public Transform spawnPoint;
    [SerializeField] private GameObject playerCameraPrefab;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private float moveSpeed = 10f; 
    [SerializeField] private float jumpForce = 5f; 
    
    
    private Rigidbody rigidBody; 
    private PlayerCamera playerCamera;
    private Animator animator;
    
    private string playerName;
    private float playerNameOffset = 1.5f;
    private bool isPaused = false;
    private bool isGrounded = true; 


    void Start()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;

        rigidBody = GetComponent<Rigidbody>();
        rigidBody.isKinematic = false;

        animator = GetComponentInChildren<Animator>();

        NetworkObject.DestroyWithScene = true;
        playerName = GameLobby.Instance.playerName;

        if (IsLocalPlayer)
        {
            gameObject.name = "Player: " + playerName;
            
            GameObject newCamera = Instantiate(playerCameraPrefab, transform.position, Quaternion.identity);
            PlayerCamera newPlayerCamera = newCamera.GetComponent<PlayerCamera>();
            newPlayerCamera.setPlayer(transform);
            playerCamera = newPlayerCamera;
            playerCamera.transform.SetParent(transform);

            playerNameText.text = playerName;
            playerNameText.transform.localPosition = Vector3.up * playerNameOffset;
            UpdateNameTextRpc(playerName);

            Respawn();
        }
    }


    void Update()
    {
        if (!IsOwner) {return;}

        if (Input.GetKeyDown(KeyCode.Escape)) // PAUSE
        {
            isPaused = !isPaused;
            SetPauseMenuActive(isPaused);
        }

        if (!isPaused && isGrounded && GameManager.Instance.CanMove() && Input.GetKeyDown(KeyCode.Space)) // JUMPING
        {
            rigidBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false; 
        }

        // NAMETAG
        playerNameText.transform.LookAt(playerCamera.transform);
        playerNameText.transform.Rotate(0, 180, 0);
    }


    void FixedUpdate()
    {
        if (!IsOwner) { return; }

        if (gameObject == null)
        {
            Destroy(gameObject);
            Destroy(playerCamera);
            playerCamera = null;
            return;
        }

        if (!isPaused && GameManager.Instance.CanMove())
        {
            // MOVEMENT INPUT
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");

            // CAMERA MOVEMENT
            Vector3 cameraForward = playerCamera.transform.forward;
            Vector3 cameraRight = playerCamera.transform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            // MOVEMENT VECTOR
            Vector3 movementDirection = (cameraForward * verticalInput + cameraRight * horizontalInput).normalized;
            Vector3 movement = movementDirection * moveSpeed * Time.deltaTime;

            
            if (movementDirection != Vector3.zero) // MOVING
            {
                rigidBody.MovePosition(rigidBody.position + movement);
                transform.rotation = Quaternion.LookRotation(movementDirection);
            }

            else // IDLE
            {

            }

            if (rigidBody.position.y < -5f) {Respawn();} // RESPAWN
        }
    }


    private void HandleDisconnect(ulong clientId) {if (clientId == OwnerClientId) {Leave();}}


    public void Respawn()
    {
        if (spawnPoint != null)
        {
            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }
    }


    [Rpc(SendTo.Everyone)]
    public void RespawnRpc() {Respawn();}


    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground")) {isGrounded = true;} // LANDING
    }


    [Rpc(SendTo.Everyone)]
    private void UpdateNameTextRpc(string name) {playerNameText.text = name; }


    public string GetName() {return playerName;}


    private void SetName(string name) {playerName = name;}


    public void UpdateName()
    {
        if (IsLocalPlayer)
        {
            string name = GameLobby.Instance.playerName;
            SetName(name);
            UpdateNameTextRpc(name);
        }
    }


    private void SetPauseMenuActive(bool active) {UIManager.Instance.TogglePauseUI(active);}

    public void Leave()
    {
        if (IsLocalPlayer)
        {
            GameLobby.Instance.LeaveLobby();
            Destroy(playerCamera);
            Destroy(gameObject);
        }
    }
}
