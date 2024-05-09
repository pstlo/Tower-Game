using TMPro;
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
    [SerializeField] private float sprintMoveSpeedMultiplier = 1.25f; 
    [SerializeField] private float sprintTime = 3000; // ms i think idk
    [SerializeField] private float idleTimer = 15; 


    [SerializeField] private float jumpForce = 5f; 

    // COMBAT
    [SerializeField] private float punchForce = 6f; 
    [SerializeField] private float kickForce = 8f;
    [SerializeField] private float headbuttForce = 5f;  

    [SerializeField] private float punchMoveSpeedModifier = 0.4f; 
    [SerializeField] private float kickMoveSpeedModifier = 0f;
    [SerializeField] private float headbuttMoveSpeedModifier = 0.75f;  
    

    // THERES GOTTA BE A BETTER WAY TO DO THIS LOL 

    // PUNCH ANIMATION
    [SerializeField] private float punchHitboxStart = 0.3f; 
    [SerializeField] private float punchHitboxEnd = 0.75f; 
    [SerializeField] private float punchAnimationDuration = 0.76f;

    // KICK ANIMATION
    [SerializeField] private float kickAnimationDuration = 1.7f; 
    [SerializeField] private float kickHitboxStart = 0.4f; 
    [SerializeField] private float kickHitboxEnd = 0.8f; 

    // HEADBUTT ANIMATION
    [SerializeField] private float headbuttAnimationDuration = 1.7f; 
    [SerializeField] private float headbuttHitboxStart = 0.8f; 
    [SerializeField] private float headbuttHitboxEnd = 1f; 

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

    private float playerViewMouseSensitivity = 10f;

    Vector3 towerCenter;

    private float speed;
    private bool paused = false;
    private bool aiming;
    private bool grounded = true;


    private bool attacking = false;
    private bool attackStarted = false;
    private bool startHitbox = false;

    private float lastAttack;
    private float attackDuration;
    private float attackHitboxStart;
    private float attackHitboxEnd;

    private bool punching = false;
    private bool headbutting = false;
    private bool kicking = false;


    // ORIENTATION
    private float facing; // -1 or 1
    private bool movingForward;
    private bool facingForward;
    
    private bool towerView = true;
    // private bool climbingStairs = true; // to do
    public NetworkVariable<bool> blocking = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private bool sprinting;
    private float sprintMeter;
    private float timeToIdle = 0f;

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

            UIManager.Instance.ToggleTowerIndicatorMode(true);
            UIManager.Instance.ToggleTowerIndicator(true);
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
            playerCamera.TogglePause(paused);
        }

        // MOVES
        if (!paused && GameManager.Instance.CanMove()) 
        {

            JumpHandler();  
            SprintHandler(); 
            CombatHandler();
            
            if (!towerView) // MOUSE LOOK
            {
                float mouseX = Input.GetAxis("Mouse X") * playerViewMouseSensitivity; 
                transform.Rotate(Vector3.up, mouseX);
            }
        }

        if (paused) 
        {
            animator.SetBool("Moving",false);
            animator.SetBool("Sprinting",false);
            sprinting = false;
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

        MoveSpeedHandler();

        if (!paused && GameManager.Instance.CanMove())
        {
            MovementHandler();
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
            animator.SetBool("Grounded", grounded);
        } 

        // ATTACKING
        if (attacking && attackStarted && collision.gameObject.CompareTag("Player"))
        {
            PlayerController controller = collision.gameObject.GetComponent<PlayerController>();

            Debug.Log(GetName() + " attacks " + controller.GetName() + "..."); // COMBAT DEBUG

            if (controller != null && controller.gameObject != gameObject)
            {
                if (controller.blocking.Value)
                {
                    if (!(collision.collider == controller.playerCollider.rightFist ||
                        collision.collider == controller.playerCollider.leftFist ||
                        collision.collider == controller.playerCollider.rightArm ||
                        collision.collider == controller.playerCollider.leftArm)) 
                    {
                        AttackForceHandler(controller);
                        Debug.Log("It lands!"); // COMBAT DEBUG
                    }
                    
                    else {Debug.Log("But its blocked!");} // COMBAT DEBUG
                }
                else {AttackForceHandler(controller);}
            }
        }
    }

    void AttackForceHandler(PlayerController other)
    {
        float force = 0f;
        if (punching) {force = punchForce;}
        else if (kicking) {force = kickForce;}
        else if (headbutting) {force = headbuttForce;}
        
        other.AttackedRpc(transform.position,force);
    }

    void CombatHandler()
    {
        AimHandler();
        BlockHandler();
        AttackInputHandler();
        AttackHandler();
    }


    [Rpc(SendTo.Everyone)]
    private void UpdateNameTextRpc(string name) {playerNameText.text = name;}

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

    private void SetPauseMenuActive(bool active) 
    {
        UIManager.Instance.TogglePauseUI(active);
        UIManager.Instance.ToggleCursor(active);
    }

    public void Leave()
    {
        if (IsLocalPlayer)
        {
            UIManager.Instance.ToggleCursor(true);
            GameLobby.Instance.LeaveLobby();
            Destroy(playerCamera);
            Destroy(gameObject);
        }
    }


    private void MovementHandler()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        if (horizontalInput != 0 || verticalInput != 0) {timeToIdle = idleTimer;}
        else {timeToIdle -= Time.deltaTime;}

        if (timeToIdle <= 0) 
        {
            timeToIdle = 0;
            // IDLE STARTS HERE !!!
        }

        // else {;} // IDLE ENDS HERE !!!

        if (towerView) {TowerViewMovement(horizontalInput, verticalInput);}
        else {PlayerViewMovement(horizontalInput, verticalInput);}
    }

    private void TowerViewMovement(float horizontalInput, float verticalInput)
    {
        // ORIENTATION (RELATIVE TO CAMERA)
        Quaternion relativeRotation = Quaternion.Inverse(playerCamera.transform.rotation) * transform.rotation;
        Vector3 relativeEulerAngles = relativeRotation.eulerAngles;
        if (relativeEulerAngles.y > 180f) {relativeEulerAngles.y -= 360f;}

        if (relativeEulerAngles.y > 0) {facingForward = true;}
        else {facingForward = false;}
        

        Vector3 movement;
        Vector3 horizontalMove;
        Vector3 verticalMove;
        Vector3 circleCenterToPlayer = transform.position - towerCenter;
        Vector3 perpendicularToCircle = Vector3.Cross(circleCenterToPlayer, Vector3.up);
        horizontalMove = horizontalInput * (Quaternion.AngleAxis(0, Vector3.up) * perpendicularToCircle.normalized);
        verticalMove = -verticalInput * circleCenterToPlayer.normalized;
        movement = horizontalMove + verticalMove;
            
        if (movement.magnitude > 0)
        {
            rb.MovePosition(rb.position + movement * speed * Time.deltaTime);

            if (grounded) {animator.SetBool("Moving",true);}
            if (!aiming) {transform.rotation = Quaternion.LookRotation(movement.normalized);} // && climbingStairs

            if (horizontalInput < 0) {movingForward = false;}
            else {movingForward = true;}

            SprintInputHandler();
        }

        else 
        {
            animator.SetBool("Moving", false);
            animator.SetBool("Sprinting", false);
            sprinting = false;
        } 

        // True if movement aligns with orientation
        bool oriented = facingForward == movingForward;
        if (oriented) {facing = 1;}
        else {facing = -1;}
    }

    private void PlayerViewMovement(float horizontalInput, float verticalInput) 
    {
        facing = 1;

        animator.SetFloat("MoveX", horizontalInput);
        animator.SetFloat("MoveY", verticalInput);

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 movement = (forward * verticalInput + right * horizontalInput).normalized;


        if (movement.magnitude > 0)
        {
            rb.MovePosition(rb.position + movement * speed * Time.deltaTime);

            if (verticalInput < 0 || horizontalInput < 0) {movingForward = false;}
            else {movingForward = true;}

            // ANIMATION
            if (grounded) {animator.SetBool("Moving", true);}

            SprintInputHandler();
        }
        
        else 
        {
            animator.SetBool("Moving", false);
            animator.SetBool("Sprinting", false);
            sprinting = false;
        }

    }

    public void SetSpawn(Vector3 pos, Quaternion rot)
    {
        spawnPoint.position = pos;
        spawnPoint.rotation = rot;
    }


    public void Attacked(Vector3 attacker, float force)
    {
        if (!IsLocalPlayer) {return;}
        Rigidbody rb = GetComponent<Rigidbody>();
        Vector3 direction = (transform.position - attacker).normalized;
        rb.AddForce(direction * force, ForceMode.Impulse);
    }


    [Rpc(SendTo.Everyone)]
    private void AttackedRpc(Vector3 attacker, float force) {Attacked(attacker,force);}


    private void BlockHandler()
    {
        if (!attacking)
        {
            if (Input.GetMouseButtonDown(2)) 
            {
                playerCollider.ToggleBlockColliders(true);
                blocking.Value = true;
                animator.SetBool("Blocking",true);
            }

            if (Input.GetMouseButtonUp(2)) 
            {
                playerCollider.ToggleBlockColliders(false);
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
            animator.SetBool("Aiming", true);
        }
        
        if (Input.GetMouseButtonUp(1)) 
        {
            aiming = false;
            playerCamera.ToggleAiming(aiming);
            animator.SetBool("Aiming", false);
        }
    }


    private void JumpHandler()
    {
        if (grounded && !attacking && Input.GetKeyDown(KeyCode.Space)) 
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            grounded = false;
            animator.SetTrigger("Jump");
            animator.SetBool("Grounded", grounded);
        }
    }

    private void SprintInputHandler()
    {
        // Sprint input
        if (grounded && !attacking && Input.GetKey(KeyCode.LeftShift)) {sprinting = true;}
        else {sprinting = false;}
    }

    private void SprintHandler()
    {
        // Sprint cooldown
        if (sprinting) 
        {
            if (sprintMeter - Time.deltaTime > 0) {sprintMeter -= Time.deltaTime;}
            else 
            {
                sprintMeter = 0;
                sprinting = false;
            }
        }

        // Replenish
        if (!sprinting)
        {
            sprintMeter += Time.deltaTime;
            sprintMeter = Mathf.Min(sprintMeter, sprintTime);
        }
        
        animator.SetBool("Sprinting", sprinting);
    }


    private void AttackInputHandler()
    {
        bool canAttack = !attacking && grounded && Time.time - lastAttack >= attackDuration;

        if (!Input.GetKeyDown(KeyCode.C))
        {
            // PUNCH
            if (canAttack && Input.GetMouseButtonDown(0)) 
            {
                punching = true;
                canAttack = false;
                lastAttack = Time.time;
                attacking = true;
                startHitbox = true;

                attackDuration = punchAnimationDuration; 
                attackHitboxStart = punchHitboxStart;
                attackHitboxEnd = punchHitboxEnd;

                animator.SetTrigger("Punch"); 

                // Debug.Log("Started punch"); // COMBAT DEBUG
            }   

            // KICK
            if (canAttack && Input.GetKeyDown(KeyCode.F)) 
            {
                kicking = true;
                canAttack = false;
                lastAttack = Time.time;
                attacking = true;
                startHitbox = true;

                attackDuration = kickAnimationDuration; 
                attackHitboxStart = kickHitboxStart;
                attackHitboxEnd = kickHitboxEnd;

                animator.SetTrigger("Kick");
                animator.SetLayerWeight(2,0f);

                // Debug.Log("Started kick"); // COMBAT DEBUG
            }

            // HEADBUTT
            if (canAttack && Input.GetKeyDown(KeyCode.Q)) 
            {
                headbutting = true;
                canAttack = false;
                lastAttack = Time.time;
                attacking = true;
                startHitbox = true;

                attackDuration = headbuttAnimationDuration; 
                attackHitboxStart = headbuttHitboxStart;
                attackHitboxEnd = headbuttHitboxEnd;

                animator.SetTrigger("Headbutt");

                // Debug.Log("Started headbutt"); // COMBAT DEBUG
            }
        }
    }

    private void AttackHandler()
    {
        float time = Time.time;

        // START ATTACK HITBOX
        if (attacking && !attackStarted && startHitbox && time - lastAttack >= attackHitboxStart)
        {
            // Debug.Log("Hitbox started"); // COMBAT DEBUG
            attackStarted = true;
            startHitbox = false;
            if (punching) {playerCollider.TogglePunchColliders(true);}
            if (kicking) {playerCollider.ToggleKickColliders(true);}
            if (headbutting) {playerCollider.ToggleHeadbuttColliders(true);}
        }

        // END ATTACK HITBOX
        if (attackStarted && time - lastAttack >= attackHitboxEnd)
        {
            attackStarted = false;
            
            if (punching) 
            {
                playerCollider.TogglePunchColliders(false);
                punching = false;
            }

            if (kicking) 
            {
                playerCollider.ToggleKickColliders(false);
                kicking = false;
            }

            if (headbutting) 
            {
                playerCollider.ToggleHeadbuttColliders(false);
                headbutting = false;
            }

            // Debug.Log("Hitbox ended"); // COMBAT DEBUG
        }

        // END ATTACK
        if (attacking && time - lastAttack >= attackDuration) 
        {
            attacking = false;
            // Debug.Log("Attack ended"); // COMBAT DEBUG
        }

        if (!attacking) {animator.SetLayerWeight(2,1f);}

    }


    private void MoveSpeedHandler()
    {
        // Guarding
        if (blocking.Value)
        {
            speed = moveSpeed * blockMovementSpeedMultiplier;
            animator.SetFloat("MoveSpeed",facing * blockMovementSpeedMultiplier);
        }

        else if (attacking)
        {
            // Punching
            if (punching) 
            {
                speed = moveSpeed * punchMoveSpeedModifier;
                animator.SetFloat("MoveSpeed",facing * punchMoveSpeedModifier);
            }

            // Kicking
            else if (kicking) {speed = moveSpeed * kickMoveSpeedModifier;}

            // Headbutting
            else if (headbutting) 
            {
                speed = moveSpeed * headbuttMoveSpeedModifier;
                animator.SetFloat("MoveSpeed",facing * headbuttMoveSpeedModifier);
            }
        }
        
        // Aiming
        else if (aiming) 
        {
            speed = moveSpeed * aimMovementSpeedMultiplier;
            animator.SetFloat("MoveSpeed",facing * aimMovementSpeedMultiplier);
        }
            
        // Regular movement
        else 
        {
            if (sprinting) {speed = sprintMoveSpeedMultiplier * moveSpeed;}
            else {speed = moveSpeed;}
            animator.SetFloat("MoveSpeed",1f);
        }
        
    }

    private void ViewHandler()
    {
        // TOGGLE TOWER VIEW
        if (Input.GetKeyDown(KeyCode.CapsLock)) 
        {
            towerView = !towerView;
            playerCamera.SetTowerView(towerView);
            UIManager.Instance.ToggleTowerIndicatorMode(towerView);
        }

        animator.SetBool("TowerView",towerView);
    }

    private void DisableColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders) {collider.enabled = false;}
    }

}
