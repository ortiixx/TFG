using UnityEngine;

public class TwoBoneIK : MonoBehaviour
{
    public Transform Upper;//root of upper arm
    public Transform Lower;//root of lower arm
    public Transform End;//root of hand
    public Transform Target;//target position of hand
    public Transform Pole;//direction to bend towards 
    public float UpperElbowRotation;//Rotation offsetts
    public float LowerElbowRotation;
    public float drag;
    public float angularDrag;
    public float k1;
    public float k2;
    public bool debugRotation;
    private float a;//values for use in cos rule
    private float b;
    private float c;
    private Vector3 en;//Normal of plane we want our arm to be on
    private Rigidbody rb1;
    private Rigidbody rb2;

    private void Start()
    {
        rb1 = Upper.GetComponent<Rigidbody>();
        rb2 = Lower.GetComponent<Rigidbody>();
        Rigidbody rb3 = End.GetComponent<Rigidbody>();
        /*
        if (rb3)
        rb3.isKinematic = true;*/
    }

    void ApplyQuaternion(Quaternion quat, Rigidbody rb)
    {
        if (quat != Quaternion.identity)
        {
            Vector3 dir;
            float angle;
            CharacterJoint Cj = rb.GetComponent<CharacterJoint>();
            if (Cj)
                Cj.enableProjection = true;
            rb.isKinematic = false;
            quat.ToAngleAxis(out angle, out dir);
            rb.angularDrag = angularDrag;
            rb.drag = drag;
            //rb.detectCollisions = false;
            dir = Vector3.Project(dir, rb.transform.right);
            rb.AddTorque(new Vector3(quat.x,quat.y,quat.z) * k1, ForceMode.VelocityChange);
        }
    }

    void FollowOrientation(Quaternion Target, Rigidbody rb)
    {
        Quaternion q2 = Target*Quaternion.Inverse(rb.transform.rotation);
        ApplyQuaternion(q2, rb);
    }


    void Update()
    {
        a = Lower.localPosition.magnitude;
        b = End.localPosition.magnitude;
        c = Vector3.Distance(Upper.position, Target.position);
        en = Vector3.Cross(Target.position - Upper.position, Pole.position - Upper.position);

        Quaternion UpperTarget = Quaternion.identity;
        Quaternion LowerTarget = Quaternion.identity;

        //Set the rotation of the upper arm
        UpperTarget = Quaternion.LookRotation(Target.position - Upper.position, Quaternion.AngleAxis(UpperElbowRotation, Lower.position - Upper.position) * (en));
        UpperTarget *= Quaternion.Inverse(Quaternion.FromToRotation(Vector3.forward, Lower.localPosition));
        UpperTarget = Quaternion.AngleAxis(-CosAngle(a, c, b), -en) * UpperTarget;

        //set the rotation of the lower arm
        LowerTarget = Quaternion.LookRotation(Target.position - Lower.position, Quaternion.AngleAxis(LowerElbowRotation, End.position - Lower.position) * (en));
        LowerTarget *= Quaternion.Inverse(Quaternion.FromToRotation(Vector3.forward, End.localPosition));


        if (debugRotation)
        {
            Upper.rotation = UpperTarget;
            Lower.rotation = LowerTarget;
            rb1.isKinematic = true;
            rb2.isKinematic = true;
        }
        else
        {
            FollowOrientation(UpperTarget.normalized, rb1);
            FollowOrientation(LowerTarget.normalized, rb2);
        }
        //Lower.LookAt(Lower, Pole.position - Upper.position);
        //Lower.rotation = Quaternion.AngleAxis(CosAngle(a, b, c), en);
    }

    //function that finds angles using the cosine rule 
    float CosAngle(float a, float b, float c)
    {
        if (!float.IsNaN(Mathf.Acos((-(c * c) + (a * a) + (b * b)) / (-2 * a * b)) * Mathf.Rad2Deg))
        {
            return Mathf.Acos((-(c * c) + (a * a) + (b * b)) / (2 * a * b)) * Mathf.Rad2Deg;
        }
        else
        {
            return 1;
        }
    }
}