using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : NetworkBehaviour
{    
    [SerializeField] public Transform spawnPoint;
    [SerializeField] private float moveSpeed = 10f; 
    [SerializeField] private float jumpForce = 5f; 

    private Rigidbody rb; 
    [SerializeField] private GameObject playerCameraPrefab;
    private PlayerCamera playerCamera;
    private bool isPaused = false;
    private bool isGrounded = true; 

    //Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        NetworkObject.DestroyWithScene = true;

        if (IsLocalPlayer)
        {
            GameObject newCamera = Instantiate(playerCameraPrefab, transform.position, Quaternion.identity);
            PlayerCamera newPlayerCamera = newCamera.GetComponent<PlayerCamera>();

            if (newPlayerCamera != null)
            {
                newPlayerCamera.player = transform;
                playerCamera = newPlayerCamera;
            }

            Respawn();
            
        }
    }

    void Update()
    {
        if (!IsOwner) return;

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


    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true; 
        }
    }


    private void SetPauseMenuActive(bool active)
    {
        if (PauseMenuManager.Instance != null)
        {
            PauseMenuManager.Instance.TogglePauseMenu(active);
        }
    }

    public void Leave()
    {
        GameLobby.Instance.LeaveLobby();
        Destroy(playerCamera);
        Destroy(gameObject);
    }
}
