using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCollider : MonoBehaviour
{
    // COLLIDERS
    public Collider rightFist; 
    public Collider leftFist; 
    public Collider rightArm; 
    public Collider leftArm; 

    public Collider rightFoot;
    public Collider leftFoot;
    public Collider rightLeg;
    public Collider leftLeg;

    public Collider noggin;
    

    public void TogglePunchColliders(bool active)
    {
        rightFist.enabled = active;
    }
    
    public void ToggleKickColliders(bool active)
    {
        rightFoot.enabled = active;
        rightLeg.enabled = active;
    }
    
    public void ToggleHeadbuttColliders(bool active) {noggin.enabled = active;}

    public void ToggleBlockColliders(bool active)
    {
        rightFist.enabled = active;
        leftFist.enabled = active;
        rightArm.enabled = active;
        leftArm.enabled = active;
    }

    // more unreferenced

}
