﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FABRIK : MonoBehaviour
{
    public List<Transform> Joints;
    public Transform Objective;
    public Vector3 InitialRootLocation;
    public float Threshold = 0.24f;
    public int MaxIterations = 10;
    private float TotalLength = 0f;
    public List<float> Lengths;         //length[i] = Distance(i,i+1)
    public LayerMask layerMask;
    public Vector3 ObjectiveLastPosition;
    Quaternion quatero;

    // Start is called before the first frame update
    void Start()
    {
        InitialRootLocation = transform.position;
        Joints = new List<Transform>();
        Lengths = new List<float>();
        foreach (FABRIKJoint t in transform.GetComponentsInChildren<FABRIKJoint>())
        {
            Rigidbody rb = t.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = true;
                rb.GetComponent<Collider>().enabled = false;
                rb.detectCollisions = false;
                Joints.Add(t.transform);
            }
        }

        TotalLength = 0f;
        for (int i = 1; i < Joints.Count; i++)
        {
            Lengths.Add(Vector3.Distance(Joints[i].transform.position, Joints[i - 1].transform.position));
            TotalLength += Lengths[i - 1];
        }
        ObjectiveLastPosition = Objective.position;
    }

    Vector2 GetClosestPoint(Vector2 pos1, float l1, float l2)
    {
        if (l1 > l2)
            return MathHelpers.ClosestPointEllipse(pos1, l1, l2);
        else
            return MathHelpers.ClosestPointEllipse(pos1, l2, l1);
    }

    Vector2 GetQuadrantPosition(Vector3 Orpos, float l1, float l2, float l3, float l4)
    { //Quadrants counter-clockwise
        Vector2 Pos = new Vector2(Orpos.x, Orpos.z);
        if (Pos.x <= l3 && Pos.x >= -l4 && Pos.y >= -l1 && Pos.y <= l2) //Within bounds
            return Pos;
        Debug.Log("Not within bounds");
        if (Orpos.x > 0 && Orpos.z > 0)
        { //blue-cyan
            return GetClosestPoint(Pos, l2, l3);
        }
        else if (Orpos.z > 0) //cyan-red
        {
            return GetClosestPoint(Pos, l1, l3);
        }
        else if (Orpos.x < 0)
        {
            Pos.y *= -1;
            Pos = GetClosestPoint(Pos, l1, l4);
            Pos.y *= -1;
            return Pos;

        }
        else
        {
            Pos.y *= -1;
            Pos = GetClosestPoint(Pos, l2, l4);
            Pos.y *= -1;
            return Pos;
        }
    }

    bool HasNoLimits(CharacterJoint CJ)
    {
        bool b1 = (int)CJ.highTwistLimit.limit == 0 && (int)CJ.lowTwistLimit.limit == 0;
        bool b2 = (int)CJ.swing1Limit.limit == 0 && (int)CJ.swing2Limit.limit == 0;

        return b1 && b2;
    }

    void ReorientateJoint(Transform joint1, Transform joint2)    //Reorientates Joint1 to Joint2, UP must always face next joint!
    {
        joint2.transform.parent = null;

        Vector3 up = joint1.parent ? joint1.parent.up : Vector3.up;
        Quaternion q1 = Quaternion.LookRotation(joint2.transform.position - joint1.transform.position, up);
        Quaternion q0 = Quaternion.Euler(0, -90f, 0);   //Adapt to different spaces
        joint1.transform.rotation = q1 * q0;
        joint2.transform.parent = joint1;
        /*
        Vector3 newFwd = Vector3.right;
        Vector3 newUp = -Vector3.up;
        Vector3 newRight = -Vector3.forward;
        Quaternion qrot = Quaternion.LookRotation(Vector3.right, -Vector3.up);

        joint2.transform.parent = null;
        FABRIKJoint JointInfo = joint1.GetComponent<FABRIKJoint>();
        Quaternion originalQuat = joint1.parent.rotation*qrot;
        Vector3 dir = Vector3.Normalize(joint2.position - joint1.position);

        Vector3 dirXZ = Vector3.ProjectOnPlane(dir, originalQuat * Vector3.up); //Dir projected in the plane with normal local-up
        float AngleX = Vector3.SignedAngle(originalQuat * Vector3.forward, dirXZ, originalQuat * Vector3.up);
        AngleX = Mathf.Clamp(AngleX, JointInfo.angle1, JointInfo.angle2);
        Quaternion q0 = Quaternion.AngleAxis(AngleX, qrot*Vector3.up);

        Vector3 dirYZ = Vector3.ProjectOnPlane(dir, q0 * originalQuat * Vector3.right); //Dir projected in the plane with normal local-up
        float AngleY = Vector3.SignedAngle(q0 * originalQuat * Vector3.forward, dirYZ, q0 * originalQuat * Vector3.up);
        AngleY = Mathf.Clamp(AngleY, JointInfo.angle3, JointInfo.angle4);
        Quaternion q1 = Quaternion.AngleAxis(AngleY, qrot*Vector3.right);

        joint1.rotation = joint1.parent.rotation * q0 * q1;

        joint2.transform.parent = joint1.transform;*/

    }

    void BackwardReach()
    {
        Joints[0].transform.position = InitialRootLocation;

        for (int i = 1; i < Joints.Count; i++)
        {
            /*RaycastHit hit;
            Vector3 dir = (Joints[i].transform.position - Joints[i - 1].transform.position);
            Ray ray = new Ray(Joints[i].transform.position, dir.normalized);
            if (Physics.Raycast(ray, out hit, dir.magnitude, layerMask))
                Joints[i].transform.position = hit.point + hit.normal * 0.02f;*/
            FABRIKJoint Fab = Joints[i - 1].GetComponent<FABRIKJoint>();
            Joints[i].transform.parent = null;
            ReorientateJoint(Joints[i - 1].transform, Joints[i].transform);
            Joints[i].transform.position = Joints[i - 1].transform.position + Joints[i-1].rotation*Fab.JointForward * Lengths[i - 1];
            Joints[i].transform.parent = Joints[i - 1].transform;
        }
    }

    void ForwardReach()
    {
        Debug.DrawLine(Joints[Joints.Count - 1].position, Objective.position, Color.red, 1f);
        Joints[Joints.Count - 1].transform.position = Objective.position;
        for (int i = Joints.Count - 1; i > 0; i--)
        {
            /*RaycastHit hit;
            Vector3 dir = (Joints[i].transform.position - Joints[i - 1].transform.position);
            Ray ray = new Ray(Joints[i - 1].transform.position, dir.normalized);
            if (Physics.Raycast(ray, out hit, dir.magnitude, layerMask))
                Joints[i].transform.position = hit.point + hit.normal * 0.02f;*/

            float r = Lengths[i - 1];
            float d = Vector3.Distance(Joints[i - 1].transform.position, Joints[i].transform.position);
            float lambda = r / d;
            Joints[i - 1].transform.position = Joints[i - 1].transform.position * lambda + Joints[i].transform.position * (1f - lambda);
        }
    }

    // Run at each physics simulation step
    void FixedUpdate()
    {
        if (Vector3.Distance(Objective.position, ObjectiveLastPosition) < 0.05f) return;
        ObjectiveLastPosition = Objective.position;

        if (Vector3.Distance(InitialRootLocation, Objective.position) > TotalLength)
        {
            Debug.Log("Target is unreachable.");
            for (int i = 1; i < Joints.Count; i++)
            {
                float r = Vector3.Distance(Joints[i].transform.position, Joints[i - 1].transform.position);
                float d = Vector3.Distance(Joints[i - 1].transform.position, Objective.transform.position);
                float lambda = r / d;

                Joints[i].transform.position = Joints[i - 1].transform.position * (1f - lambda) + Objective.position * lambda;
                ReorientateJoint(Joints[i - 1].transform, Joints[i].transform);
            }
        }
        else
        {
            Debug.Log("Target is reachable.");
            for (int c = 0; c < MaxIterations && Vector3.Distance(Joints[Joints.Count - 1].transform.position, Objective.position) > Threshold; c++)
            {
                ForwardReach();    //FORWARD PASS
                BackwardReach();   //BACKWARD PASS
            }
        }
    }
}
