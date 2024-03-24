using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform tower;
    [SerializeField] private float cameraDist = 30f;

    Vector3 centerPos;

    private float cameraDirection = -1f; // -1 -> forwards,  1 -> backwards

    void Start() {centerPos = tower.position;}

    void FixedUpdate()
    {
        if (player == null) 
        {
            Destroy(gameObject);
            return;
        }
        
        centerPos.y = player.position.y;
        
        float angle = Vector3.Angle(player.position,centerPos);
        Vector3 directionToPlayer = (player.position - centerPos).normalized;
        
        transform.position = centerPos + Quaternion.Euler(0, cameraDirection * angle, 0) * directionToPlayer * cameraDist;
        transform.LookAt(player);
    }

    public void setPlayer(Transform player) {this.player = player;}
}
