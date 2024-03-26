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
    [SerializeField] private int maxHeight = 100;
    [SerializeField] private int minHeight = 0;
    [SerializeField] private float scrollingHeightSpeed = 3;

    private float tiltAngle = 0;
    private float orbitAngle = 0;
    private float cameraDist = 30;
    private float cameraHeight;
    private bool orbiting = false;
    private bool movingOrbit = false;
    private bool scrollingHeight = false;

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
        if (!scrollingHeight) {cameraHeight = towerPosition.y;}


        if (!orbiting)
        {
            Vector3 directionToPlayer = (player.position - towerPosition).normalized;
            Vector3 targetPosition = towerPosition + directionToPlayer * cameraDist;
            targetPosition.y = cameraHeight;
            transform.position = targetPosition;
            transform.LookAt(player);
            Quaternion rotation = Quaternion.Euler(tiltAngle, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
            transform.rotation = rotation;
        }
        
        

        float mouseWheel = Input.GetAxis("Mouse ScrollWheel");

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            Vector3 targetPosition = towerPosition;

            // HEIGHT SCROLL
            if (mouseWheel != 0) 
            {
                Debug.Log("transform: " + transform.position.y + " height: " + cameraHeight);
                scrollingHeight = true;
                cameraHeight += mouseWheel * scrollingHeightSpeed;
                if (cameraHeight > maxHeight) {cameraHeight = Mathf.Max(maxHeight, player.position.y);}
                if (cameraHeight < minHeight) {cameraHeight = Mathf.Min(minHeight, player.position.y);}
            }  
        

            if (scrollingHeight) {targetPosition.y = cameraHeight;}

            // CAMERA TILT
            if (Input.GetMouseButton(1))
            {
                float mouseY = Input.GetAxis("Mouse Y");
                tiltAngle -= mouseY * tiltSpeed;
                tiltAngle = Mathf.Clamp(tiltAngle, minTilt, maxTilt);
            }

            // CAMERA ORBIT
            if (Input.GetMouseButtonDown(0))
            {
                orbiting = true;
                movingOrbit = true;
                Vector3 offset = transform.position - targetPosition;
                orbitAngle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
            }

            if (Input.GetMouseButtonUp(0)) 
            {
                movingOrbit = false;
            }

            if (movingOrbit)
            {
                float mouseX = Input.GetAxis("Mouse X");
                orbitAngle += mouseX * orbitSpeed;
               
            }

            if (orbiting)
            {
                Vector3 orbitPosition = targetPosition + Quaternion.Euler(tiltAngle, orbitAngle, cameraDist) * new Vector3(0f, 0f, cameraDist);
                transform.position = orbitPosition;
                transform.LookAt(targetPosition);
            }
        }

        else 
        {
            tiltAngle = 0;
            orbiting = false;
            movingOrbit = false;
            scrollingHeight = false;

            // CAMERA ZOOM
            if (mouseWheel != 0)
            {
                cameraDist -= mouseWheel * zoomSpeed;
                cameraDist = Mathf.Clamp(cameraDist, maxZoom, minZoom);
            }
        }
    }


    public void SetPlayer(Transform player) {this.player = player;}

}
