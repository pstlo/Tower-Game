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

    [SerializeField] private Collider rightFistCollider; 
    
    // MOVEMENT
    [SerializeField] private float moveSpeed = 10f; 
    [SerializeField] private float jumpForce = 5f; 

    // COMBAT
    [SerializeField] private float punchForce = 10f; 
    [SerializeField] private float punchMoveSpeedModifier = 0.4f; 
    [SerializeField] private float punchDuration = 0.75f; 
    [SerializeField] private float punchHitboxDuration = 1.1f; 
    [SerializeField] private float punchAnimationDuration = 1.5f;

    // AIMING
    [SerializeField] private float aimSensitivity = 3000f;
    [SerializeField] private float aimMovementSpeedMultiplier = 0.75f; 


    
    // COMPONENTS
    private Rigidbody rb; 
    private PlayerCamera playerCamera;
    private Animator animator;
    
    private string playerName; 
    private float playerNameOffset = 1.5f;

    Vector3 towerCenter;

    private bool isPaused = false;
    private bool isGrounded = true;

    private bool punching = false;
    private bool punchHitboxStarted = false;
    private float lastPunchTime; 

    private bool aiming;


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

            rightFistCollider.enabled = false;

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

            // AIMING
            if (Input.GetMouseButton(1) && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                aiming = true;
                Vector3 rotation = new Vector3(0f, Input.GetAxis("Mouse X") * aimSensitivity, 0f);
                transform.Rotate(rotation * Time.deltaTime, Space.World);
                
            }

            else {aiming = false;}

            playerCamera.ToggleAiming(aiming);


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
                // START PUNCH
                if (isGrounded && Time.time - lastPunchTime >= punchAnimationDuration)
                {
                    lastPunchTime = Time.time;
                    punching = true;
                    animator.SetTrigger("Punch"); 
                }
            }

        }

        // NAMETAG
        playerNameText.transform.LookAt(playerCamera.transform);
        playerNameText.rectTransform.Rotate(0, 180, 0);

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


        // START PUNCH HITBOX
        if (punching && !punchHitboxStarted && Time.time - lastPunchTime >= punchDuration)
        {
            punchHitboxStarted = true;
            rightFistCollider.enabled = true;
        }

        // END PUNCH HITBOX
        if (punching && punchHitboxStarted && Time.time - lastPunchTime >= punchHitboxDuration)
        {
            punchHitboxStarted = false;
            rightFistCollider.enabled = false;
        }

        // END PUNCH
        if (punching && Time.time - lastPunchTime >= punchAnimationDuration) {punching = false;}
        

        // MOVE SPEED
        if (aiming) 
        {
            if (punching) {speed = moveSpeed * aimMovementSpeedMultiplier * punchMoveSpeedModifier;}
            else {speed = moveSpeed * aimMovementSpeedMultiplier;}
        }

        else
        {
            if (punching) {speed = moveSpeed * punchMoveSpeedModifier;}
            else {speed = moveSpeed;}
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
                rb.MovePosition(rb.position + movement * speed * Time.deltaTime);
                animator.SetFloat("Speed", 0.5f);
                if (!aiming) {transform.rotation = Quaternion.LookRotation(movement.normalized);}
                
            }

            else {animator.SetFloat("Speed", 0); }// prob should not be instant


            if (rb.position.y < -5f) {Respawn();}
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
        // LANDING
        if (collision.gameObject.CompareTag("Ground")) 
        {
            isGrounded = true;
            rb.angularVelocity = Vector3.zero;
        } 

        // PUNCHING
        if (punching && punchHitboxStarted && collision.gameObject.CompareTag("Player"))
        {
            PlayerController controller = collision.gameObject.GetComponent<PlayerController>();
            if (controller != null && controller.gameObject != gameObject) {controller.PunchedRpc(transform.position);}
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



    public void Punched(Vector3 puncher)
    {
        if (!IsLocalPlayer) {return;}
        Rigidbody rb = GetComponent<Rigidbody>();
        Vector3 direction = (transform.position - puncher).normalized;
        rb.AddForce(direction * punchForce, ForceMode.Impulse);
    }


    [Rpc(SendTo.Everyone)]
    private void PunchedRpc(Vector3 puncher) {Punched(puncher);}
}
