using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SocketBallTest : MonoBehaviour
{
    public Transform Child;
    public Quaternion orientation;
    public Quaternion originalQuat;
    public Quaternion originalQuat2;
    public float Limit1X = 50f;
    public float Limit2X = 50f;
    public float Limit1Y = 50f;
    public float Limit2Y = 50f;
    Vector3 OriginalFwd;

    private void Start()
    {
        orientation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
        originalQuat = orientation*transform.rotation;
        originalQuat2 = transform.rotation;
        //transform.rotation = originalQuat;
    }

    private void Update()
    {
        Vector3 dir = Vector3.Normalize(Child.position - transform.position);
        Vector3 dirXZ = Vector3.ProjectOnPlane(dir, originalQuat * Vector3.up); //Dir projected in the plane with normal local-up
        float AngleX = Vector3.SignedAngle(originalQuat * Vector3.forward, dirXZ, originalQuat * Vector3.up);
        AngleX = Mathf.Clamp(AngleX, Limit1X, Limit2X);
        Quaternion q0 = Quaternion.AngleAxis(AngleX, orientation * Vector3.up);

        Vector3 dirYZ = Vector3.ProjectOnPlane(dir, q0*originalQuat * Vector3.right); //Dir projected in the plane with normal local-up
        float AngleY = Vector3.SignedAngle(q0 * originalQuat * Vector3.forward, dirYZ, q0 * originalQuat * Vector3.right);
        AngleY = Mathf.Clamp(AngleY, Limit1Y, Limit2Y);

        Quaternion q1 = Quaternion.AngleAxis(AngleY, orientation*Vector3.right);

        transform.rotation = originalQuat2*q0*q1;

        Debug.DrawRay(transform.position, dir * 10, Color.red);
        Debug.DrawRay(transform.position, originalQuat * Vector3.forward * 10, Color.blue);
        Debug.DrawRay(transform.position, transform.up * 10, Color.blue);
        Debug.Log(AngleX); Debug.Log(AngleY);
    }
}
