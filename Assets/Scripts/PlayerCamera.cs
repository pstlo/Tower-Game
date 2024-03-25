using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform tower;
    
    [SerializeField] private float maxZoom = 20;
    [SerializeField] private float minZoom = 100;
    [SerializeField] private float zoomSpeed = 5;
    [SerializeField] private float maxTilt = 60;
    [SerializeField] private float minTilt = -60;
    [SerializeField] private float tiltSpeed = 2;
    [SerializeField] private float orbitSpeed = 2;

    bool orbiting = false;
    private float tiltAngle = 0;
    private float orbitAngle = 0f;
    private float cameraDist = 30;

    private Vector3 towerPosition;


    void Start() {towerPosition = tower.position;}
    
    void Update()
    {
        if (player == null || tower == null) 
        {
            Destroy(gameObject);
            return;
        }

        towerPosition.y = player.position.y;

        if (!orbiting)
        {
            Vector3 directionToPlayer = (player.position - towerPosition).normalized;
            Vector3 targetPosition = towerPosition + directionToPlayer * cameraDist;
            targetPosition.y = player.position.y;
            transform.position = targetPosition;
            transform.LookAt(player);
        }

        // CAMERA ZOOM
        float mouseWheel = Input.GetAxis("Mouse ScrollWheel");
        cameraDist -= mouseWheel * zoomSpeed;
        cameraDist = Mathf.Clamp(cameraDist, maxZoom, minZoom);


        Quaternion rotation = Quaternion.Euler(tiltAngle, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
        transform.rotation = rotation;

        // CAMERA TILT
        if (Input.GetMouseButton(1))
        {
            float mouseY = Input.GetAxis("Mouse Y");
            tiltAngle -= mouseY * tiltSpeed;
            tiltAngle = Mathf.Clamp(tiltAngle, minTilt, maxTilt);
        }

        if (Input.GetMouseButtonUp(1)) {tiltAngle = 0;}

        // CAMERA ORBIT
        if (Input.GetMouseButtonDown(0))
        {
            orbiting = true;
            Vector3 offset = transform.position - towerPosition;
            orbitAngle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        }

        if (Input.GetMouseButtonUp(0)) {orbiting = false;}

        if (orbiting)
        {
            float mouseX = Input.GetAxis("Mouse X");
            orbitAngle += mouseX * orbitSpeed;
            Vector3 orbitPosition = towerPosition + Quaternion.Euler(0f, orbitAngle, 0f) * new Vector3(0f, 0f, cameraDist);
            transform.position = orbitPosition;
            transform.LookAt(towerPosition);
        }
    }

    public void SetPlayer(Transform player) {this.player = player;}

}
