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

    
    // MOVEMENT
    [SerializeField] private float moveSpeed = 10f; 
    [SerializeField] private float jumpForce = 5f; 


    // COMBAT
    [SerializeField] private float punchForce = 6f; 
    [SerializeField] private float punchMoveSpeedModifier = 0.4f; 
    [SerializeField] private float punchHitboxStart = 0.3f; 
    [SerializeField] private float punchHitboxEnd = 0.75f; 
    [SerializeField] private float punchAnimationDuration = 0.76f;
    [SerializeField] private float blockMovementSpeedMultiplier = 0.25f;


    // AIMING
    [SerializeField] private float aimSensitivity = 3000f;
    [SerializeField] private float aimMovementSpeedMultiplier = 0.75f; 
    

    // COMPONENTS
    private Rigidbody rb; 
    private PlayerCamera playerCamera;
    private Animator animator;
    public PlayerCollider playerCollider;
    

    private string playerName; 
    private float playerNameOffset = 1.5f;

    Vector3 towerCenter;

    private bool paused = false;
    private bool grounded = true;
    private bool punching = false;
    private bool punchHitboxStarted = false;
    private float lastPunchTime; 
    private bool aiming;
    private float speed;
    private bool towerView = true;
    public NetworkVariable<bool> blocking = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private float mouseSensitivity = 10f;

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
        blocking.Value = false;

        DisableColliders();
        gameObject.GetComponent<Collider>().enabled = true;

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

        // PAUSE
        if (Input.GetKeyDown(KeyCode.Escape)) 
        {
            paused = !paused;
            SetPauseMenuActive(paused);
        }

        // MOVES
        if (!paused && GameManager.Instance.CanMove()) 
        {
            AimHandler();
            BlockHandler();
            JumpHandler();   
            PunchStartHandler();
            
            if (!towerView) 
            {            
                float mouseX = Input.GetAxis("Mouse X");
                transform.Rotate(Vector3.up, mouseX * mouseSensitivity);
            }
        }

        // NAMETAG
        playerNameText.transform.LookAt(playerCamera.transform);
        playerNameText.rectTransform.Rotate(0, 180, 0);

        towerCenter.y = gameObject.transform.position.y;

        ViewHandler();
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

        PunchHandler();
        MoveSpeedHandler();


        if (!paused && GameManager.Instance.CanMove())
        {
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            Vector3 movement;
            Vector3 horizontalMove;
            Vector3 verticalMove;

            if (towerView)
            {
                Vector3 circleCenterToPlayer = transform.position - towerCenter;
                Vector3 perpendicularToCircle = Vector3.Cross(circleCenterToPlayer, Vector3.up);
                horizontalMove = horizontalInput * (Quaternion.AngleAxis(0, Vector3.up) * perpendicularToCircle.normalized);
                verticalMove = -verticalInput * circleCenterToPlayer.normalized;
                movement = horizontalMove + verticalMove;
            }

            else 
            {
                Vector3 forward = transform.forward * verticalInput;
                Vector3 right = transform.right * horizontalInput;
                movement = forward + right;
            }

            // MOVING
            if (movement.magnitude > 0)
            {
                rb.MovePosition(rb.position + movement * speed * Time.deltaTime);
                
                if (!aiming) 
                {
                    animator.SetFloat("Speed", 1);
                    if (towerView) {transform.rotation = Quaternion.LookRotation(movement.normalized);}


                }
            }

            // IDLE
            else 
            {
                if (!aiming) {animator.SetFloat("Speed", 0);} // prob should not be instant
            } 



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
            grounded = true;
            rb.angularVelocity = Vector3.zero;
        } 

        // PUNCHING
        if (punching && punchHitboxStarted && collision.gameObject.CompareTag("Player"))
        {
            PlayerController controller = collision.gameObject.GetComponent<PlayerController>();
            if (controller != null && controller.gameObject != gameObject)
            {
                if (controller.blocking.Value)
                {
                    if (!(collision.collider == controller.playerCollider.rightFist ||
                        collision.collider == controller.playerCollider.leftFist ||
                        collision.collider == controller.playerCollider.rightArm||
                        collision.collider == controller.playerCollider.leftArm)) {controller.PunchedRpc(transform.position);}
                }
                else {controller.PunchedRpc(transform.position);}
            }
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


    private void BlockHandler()
    {
        if (!punching)
        {
            if (Input.GetMouseButtonDown(2)) 
            {
                playerCollider.rightFist.enabled = true;
                playerCollider.leftFist.enabled = true;
                playerCollider.rightArm.enabled = true;
                playerCollider.leftArm.enabled = true;
                blocking.Value = true;
                animator.SetBool("Blocking",true);
            }

            if (Input.GetMouseButtonUp(2)) 
            {
                playerCollider.rightFist.enabled = false;
                playerCollider.leftFist.enabled = false;
                playerCollider.rightArm.enabled = false;
                playerCollider.leftArm.enabled = false;
                blocking.Value = false;
                animator.SetBool("Blocking",false);
            }
        }
    }


    private void AimHandler()
    {
        if (aiming && towerView) 
        {
            Vector3 rotation = new Vector3(0f, Input.GetAxis("Mouse X") * aimSensitivity, 0f);
            transform.Rotate(rotation * Time.deltaTime);
        }

        if (Input.GetMouseButtonDown(1) && !Input.GetKey(KeyCode.C))
        {
            aiming = true;
            playerCamera.ToggleAiming(aiming);
            animator.SetFloat("Speed", 0.5f);
        }
        
        if (Input.GetMouseButtonUp(1)) 
        {
            aiming = false;
            playerCamera.ToggleAiming(aiming);
        }
    }


    private void JumpHandler()
    {
        if (grounded && !punching && Input.GetKeyDown(KeyCode.Space)) 
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            grounded = false;
            animator.SetTrigger("Jump");
        }
    }


    private void PunchStartHandler()
    {
        if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.C))
        {
            // START PUNCH
            if (grounded && Time.time - lastPunchTime >= punchAnimationDuration)
            {
                lastPunchTime = Time.time;
                punching = true;
                animator.SetTrigger("Punch"); 
            }
        }
    }


    private void PunchHandler()
    {
        // START PUNCH HITBOX
        if (punching && !punchHitboxStarted && Time.time - lastPunchTime >= punchHitboxStart)
        {
            punchHitboxStarted = true;
            playerCollider.rightFist.enabled = true;
        }

        // END PUNCH HITBOX
        if (punching && punchHitboxStarted && Time.time - lastPunchTime >= punchHitboxEnd)
        {
            punchHitboxStarted = false;
            playerCollider.rightFist.enabled = false;
        }

        // END PUNCH
        if (punching && Time.time - lastPunchTime >= punchAnimationDuration) {punching = false;}
    }


    private void MoveSpeedHandler()
    {
        if (blocking.Value)
        {
            speed = moveSpeed * blockMovementSpeedMultiplier;
        }

        else if (aiming) 
        {
            if (punching) {speed = moveSpeed * aimMovementSpeedMultiplier * punchMoveSpeedModifier;}
            else {speed = moveSpeed * aimMovementSpeedMultiplier;}
        }

        else
        {
            if (punching) {speed = moveSpeed * punchMoveSpeedModifier;}
            else {speed = moveSpeed;}
        }
    }

    private void ViewHandler()
    {
        // TOGGLE TOWER VIEW
        if (Input.GetKeyDown(KeyCode.CapsLock)) 
        {
            towerView = !towerView;
            playerCamera.SetTowerView(towerView);
            UIManager.Instance.ToggleTowerCameraIndicator(towerView);
        }
    }

    private void DisableColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders) {collider.enabled = false;}
    }

}
