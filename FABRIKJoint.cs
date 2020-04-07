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
    public float angle1;
    public float angle2;
    public float angle3;
    public float angle4;

    public Vector3 OriginalForward;

    // Start is called before the first frame update
    void Start()
    {
        OriginalForward = transform.forward;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
