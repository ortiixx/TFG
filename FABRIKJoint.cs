using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FABRIKJoint : MonoBehaviour
{
    public enum JointType
    {
        Hinge,
        Socketball
    }

    public float angle1;
    public float angle2;
    public float angle3;
    public float angle4;
    public JointType RestrictionType = JointType.Socketball;
    public Vector3 Axis;
    public Vector3 TwistAxis;
    public Vector3 Forward;
    private void Start()
    {
        if (Axis != Vector3.zero && TwistAxis != Vector3.zero)
            Forward = Vector3.Cross(Axis, TwistAxis);
        else
            Debug.LogError("Axis not set at " + transform.name);
    }   

    // Update is called once per frame
    void Update()
    {

    }
}
