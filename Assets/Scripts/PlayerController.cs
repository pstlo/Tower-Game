using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{    
    [SerializeField] public Transform tower;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject playerCameraPrefab;
    [SerializeField] private TMP_Text playerNameText;
    
    // CONSTANTS
    [SerializeField] private float moveSpeed = 10f; 
    [SerializeField] private float jumpForce = 5f; 
    
    // COMPONENTS
    private Rigidbody rb; 
    private PlayerCamera playerCamera;
    private Animator animator;
    
    private string playerName;
    private float playerNameOffset = 1.5f;

    // STATES
    private bool isPaused = false;
    private bool isGrounded = true; 

    Vector3 towerCenter;


    void Start()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;

        animator = GetComponentInChildren<Animator>();

        NetworkObject.DestroyWithScene = true;
        playerName = GameLobby.Instance.playerName;
        towerCenter = tower.position;

        if (IsLocalPlayer)
        {
            gameObject.name = "Player: " + playerName;
            
            GameObject newCamera = Instantiate(playerCameraPrefab, transform.position, Quaternion.identity);
            PlayerCamera newPlayerCamera = newCamera.GetComponent<PlayerCamera>();
            newPlayerCamera.SetPlayer(transform);
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
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false; 

            // JUMP ANIMATION
        }

        // NAMETAG
        playerNameText.transform.LookAt(playerCamera.transform);
        playerNameText.transform.Rotate(0, 180, 0);

        towerCenter.y = gameObject.transform.position.y;
    }


    void FixedUpdate()
    {
        if (!IsOwner) return;

        if (gameObject == null)
        {
            Destroy(gameObject);
            Destroy(playerCamera);
            playerCamera = null;
            return;
        }

        if (!isPaused && GameManager.Instance.CanMove())
        {
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");

            Vector3 circleCenterToPlayer = transform.position - towerCenter;
            Vector3 perpendicularToCircle = Vector3.Cross(circleCenterToPlayer, Vector3.up);
            Vector3 horizontalMove = horizontalInput * (Quaternion.AngleAxis(0, Vector3.up) * perpendicularToCircle.normalized);
            Vector3 verticalMove = -verticalInput * circleCenterToPlayer.normalized;
            Vector3 movement = horizontalMove + verticalMove;

            if (movement.magnitude > 0)
            {
                rb.MovePosition(rb.position + movement * moveSpeed * Time.deltaTime);
                transform.rotation = Quaternion.LookRotation(movement.normalized);
                animator.SetFloat("Speed", 0.5f);
            }
            else
            {
                animator.SetFloat("Speed", 0); // prob should not be instant
            }

            if (rb.position.y < -5f) Respawn();
        }
    }


    private void HandleDisconnect(ulong clientId) {if (clientId == OwnerClientId) {Leave();}}


    public void Respawn()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = spawnPoint.position;
        rb.rotation = spawnPoint.rotation;
    }


    [Rpc(SendTo.Everyone)]
    public void RespawnRpc() {Respawn();}


    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground")) // LANDING
        {
            isGrounded = true;
            rb.angularVelocity = Vector3.zero;
        } 
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

    public void SetSpawn(Vector3 pos, Quaternion rot)
    {;
        spawnPoint.position = pos;
        spawnPoint.rotation = rot;
    }
}
