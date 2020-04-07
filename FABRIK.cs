﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FABRIK : MonoBehaviour
{
    public List<FABRIKJoint> Joints;
    public Transform Objective;
    public Vector3 InitialRootLocation;
    public float Threshold = 0.24f;
    public int MaxIterations = 10;
    private float TotalLength = 0f;
    private List<float> Lengths;         //length[i] = Distance(i,i+1)
    public LayerMask layerMask;
    public Vector3 ObjectiveLastPosition;

    // Start is called before the first frame update
    void Start()
    {
        InitialRootLocation = transform.position;
        Joints = new List<FABRIKJoint>();
        Lengths = new List<float>();
        foreach(FABRIKJoint t in transform.GetComponentsInChildren<FABRIKJoint>())
                Joints.Add(t);

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

    void ReorientateJoint(FABRIKJoint joint1, FABRIKJoint joint2)    //Reorientates Joint1 to Joint2, UP must always face next joint!
    {
        Vector3 dir = (joint2.transform.position - joint1.transform.position).normalized;
        Vector3 fwd = joint1.JointConstraintType == FABRIKJoint.JointType.Hinge ? joint1.OriginalForward : joint1.transform.forward;
        Quaternion rotation = Quaternion.LookRotation(Vector3.Cross(joint1.transform.right, dir), dir);
        joint2.transform.parent = null;
        joint1.transform.Rotate(Vector3.up * 90f);
        joint1.transform.LookAt(joint2.transform.position, joint2.transform.up);
        joint1.transform.Rotate(Vector3.up * -90f);
        joint2.transform.parent = joint1.transform;
    }

    void HingeConstraint(FABRIKJoint Hinge, FABRIKJoint Joint)
    {
        if (Hinge.JointConstraintType != FABRIKJoint.JointType.Hinge)
            return;

        Vector3 JointProjected = Vector3.ProjectOnPlane(Joint.transform.position, Hinge.transform.forward);
        Vector3 HingeProjected = Vector3.ProjectOnPlane(Hinge.transform.position, Hinge.transform.forward);
        Vector3 LocalPos = JointProjected - HingeProjected;

        Joint.transform.position = Hinge.transform.position + LocalPos;
    }

    void SocketBallConstraint(FABRIKJoint SocketBall, FABRIKJoint Joint)
    {
        if (SocketBall.JointConstraintType != FABRIKJoint.JointType.SocketBall)
            return;
        Vector3 LocalPos = Joint.transform.position - SocketBall.transform.position;
        Vector3 newPos = Vector3.Project(LocalPos, SocketBall.transform.right);
        Vector3 O = SocketBall.transform.position + newPos;

        float l1 = newPos.magnitude * Mathf.Tan(Mathf.Deg2Rad * SocketBall.angle1);
        float l2 = newPos.magnitude * Mathf.Tan(Mathf.Deg2Rad * SocketBall.angle2);
        float l3 = newPos.magnitude * Mathf.Tan(Mathf.Deg2Rad * SocketBall.angle3);
        float l4 = newPos.magnitude * Mathf.Tan(Mathf.Deg2Rad * SocketBall.angle4);
        Debug.DrawLine(SocketBall.transform.position, O, Color.red, 30f);
        Debug.DrawLine(SocketBall.transform.position, O + -SocketBall.transform.up * l1, Color.red, 30f); //l1 ynegaxis
        Debug.DrawLine(SocketBall.transform.position, O + SocketBall.transform.up * l2, Color.blue, 30f); //l2 yposaxis
        Debug.DrawLine(SocketBall.transform.position, O + SocketBall.transform.forward * l3, Color.cyan, 30f); //l3 xposaxis
        Debug.DrawLine(SocketBall.transform.position, O + -SocketBall.transform.forward * l4, Color.black, 30f); //l4 xnegaxis
        Debug.DrawLine(Vector3.zero, Vector3.zero, Color.red, 30f);
        Debug.DrawLine(Vector3.zero, Vector3.zero + Vector3.right * l1, Color.red, 30f);
        Debug.DrawLine(Vector3.zero, Vector3.zero + -Vector3.right * l2, Color.blue, 30f);
        Debug.DrawLine(Vector3.zero, Vector3.zero + Vector3.forward * l3, Color.cyan, 30f);
        Debug.DrawLine(Vector3.zero, Vector3.zero + -Vector3.forward * l4, Color.black, 30f);
        Vector3 Pos = Vector3.zero;
        Vector3 dir = Joint.transform.position - SocketBall.transform.position;
        Pos.x = Vector3.Dot(-SocketBall.transform.up, dir);
        Pos.z = Vector3.Dot(SocketBall.transform.forward, dir);
        Joint.transform.position = Pos;
        Vector2 pos = GetQuadrantPosition(Joint.transform.position, l1, l2, l3, l4);
        Vector3 result = O;
        result -= SocketBall.transform.up * pos.x;
        result += SocketBall.transform.forward * pos.y;
        Joint.transform.position = result;
    }

    void BackwardReach()
    {
        Joints[0].transform.position = InitialRootLocation;
        ReorientateJoint(Joints[0], Joints[1]);

        for (int i = 1; i < Joints.Count; i++)
        {
            HingeConstraint(Joints[i - 1], Joints[i]);
            //SocketBallConstraint(Joints[i - 1], Joints[i]);
            RaycastHit hit;
            Vector3 dir = (Joints[i].transform.position - Joints[i - 1].transform.position);
            Ray ray = new Ray(Joints[i].transform.position, dir.normalized);
            if (Physics.Raycast(ray, out hit, dir.magnitude, layerMask))
                Joints[i].transform.position = hit.point + hit.normal * 0.02f;

            float r = Lengths[i - 1];
            float d = Vector3.Distance(Joints[i - 1].transform.position, Joints[i].transform.position);
            float lambda = r/d;
            Joints[i].transform.position = Joints[i - 1].transform.position * (1f - lambda) + Joints[i].transform.position * lambda;
            ReorientateJoint(Joints[i - 1], Joints[i]);
        }
    }

    void ForwardReach()
    {
        Joints[Joints.Count - 1].transform.position = Objective.position;
        for (int i = Joints.Count - 1; i > 0; i--)
        {
            Joints[i - 1].transform.parent = null;
            HingeConstraint(Joints[i], Joints[i - 1]);
            //SocketBallConstraint(Joints[i], Joints[i-1]);
            RaycastHit hit;
            Vector3 dir = (Joints[i].transform.position - Joints[i - 1].transform.position);
            Ray ray = new Ray(Joints[i - 1].transform.position, dir.normalized);
            if (Physics.Raycast(ray, out hit, dir.magnitude, layerMask))
                Joints[i].transform.position = hit.point + hit.normal * 0.02f;

            float r = Lengths[i - 1];
            float d = Vector3.Distance(Joints[i - 1].transform.position, Joints[i].transform.position);
            float lambda = r/d;
            Joints[i - 1].transform.position = Joints[i - 1].transform.position * lambda + Joints[i].transform.position * (1f - lambda);
            ReorientateJoint(Joints[i-1], Joints[i]);
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
                float r = Vector3.Distance(Joints[i].transform.position, Joints[i-1].transform.position);
                float d = Vector3.Distance(Joints[i-1].transform.position, Objective.transform.position);
                float lambda = r / d;

                Joints[i].transform.position = Joints[i-1].transform.position * (1f - lambda) + Objective.position * lambda;
                ReorientateJoint(Joints[i-1], Joints[i]);
            }
        }
        else
        {
            Debug.Log("Target is reachable.");
            for (int c = 0; c < MaxIterations && Vector3.Distance(Joints[Joints.Count - 1].transform.position, Objective.position) > Threshold; c++){
                ForwardReach();    //FORWARD PASS
                BackwardReach();   //BACKWARD PASS
            }
        }
    }
}
