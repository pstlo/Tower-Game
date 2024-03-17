using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : NetworkBehaviour
{    
    [SerializeField] public Transform spawnPoint;
    [SerializeField] private GameObject playerCameraPrefab;
    [SerializeField] private TMP_Text playerNamePrefab;
    [SerializeField] private float moveSpeed = 10f; 
    [SerializeField] private float jumpForce = 5f; 
    private Rigidbody rb; 
    private PlayerCamera playerCamera;
    private bool isPaused = false;
    private bool isGrounded = true; 
    private TMP_Text playerNameText;
    private string playerName;
    private float playerNameOffset = 1.5f;

    void Start()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        NetworkObject.DestroyWithScene = true;
        playerName = GameLobby.Instance.playerName;
        gameObject.name = "Player: " + playerName;

        if (IsLocalPlayer)
        {
            GameObject newCamera = Instantiate(playerCameraPrefab, transform.position, Quaternion.identity);
            PlayerCamera newPlayerCamera = newCamera.GetComponent<PlayerCamera>();

            newPlayerCamera.player = transform;
            playerCamera = newPlayerCamera;
            playerCamera.transform.SetParent(transform);
            playerNameText = Instantiate(playerNamePrefab, transform.position + Vector3.up * playerNameOffset, Quaternion.identity);
            playerNameText.text = playerName;
            playerNameText.transform.SetParent(transform);
            playerNameText.transform.localPosition = Vector3.up * playerNameOffset;
            Respawn();
        }
    }

    void Update()
    {
        if (!IsOwner) {return;}

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isPaused = !isPaused;
            SetPauseMenuActive(isPaused);
        }

        if (!isPaused && isGrounded && Input.GetKeyDown(KeyCode.Space)) 
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false; 
        }
        playerNameText.transform.LookAt(playerCamera.transform);
        playerNameText.transform.Rotate(0, 180, 0);
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

        if (!isPaused)
        {
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            Vector3 cameraForward = playerCamera.transform.forward;
            Vector3 cameraRight = playerCamera.transform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();
            Vector3 movementDirection = (cameraForward * verticalInput + cameraRight * horizontalInput).normalized;
            Vector3 movement = movementDirection * moveSpeed * Time.deltaTime;
            rb.MovePosition(rb.position + movement);

            if (rb.position.y < -5f) {Respawn();}
        }
    }

     private void HandleDisconnect(ulong clientId)
    {
        if (clientId == OwnerClientId) {Leave();}
    }

    public void Respawn()
    {
        if (spawnPoint != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }
    }

    [Rpc(SendTo.Everyone)]
    public void RespawnRpc() {Respawn();}

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground")) {isGrounded = true;}
    }


    private void SetPauseMenuActive(bool active)
    {
        if (PauseMenuManager.Instance != null) {PauseMenuManager.Instance.TogglePauseMenu(active);}
    }

    public void Leave()
    {
        GameLobby.Instance.LeaveLobby();
        Destroy(playerCamera);
        Destroy(gameObject);
    }
}
