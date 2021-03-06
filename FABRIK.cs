﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FABRIK : MonoBehaviour
{
    public List<Transform> Joints;
    public Transform[] UpperObjectives;
    public Transform[] LowerObjectives;
    public Vector3 InitialRootLocation;
    public float Threshold = 0.02f;
    public int MaxIterations = 10;
    private float TotalLength = 0f;
    public List<float> Lengths;         //length[i] = Distance(i,i+1)
    public LayerMask layerMask;
    public Vector3 TargetLastPosition;
    public Vector3 parentV;
    private Vector3 Target;
    private Vector3 OffsetUp;
    private Vector3 OffsetDown;
    Quaternion quatero;

    // Start is called before the first frame update
    void Start()
    {
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
            }
            Joints.Add(t.transform);
        }

        TotalLength = 0f;
        for (int i = 1; i < Joints.Count; i++)
        {
            Lengths.Add(Vector3.Distance(Joints[i].transform.position, Joints[i - 1].transform.position));
            TotalLength += Lengths[i - 1];
        }
        Target = (UpperObjectives[0].position + UpperObjectives[1].position) / 2f + OffsetUp;
        TargetLastPosition = Target;
        OffsetUp = Joints[Joints.Count - 1].position - Target;
        OffsetDown = Joints[0].position-((LowerObjectives[0].position + LowerObjectives[1].position) / 2f);
        InitialRootLocation = (LowerObjectives[0].position + LowerObjectives[1].position) / 2 + OffsetDown;
    }

    Vector2 GetClosestPoint(Vector2 pos1, float l1, float l2)
    {
        if (l1 > l2)
            return MathHelpers.ClosestPointEllipse(pos1, l1, l2);
        else
            return MathHelpers.ClosestPointEllipse(pos1, l2, l1);
    }

    /*
     * Constrains Joint to Socketball
     */
    void SocketBallConstraint(Transform SocketBall, Transform Joint, bool forward)
    {
        return;
        FABRIKJoint FJ = SocketBall.GetComponent<FABRIKJoint>();

        Vector3 LocalPos = Joint.transform.position - SocketBall.transform.position;
        Vector3 LocalUp = SocketBall.rotation * FJ.TwistAxis;
        Vector3 LocalRight = SocketBall.rotation * FJ.Axis;
        Vector3 LocalFwd = forward ? -Vector3.Cross(LocalUp, LocalRight) : Vector3.Cross(LocalUp, LocalRight);

        Vector3 newPos = Vector3.Project(LocalPos, LocalFwd);
        Vector3 O = SocketBall.transform.position + newPos;

        float l1 = newPos.magnitude * Mathf.Tan(Mathf.Deg2Rad * FJ.angle1);
        float l2 = newPos.magnitude * Mathf.Tan(Mathf.Deg2Rad * FJ.angle2);
        float l3 = newPos.magnitude * Mathf.Tan(Mathf.Deg2Rad * FJ.angle3);
        float l4 = newPos.magnitude * Mathf.Tan(Mathf.Deg2Rad * FJ.angle4);

        Vector3 Pos = Vector3.zero;
        Vector3 dir = Joint.transform.position - SocketBall.transform.position;

        Pos.x = Vector3.Dot(-LocalUp, dir);
        Pos.z = Vector3.Dot(LocalRight, dir);
        Joint.transform.position = Pos;
        Vector2 pos = GetQuadrantPosition(Joint.transform.position, l1, l2, l3, l4);
        Vector3 result = O;
        result -= LocalUp * pos.x;
        result += LocalRight * pos.y;
        Joint.transform.position = result;
    }

 /*
 * Constrains Child to Hinge
 */
    void HingeConstraint(Transform Hinge, Transform Child)
    {
        return;
        FABRIKJoint FJ = Hinge.GetComponent<FABRIKJoint>();
        Vector3 GlobalTwistAxis = FJ.transform.rotation * FJ.TwistAxis;
        Vector3 ProjectedPosition1 = Vector3.ProjectOnPlane(Child.position, GlobalTwistAxis);
        Vector3 ProjectedPosition2 = Vector3.ProjectOnPlane(Hinge.position, GlobalTwistAxis);
        Vector3 RelativePosition = ProjectedPosition1 - ProjectedPosition2;
        Child.position = Hinge.position + RelativePosition;
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

    /*
     * Forces joint1 to look at joint2
     */
    void ReorientateJoint(Transform joint1, Transform joint2)    //Reorientates Joint1 to Joint2, UP must always face next joint!
    {
        FABRIKJoint FJ = joint1.GetComponent<FABRIKJoint>();
        /*Vector3 Axis = FJ.Axis;
        Vector3 TwistAxis = FJ.Axis;
        Vector3 Forward = FJ.Forward;
        Vector3 originalUp = joint1 == Joints[0] ? parentV : joint1.parent.up;

        joint2.transform.parent = null;
        Vector3 dir = joint2.position - joint1.position;
        Quaternion q0 = Quaternion.LookRotation(dir, originalUp);
        Quaternion q = Quaternion.LookRotation(Forward);
        joint1.rotation = q0*Quaternion.Inverse(q);*/

        joint2.transform.parent = null;
        Quaternion originalQuat = joint1 == Joints[0] ? quatero : joint1.parent.rotation;
        Vector3 Axis = FJ.Axis;
        Vector3 TwistAxis = FJ.TwistAxis;
        Vector3 Forward = FJ.Forward;

        Vector3 dir = Vector3.Normalize(joint2.position - joint1.position);
        Vector3 dirXZ = Vector3.ProjectOnPlane(dir, originalQuat * TwistAxis); //Dir projected in the plane with twist axis
        float AngleX = Vector3.SignedAngle(originalQuat * Forward, dirXZ, originalQuat * TwistAxis);
        AngleX = Mathf.Clamp(AngleX, FJ.angle1, FJ.angle2);
        Quaternion q0 = Quaternion.AngleAxis(AngleX, TwistAxis);

        Vector3 dirYZ = Vector3.ProjectOnPlane(dir, q0 * originalQuat * Axis); //Dir projected in the plane with axis
        float AngleY = Vector3.SignedAngle(q0 * originalQuat * Forward, dirYZ, q0 * originalQuat * Axis);
        AngleY = Mathf.Clamp(AngleY, FJ.angle3, FJ.angle4);

        Quaternion q1 = Quaternion.AngleAxis(AngleY, Axis);

        joint1.rotation = originalQuat * q0 * q1;

        joint2.transform.parent = joint1;
    }

    void BackwardReach()
    {
        Joints[0].transform.position = InitialRootLocation;
        for (int i = 1; i < Joints.Count; i++)
        {
            FABRIKJoint Fab = Joints[i - 1].GetComponent<FABRIKJoint>();
            Joints[i].transform.parent = null;
            switch (Fab.RestrictionType)
            {
                case FABRIKJoint.JointType.Hinge:
                    HingeConstraint(Joints[i - 1], Joints[i]);
                    break;
                case FABRIKJoint.JointType.Socketball:
                    SocketBallConstraint(Joints[i - 1], Joints[i], false);
                    break;
            }
            Joints[i].transform.position = Joints[i - 1].transform.position + Joints[i - 1].rotation * Fab.Forward * Lengths[i - 1];
            ReorientateJoint(Joints[i - 1].transform, Joints[i].transform);
            Joints[i].transform.position = Joints[i - 1].transform.position + Joints[i - 1].rotation * Fab.Forward * Lengths[i - 1];

            Joints[i].transform.parent = Joints[i - 1].transform;
        }
    }

    void ForwardReach()
    {
        Joints[Joints.Count - 1].transform.position = Target;
        for (int i = Joints.Count - 1; i > 0; i--)
        {
            FABRIKJoint Fab = Joints[i-1].GetComponent<FABRIKJoint>();
            float r = Lengths[i - 1];
            float d = Vector3.Distance(Joints[i - 1].transform.position, Joints[i].transform.position);
            float lambda = r / d;
            Joints[i].transform.parent = null;
            Joints[i - 1].transform.position = Joints[i - 1].transform.position * lambda + Joints[i].transform.position * (1f - lambda);
            ReorientateJoint(Joints[i - 1], Joints[i]);
            Joints[i].transform.position = Joints[i - 1].transform.position + Joints[i - 1].rotation * Fab.Forward * Lengths[i - 1];
            Joints[i].transform.parent = Joints[i - 1];
        }
    }

    // Run at each physics simulation step
    void FixedUpdate()
    {
        parentV = Joints[0].up;
        quatero = Joints[0].rotation;
        Target = (UpperObjectives[0].position + UpperObjectives[1].position) / 2f + OffsetUp;
        TargetLastPosition = Target;
        if (Vector3.Distance(InitialRootLocation, Target) > TotalLength)
        {
            Debug.Log("Target is unreachable.");
            Joints[0].transform.position = InitialRootLocation;
            for (int i = 1; i < Joints.Count; i++)
            {
                float r = Vector3.Distance(Joints[i].transform.position, Joints[i - 1].transform.position);
                float d = Vector3.Distance(Joints[i - 1].transform.position, Target);
                float lambda = r / d;
                FABRIKJoint FJ = Joints[i - 1].GetComponent<FABRIKJoint>();
                Joints[i].transform.position = Joints[i - 1].transform.position * (1f - lambda) + Target * lambda;
                ReorientateJoint(Joints[i - 1].transform, Joints[i].transform);
                Joints[i].transform.position = Joints[i - 1].transform.position + Joints[i-1].rotation* FJ.Forward * Lengths[i - 1];
            }
        }
        else
        {
            Debug.Log("Target is reachable.");
            for (int c = 0; c < MaxIterations && Vector3.Distance(Joints[Joints.Count - 1].transform.position, Target) > Threshold; c++)
            {
                ForwardReach();    //FORWARD PASS
                BackwardReach();   //BACKWARD PASS
            }
        }
    }
}
