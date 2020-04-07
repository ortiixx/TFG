using UnityEngine;
using System.Collections;

public class test : MonoBehaviour
{
    public Transform lookTarget;

    // in degrees
    public float leftExtent;
    public float rightExtent;
    public float upExtent;
    public float downExtent;
    [SerializeField, HideInInspector]
    Quaternion
            original;
    private Vector3 dirXZ, forwardXZ, dirYZ, forwardYZ;

    // Use this for initialization
    void Start()
    {
        original = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 dirToTarget = (lookTarget.position - transform.position);
        Vector3 originalForward = original * Vector3.forward;

        Vector3 yAxis = Vector3.up; // world y axis
        dirXZ = Vector3.ProjectOnPlane(dirToTarget, yAxis);
        forwardXZ = Vector3.ProjectOnPlane(originalForward, yAxis);
        float yAngle = Vector3.Angle(dirXZ, forwardXZ) * Mathf.Sign(Vector3.Dot(yAxis, Vector3.Cross(forwardXZ, dirXZ)));
        float yClamped = Mathf.Clamp(yAngle, leftExtent, rightExtent);
        Quaternion yRotation = Quaternion.AngleAxis(yClamped, Vector3.up);

        Debug.Log(string.Format("Desired Y rotation: {0}, clamped Y rotation: {1}", yAngle, yClamped), this);

        originalForward = yRotation * original * Vector3.forward;
        Vector3 xAxis = yRotation * original * Vector3.right; // our local x axis
        dirYZ = Vector3.ProjectOnPlane(dirToTarget, xAxis);
        forwardYZ = Vector3.ProjectOnPlane(originalForward, xAxis);
        float xAngle = Vector3.Angle(dirYZ, forwardYZ) * Mathf.Sign(Vector3.Dot(xAxis, Vector3.Cross(forwardYZ, dirYZ)));
        float xClamped = Mathf.Clamp(xAngle, upExtent, downExtent);
        Quaternion xRotation = Quaternion.AngleAxis(xClamped, Vector3.right);

        Debug.Log(string.Format("Desired X rotation: {0}, clamped X rotation: {1}", xAngle, xClamped), this);


        Quaternion newRotation = yRotation * original * xRotation;
        transform.rotation = newRotation;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + dirXZ);
        Gizmos.DrawLine(transform.position, transform.position + forwardXZ);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + dirYZ);
        Gizmos.DrawLine(transform.position, transform.position + forwardYZ);
    }
}