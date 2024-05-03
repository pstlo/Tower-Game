using Unity.Netcode;
using UnityEngine;

public class Boulder : NetworkBehaviour
{
    [SerializeField] private Transform tower;
    [SerializeField] private float speed = 2.0f;
    [SerializeField] private float knockbackForce = 25f;

    private float direction = -1; // clockwise
    private float despawnHeight = 1;

    private Rigidbody rb;
    private Vector3 towerCenter;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        towerCenter = tower.position;
    }

    private void Update()
    {
        towerCenter.y = transform.position.y;
        Vector3 directionToTower = towerCenter - transform.position;
        float radius = directionToTower.magnitude;
        float y = transform.position.y;
        float angle = direction * Time.time * speed; 
        float x = towerCenter.x + radius * Mathf.Cos(angle);
        float z = towerCenter.z + radius * Mathf.Sin(angle);
        transform.position = new Vector3(x, y, z);

        if (IsOwner && transform.position.y < despawnHeight) {Destroy(gameObject);}
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Vector3 knockbackDirection = collision.transform.position - transform.position;
            knockbackDirection.y = 0f; 

            Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
            if (playerRb != null) {playerRb.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode.Impulse);}
        }    
    }
}
