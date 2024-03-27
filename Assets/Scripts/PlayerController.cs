using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{    
    [SerializeField] private Transform tower;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject playerCameraPrefab;
    [SerializeField] private TMP_Text playerNameText; 
    
    // CONSTANTS
    [SerializeField] private float moveSpeed = 10f; 
    [SerializeField] private float jumpForce = 5f; 
    [SerializeField] private float punchForce = 5f; 
    [SerializeField] private float punchRange = 2f; 
    [SerializeField] private float punchCooldown = 3f;
    [SerializeField] private float punchMoveSpeedModifier = 0.4f;


    
    // COMPONENTS
    private Rigidbody rb; 
    private PlayerCamera playerCamera;
    private Animator animator;
    
    private string playerName; // USE NETWORK VAR public NetworkVariable<FixedString128Bytes> playerName = new NetworkVariable<FixedString128Bytes>("DefaultUserName", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private float playerNameOffset = 1.5f;

    Vector3 towerCenter;

    // STATES
    private bool isPaused = false;
    private bool isGrounded = true;
    private bool punching;
    private float lastPunchTime; 
    private float speed;


    void Start()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;

        animator = GetComponentInChildren<Animator>();

        NetworkObject.DestroyWithScene = true;
        playerName = GameLobby.Instance.playerName;
        towerCenter = tower.position;
        speed = moveSpeed;

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

        if (!isPaused && GameManager.Instance.CanMove()) 
        {
            // JUMPING
            if (isGrounded && Input.GetKeyDown(KeyCode.Space)) 
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                isGrounded = false;

                // JUMP ANIMATION
                animator.SetTrigger("Jump");
            }

            // PUNCHING
            if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                if (isGrounded && Time.time - lastPunchTime >= punchCooldown)
                {
                    lastPunchTime = Time.time;
                    punching = true;
                    animator.SetTrigger("Punch");

                    if (IsServer) {Punch();}
                    else {PunchRpc();}
                }
            }

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

        if (punching && Time.time - lastPunchTime >= punchCooldown) {punching = false;}

        if (punching) {speed = moveSpeed * punchMoveSpeedModifier;}
        else {speed = moveSpeed;}

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
                rb.MovePosition(rb.position + movement * speed * Time.deltaTime);
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
    {
        spawnPoint.position = pos;
        spawnPoint.rotation = rot;
    }


    private void Punch()
    {
        foreach (Collider hitCollider in Physics.OverlapSphere(transform.position, punchRange))
        {
            if (hitCollider.gameObject != gameObject && hitCollider.CompareTag("Player"))
            {
                PlayerController playerController = hitCollider.gameObject.GetComponent<PlayerController>();
                Debug.Log(GetName() + " threw a punch");
                playerController.Punched(punchForce, transform.position);
            }
        }
    }


    [Rpc(SendTo.Everyone)]
    private void PunchRpc() {Punch();}


    public void Punched(float punchForce, Vector3 punchOrigin)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        Vector3 direction = (transform.position - punchOrigin).normalized;
        rb.AddForce(direction * punchForce, ForceMode.Impulse);
        Debug.Log(GetName() + " received a punch");
    }
}
