using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SocketBallTest : MonoBehaviour
{
    public Transform Child;
    public Vector3 TwistAxis;
    public Vector3 Axis;
    public Quaternion originalQuat;
    public Quaternion originalQuat2;
    public float Limit1X = 50f;
    public float Limit2X = 50f;
    public float Limit1Y = 50f;
    public float Limit2Y = 50f;
    Vector3 OriginalFwd;
    public Vector3 Forward;

    private void Start()
    {
        originalQuat = transform.rotation;
        Forward = Vector3.Cross(Axis, TwistAxis);
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


    void SocketBallConstraint(Transform SocketBall, Transform Joint)
    {
        Vector3 LocalPos = Joint.transform.position - SocketBall.transform.position;
        Vector3 LocalUp = transform.rotation * TwistAxis;
        Vector3 LocalRight = transform.rotation * Axis;
        Vector3 LocalFwd = Vector3.Cross(LocalUp, LocalRight);
        Debug.Log(LocalFwd);
        Vector3 newPos = Vector3.Project(LocalPos, LocalFwd);
        Vector3 O = SocketBall.transform.position + newPos;

        float l1 = newPos.magnitude * Mathf.Tan(Mathf.Deg2Rad * Limit1X);
        float l2 = newPos.magnitude * Mathf.Tan(Mathf.Deg2Rad * Limit2X);
        float l3 = newPos.magnitude * Mathf.Tan(Mathf.Deg2Rad * Limit1Y);
        float l4 = newPos.magnitude * Mathf.Tan(Mathf.Deg2Rad * Limit2Y);
        Debug.DrawLine(SocketBall.transform.position, O, Color.red, 30f);
        Debug.DrawLine(SocketBall.transform.position, O + -LocalUp * l1, Color.red, 30f); //l1 ynegaxis
        Debug.DrawLine(SocketBall.transform.position, O + LocalUp * l2, Color.blue, 30f); //l2 yposaxis
        Debug.DrawLine(SocketBall.transform.position, O + LocalRight * l3, Color.cyan, 30f); //l3 xposaxis
        Debug.DrawLine(SocketBall.transform.position, O + -LocalRight * l4, Color.black, 30f); //l4 xnegaxis
        Debug.DrawLine(Vector3.zero, Vector3.zero, Color.red, 30f);
        Debug.DrawLine(Vector3.zero, Vector3.zero + Vector3.right * l1, Color.red, 30f);
        Debug.DrawLine(Vector3.zero, Vector3.zero + -Vector3.right * l2, Color.blue, 30f);
        Debug.DrawLine(Vector3.zero, Vector3.zero + Vector3.forward * l3, Color.cyan, 30f);
        Debug.DrawLine(Vector3.zero, Vector3.zero + -Vector3.forward * l4, Color.black, 30f);
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



    private void Update()
    {
        SocketBallConstraint(this.transform, Child);
    }
}
