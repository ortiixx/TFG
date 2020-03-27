using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FABRIKJoint : MonoBehaviour
{
    public enum JointType
    {
        Hinge,
        SocketBall,
        Rotator
    }
    public JointType JointConstraintType;
    public float angleMin1;
    public float angleMax1;
    public float angleMin2;
    public float angleMax2;

    public Quaternion OriginalRotator;

    // Start is called before the first frame update
    void Start()
    {
        OriginalRotator = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
