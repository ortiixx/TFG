using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FABRIKJoint : MonoBehaviour
{
    public float angle1;
    public float angle2;
    public float angle3;
    public float angle4;

    public Vector3 JointForward;
    public Vector3 JointUp;

    // Update is called once per frame
    void Update()
    {
        Debug.DrawRay(transform.position, transform.rotation*JointForward*5f, Color.blue);
        Debug.DrawRay(transform.position, transform.rotation * JointUp *5f, Color.green);
        Debug.DrawRay(transform.position, transform.rotation * Vector3.Cross(JointUp, JointForward)*5f, Color.red);
        Debug.DrawRay(Vector3.zero, Vector3.up * 10f);
    }
}
