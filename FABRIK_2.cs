/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FABRIK_2 : MonoBehaviour
{
    public List<Rigidbody> Joints;
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
        Joints = new List<Rigidbody>();
        Lengths = new List<float>();
        foreach (CharacterJoint t in transform.GetComponentsInChildren<CharacterJoint>())
        {
            Rigidbody rb = t.GetComponent<Rigidbody>();
            rb.isKinematic = true;
            Joints.Add(rb);
        }

        TotalLength = 0f;
        for (int i = 1; i < Joints.Count; i++)
        {
            Lengths.Add(Vector3.Distance(Joints[i].transform.position, Joints[i - 1].transform.position));
            TotalLength += Lengths[i - 1];
        }
        TotalLength += 0.25f;
        ObjectiveLastPosition = Objective.position;
        quatero = Joints[0].rotation;
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
        CharacterJoint CJ = joint1.GetComponent<CharacterJoint>();
        if (false && HasNoLimits(CJ))
        {
            Vector3 up = joint1.parent ? joint1.parent.up : Vector3.up;
            Quaternion q1 = Quaternion.LookRotation(joint2.transform.position - joint1.transform.position, up);
            Quaternion q0 = Quaternion.Euler(0, -90f, 0);   //Adapt to different spaces
            joint1.transform.rotation = q1*q0;
            return;
        }
        else
        {
            Quaternion originalQuat = joint1.parent ? joint1.parent.rotation : quatero;
            Vector3 dir = Vector3.Normalize(joint2.position - joint1.position);
            Vector3 newFwd = Vector3.right;
            Vector3 newUp = -Vector3.up;
            Vector3 newRight = -Vector3.forward;

            Vector3 dirXZ = Vector3.ProjectOnPlane(dir, originalQuat * newUp); //Dir projected in the plane with normal local-up
            float AngleX = Vector3.SignedAngle(originalQuat * newFwd, dirXZ, originalQuat * newUp);
            AngleX = Mathf.Clamp(AngleX, -360, 360);
            Quaternion q0 = Quaternion.AngleAxis(AngleX, newRight);

            Vector3 dirYZ = Vector3.ProjectOnPlane(dir, q0 * originalQuat * newRight); //Dir projected in the plane with normal local-up
            float AngleY = Vector3.SignedAngle(q0 * originalQuat * newFwd, dirYZ, q0 * originalQuat * newRight);
            AngleY = Mathf.Clamp(AngleY, -360, 360);
            Quaternion q1 = Quaternion.AngleAxis(AngleY, newUp);

            joint1.rotation = originalQuat * q0 * q1;
            Debug.Log("Angle X: "+ AngleX);
            Debug.Log("Angle Y: " + AngleY);
            //Debug.DrawRay(transform.position, dir * 10, Color.red, 2f);
            //Debug.DrawRay(transform.position, originalQuat * Vector3.forward * 10, Color.blue, 2f);
            //Debug.DrawRay(transform.position, transform.rotation*newFwd * 10, Color.blue, 2f);

        }
        joint2.transform.parent = joint1.transform;


    }

    void HingeConstraint(Rigidbody Hinge, Rigidbody Joint)
    {
        CharacterJoint Cj = Hinge.GetComponent<CharacterJoint>();
        if ((int)Cj.swing1Limit.limit == 0 && 0 == (int)Cj.swing2Limit.limit)
        {
            Vector3 JointProjected = Vector3.ProjectOnPlane(Joint.transform.position, Hinge.transform.forward);
            Vector3 HingeProjected = Vector3.ProjectOnPlane(Hinge.transform.position, Hinge.transform.forward);
            Vector3 LocalPos = JointProjected - HingeProjected;

            Joint.transform.position = Hinge.transform.position + LocalPos;
        }
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

        for (int i = 1; i < Joints.Count; i++)
        {
            RaycastHit hit;
            Vector3 dir = (Joints[i].transform.position - Joints[i - 1].transform.position);
            Ray ray = new Ray(Joints[i].transform.position, dir.normalized);
            if (Physics.Raycast(ray, out hit, dir.magnitude, layerMask))
                Joints[i].transform.position = hit.point + hit.normal * 0.02f;
            Joints[i].transform.parent = null;
            ReorientateJoint(Joints[i - 1].transform, Joints[i].transform);
            Joints[i].transform.position = Joints[i - 1].transform.position + Joints[i - 1].transform.right * Lengths[i-1];
            Joints[i].transform.parent = Joints[i-1].transform;

        }
    }

    void ForwardReach()
    {
        Joints[Joints.Count - 1].transform.position = Objective.position;
        for (int i = Joints.Count - 1; i > 0; i--)
        {
            RaycastHit hit;
            Vector3 dir = (Joints[i].transform.position - Joints[i - 1].transform.position);
            Ray ray = new Ray(Joints[i - 1].transform.position, dir.normalized);
            if (Physics.Raycast(ray, out hit, dir.magnitude, layerMask))
                Joints[i].transform.position = hit.point + hit.normal * 0.02f;

            float r = Lengths[i - 1];
            float d = Vector3.Distance(Joints[i - 1].transform.position, Joints[i].transform.position);
            float lambda = r/d;
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
                float r = Vector3.Distance(Joints[i].transform.position, Joints[i-1].transform.position);
                float d = Vector3.Distance(Joints[i-1].transform.position, Objective.transform.position);
                float lambda = r / d;

                Joints[i].transform.position = Joints[i-1].transform.position * (1f - lambda) + Objective.position * lambda;
                ReorientateJoint(Joints[i-1].transform, Joints[i].transform);
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
*/