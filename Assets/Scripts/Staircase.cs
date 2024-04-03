using UnityEngine;

//[ExecuteInEditMode] // SPAWNS IN EDITOR - ONLY RUN ONCE
public class Staircase : MonoBehaviour
{
    [SerializeField] private GameObject tower;
    [SerializeField] private GameObject stepPrefab;
    [SerializeField] private int numStairs; // 368
    [SerializeField] private float stairAngle = 10f;


    void Start()
    {
        Vector3 centerBottom = gameObject.transform.position;
        float radius = tower.transform.localScale.x;
        float angleDegrees = stairAngle;
        float angleRadians = angleDegrees * Mathf.PI / 180f;

        for (var i = 0; i < numStairs; i++)
        {
            float x = radius * Mathf.Cos(i * angleRadians);
            float z = radius * Mathf.Sin(i * angleRadians);
            Vector3 pos = new Vector3(centerBottom.x + x, i * 0.2f, centerBottom.z + z); 
            Quaternion rot = Quaternion.Euler(0, -i * angleDegrees, 0); 
            GameObject step = Instantiate(stepPrefab, pos, rot);
            step.transform.SetParent(tower.transform);
        }
    }

}
