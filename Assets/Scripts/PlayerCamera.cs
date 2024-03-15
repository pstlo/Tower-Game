using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] public Transform player;
    [SerializeField] private Transform tower;
    [SerializeField] private float cameraDist = 30f;

    // Update is called once per frame
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
}
