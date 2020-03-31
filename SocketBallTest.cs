using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SocketBallTest : MonoBehaviour
{
    public GameObject Child;
    public float angle1;
    public float angle2;
    public float angle3;
    public float angle4;

    // Start is called before the first frame update
    void Start()
    {
        Vector3 LocalPos = Child.transform.position - transform.position;
        Vector3 newPos = Vector3.Project(LocalPos, transform.up);
        Vector3 O = transform.position + newPos;

        float l1 = newPos.magnitude * Mathf.Tan(Mathf.Deg2Rad*angle1);
        float l2 = newPos.magnitude * Mathf.Tan(Mathf.Deg2Rad*angle2);
        float l3 = newPos.magnitude * Mathf.Tan(Mathf.Deg2Rad*angle3);
        float l4 = newPos.magnitude * Mathf.Tan(Mathf.Deg2Rad*angle4);
        Debug.DrawLine(transform.position, O, Color.red, 30f);
        Debug.DrawLine(transform.position, O + -transform.right * l1, Color.red, 30f); //l1
        Debug.DrawLine(transform.position, O + transform.right * l2, Color.blue, 30f); //l2
        Debug.DrawLine(transform.position, O + transform.forward * l3, Color.cyan, 30f); //l3
        Debug.DrawLine(transform.position, O + -transform.forward * l4, Color.black, 30f); //l4
        Debug.DrawLine(Vector3.zero, Vector3.zero, Color.red, 30f);
        Debug.DrawLine(Vector3.zero, Vector3.zero + Vector3.left * l1, Color.red, 30f);
        Debug.DrawLine(Vector3.zero, Vector3.zero + -Vector3.left * l2, Color.blue, 30f);
        Debug.DrawLine(Vector3.zero, Vector3.zero + Vector3.forward * l3, Color.cyan, 30f);
        Debug.DrawLine(Vector3.zero, Vector3.zero + -Vector3.forward * l4, Color.black, 30f);
        Vector3 Pos = Vector3.zero;
        Vector3 dir = Child.transform.position - transform.position;
        Pos.x = Vector3.Dot(transform.right, dir);
        Pos.z = Vector3.Dot(transform.forward, dir);
        Child.transform.position = Pos;

        Vector2 pos = GetQuadrantPosition(Child.transform.position, l1 ,l2 ,l3 ,l4);
        Vector3 result = O;
        result += transform.right*pos.x;
        result += transform.forward*pos.y;
        Child.transform.position = result;

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
        Vector2 Pos = new Vector2(Orpos.x, Orpos.y);

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
            Pos =  GetClosestPoint(Pos, l1, l4);
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
