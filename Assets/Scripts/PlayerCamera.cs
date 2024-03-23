using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform tower;
    [SerializeField] private float cameraDist = 30f;


    void FixedUpdate()
    {
        if (player == null) 
        {
            Destroy(gameObject);
            return;
        }
        
        Vector3 centerPos = tower.position;
        centerPos.y = player.position.y;
        float angle = Vector3.Angle(player.position,centerPos);
        Vector3 directionToPlayer = (player.position - centerPos).normalized;
        Vector3 desiredPosition = centerPos + Quaternion.Euler(0, angle, 0) * directionToPlayer * cameraDist;
        transform.position = desiredPosition;
        transform.LookAt(player);
    }

    public void setPlayer(Transform player) {this.player = player;}
}
