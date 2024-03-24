using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform tower;
    [SerializeField] private float cameraDist = 30f;

    private Vector3 towerPosition;

    void Start() {towerPosition = tower.position;}
    
    void FixedUpdate()
    {
        if (player == null || tower == null) 
        {
            Destroy(gameObject);
            return;
        }


        towerPosition.y = player.position.y;

        Vector3 directionToPlayer = (player.position - towerPosition).normalized;
        Vector3 targetPosition = towerPosition + directionToPlayer * cameraDist;
        targetPosition.y = player.position.y;

        transform.position = targetPosition;
        transform.LookAt(player);
    }

    public void SetPlayer(Transform player) 
    { 
        this.player = player; 
    }
}
