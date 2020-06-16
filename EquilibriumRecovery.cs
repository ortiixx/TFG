using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ELocomotionState
{
    RecoverFoot,
    KeepBalancing,
    Idle
};
public class EquilibriumRecovery : MonoBehaviour
{
    public float margin = 3f;
    public float ordl;
    public float ordh;
    public float hf;
    public float force = 100f;
    public float WavingArmsForce = 40f;
    public float spineControl = -1.5f;
    public float CMControl = -1.5f;
    public float MaxStepSpeed = 4f;
    public float StepDuration = 5f;
    public float stepCheckDistance = 0.25f;
    public float CMAngularControl;
    public float CMControlForce;
    public float mass;
    public Transform LeftArm;
    public Transform RightArm;
    public Transform LeftEffector;
    public Transform RightEffector;
    public Transform LeftFoot;
    public Transform RightFoot;
    public Transform Spine;
    public Transform DesiredStep;
    public Vector3 SpineLocalUp;
    public Vector3 CMLocalUp;
    public LayerMask lm;
    public float ImpulseForce = 300f;

    private Rigidbody SpineRB;
    private Rigidbody CM;
    private Rigidbody LeftArmRB;
    private Rigidbody RightArmRB;
    private Vector3 RightFootRelativePos;
    private Vector3 SpineOriginalUp;
    private Vector3 CMOriginalUp;
    private bool CanMoveFoot = true;
    private float effectorDistanceEpsilon=.05f;
    private float CMDistanceEpsilon=.05f;
    private Vector3 InitialLeftFootPos;
    private Vector3 InitialRightFootPos;
    private Quaternion OriginalRightFootRotation;
    private Quaternion OriginalLeftFootRotation;
    private Vector3 OriginalLeftFootRelativeCM;
    private Vector3 OriginalRightFootRelativeCM;
    private BoneDriver Driver;
    private bool lastStepRight = false;
    private Vector3 CP;
    private Vector3 pos;
    private Quaternion LeftFootAerial;
    private Quaternion RightFootAerial;
    private bool RecoveryDone = false;
    private ELocomotionState LocomotionState;
    private float desiredCMHeight;

    // Start is called before the first frame update
    void Start()
    {
        RightArmRB = RightArm.GetComponent<Rigidbody>();
        LeftArmRB = LeftArm.GetComponent<Rigidbody>();
        Driver = gameObject.GetComponent<BoneDriver>();
        CM = gameObject.GetComponent<Rigidbody>();

        if (!Driver)
            Debug.LogError("Could not find the bonedriver component!");
        if (Spine)
            SpineRB = Spine.GetComponent<Rigidbody>();
        SpineOriginalUp = Spine.rotation * SpineLocalUp;
        CMOriginalUp = CM.rotation * CMLocalUp;
        OriginalLeftFootRotation = LeftFoot.transform.rotation;
        OriginalRightFootRotation = RightFoot.transform.rotation;

        LeftFootAerial = new Quaternion(0.04686619f, -0.02704594f, -0.323909f, 0.9444999f);
        RightFootAerial = new Quaternion(-0.01521215f, 0.01137009f, -0.8709798f, -0.4910399f);
        RightFootRelativePos = LeftFoot.position - RightFoot.position;
        LocomotionState = ELocomotionState.Idle;
        desiredCMHeight = CM.position.y;

        OriginalLeftFootRelativeCM = CM.position - LeftFoot.position;
        OriginalRightFootRelativeCM = CM.position - RightFoot.position;
    }

    Vector3 GetDesiredPS(float dl, float dh)
    {
        Vector3 res = Vector3.zero;
        Vector3 angularMomentum = new Vector3(
        CM.inertiaTensor.x * CM.angularVelocity.x,
        CM.inertiaTensor.y * CM.angularVelocity.y,
        CM.inertiaTensor.z * CM.angularVelocity.z
        );
        if (CM)
        {
            res.y = LeftEffector.position.y;
            res.x = CM.position.x + CM.position.y * ((dl * CM.velocity.x * mass) / GroundReactionForce()) + ((dh * angularMomentum.z) / GroundReactionForce());
            res.z = CM.position.z + CM.position.y * ((dl * CM.velocity.z * mass) / GroundReactionForce()) - ((dh * angularMomentum.x) / GroundReactionForce());
        }
        Debug.DrawLine(res, res - Vector3.up, Color.yellow);
        return res;
    }

    float GroundReactionForce()
    {
        return CM.velocity.y + mass * Physics.gravity.magnitude;
    }

    public static Vector3 Bezier3(Vector3 s, Vector3 p1, Vector3 p2, Vector3 e, float t)
    {
        t = Mathf.Clamp(t, .0f, 1.0f);
        float rt = 1 - t;
        return rt * rt * rt * s + 3 * rt * rt * t * p1 + 3 * rt * t * t * p2 + t * t * t * e;
    }

    bool CheckIsRightFoot()
    {
        return Vector3.Distance(RightFoot.position, DesiredStep.position) < Vector3.Distance(LeftFoot.position, DesiredStep.position);
    }

    IEnumerator PerformStep(bool isRightFoot)
    {
        Vector3 velo = CM.velocity;
        float ControlPointHeight = .4f;
        float SimulationTime = .0f;
        Transform Foot = isRightFoot ? RightFoot : LeftFoot;
        Transform Effector = isRightFoot ? RightEffector : LeftEffector;
        Vector3 MidPoint = isRightFoot ? LeftFoot.position : RightFoot.position;    //We want the midpoint between the other foot and the destination
        Vector3 InitialFootLocation = Foot.position;
        Vector3 FootControlPoint1 = (DesiredStep.position - Foot.position) / 5f; FootControlPoint1 += Foot.position; FootControlPoint1 += Vector3.up * ControlPointHeight;
        Vector3 FootControlPoint2 = FootControlPoint1; FootControlPoint2.y = Foot.position.y;
        Vector3 vec = DesiredStep.position;
        Quaternion AerialPhase = isRightFoot ? RightFootAerial : LeftFootAerial;
        Quaternion GroundPhase = isRightFoot ? OriginalRightFootRotation : OriginalLeftFootRotation;
        MidPoint = (MidPoint + DesiredStep.position) / 2f;
        MidPoint.y += (transform.position - MidPoint).y;
        CanMoveFoot = false;
        vec.y = Effector.position.y;
        DesiredStep.position = vec;
        while (Vector3.Distance(Effector.position, DesiredStep.position)>effectorDistanceEpsilon)// && Vector3.Distance(CM.position, MidPoint)>CMDistanceEpsilon)
        {
            float t = Vector3.Distance(Foot.position, DesiredStep.position) / Vector3.Distance(InitialFootLocation, DesiredStep.position);
            if (t<0.5f)
                Foot.rotation = Quaternion.Slerp(GroundPhase, AerialPhase, t);
            else
                Foot.rotation = Quaternion.Slerp(AerialPhase, GroundPhase,  t);

            if(Vector3.Distance(Effector.position, DesiredStep.position) < effectorDistanceEpsilon)
            {
                Effector.position = DesiredStep.position;
            }
            else
            {
                Vector3 pos = Bezier3(InitialFootLocation, FootControlPoint1, FootControlPoint2, DesiredStep.position, SimulationTime*MaxStepSpeed);
                Effector.position = pos;
                //Speed = MathHelpers.SampleGaussian(SimulationTime, MaxStepSpeed, StepDuration/2f, StepDuration);
            }
            CM.AddForce((MidPoint - transform.position) * CMControlForce*MaxStepSpeed*2.1f, ForceMode.Acceleration);
            Debug.DrawLine(FootControlPoint1, (Foot.position + DesiredStep.position) / 2f, Color.red);
            Debug.DrawLine(transform.position, MidPoint);
            SimulationTime += Time.deltaTime;
            yield return null;
        }
        vec = Effector.position;
        vec.y = DesiredStep.position.y;
        Effector.position = vec;
        Foot.rotation = GroundPhase;
        yield return new WaitForSecondsRealtime(0.2f);
        CanMoveFoot = true;
        lastStepRight = isRightFoot;
    }

    void MaintainSpineBalance()
    {
        float Epsilon = 90f;
        Vector3 Center = (LeftFoot.position + RightFoot.position) / 2f;
        Vector3 v1 = (Center - LeftFoot.position).normalized;
        Vector3 v2 = (Center - CM.position).normalized;
        Vector3 Dir = Vector3.Cross(v2, v1);
        Dir.y = CM.transform.up.y; Dir.Normalize();
        Debug.DrawRay(CM.position, Dir, Color.green);
        Debug.DrawRay(CM.position, CM.transform.up, Color.green);
        Quaternion q1 = Quaternion.FromToRotation(Spine.rotation*SpineLocalUp, SpineOriginalUp);
        Quaternion q2 = Quaternion.FromToRotation(CM.rotation * CMLocalUp, CMOriginalUp);
        Quaternion q3 = Quaternion.FromToRotation(CM.transform.up, Dir);
       // if(Vector3.Angle(CM.transform.up, Dir)>Epsilon)
       //     q2 *= q3;
        if (SpineRB)
        {

            Vector3 Torque = Vector3.zero;
            Torque.x = q1.x;
            Torque.y = 0;
            Torque.z = q1.z;
            SpineRB.AddTorque(Torque * spineControl);
            float k = Torque.magnitude;
            Torque.x = 1f;
            Torque.y = 1f;
            Torque.z = 0f;
            LeftArmRB.AddRelativeTorque(Torque * WavingArmsForce*k, ForceMode.VelocityChange);
            RightArmRB.AddRelativeTorque(Torque * WavingArmsForce*k, ForceMode.VelocityChange);
            LeftArmRB.drag = 5f;
            LeftArmRB.angularDrag = 20f;
            LeftArmRB.GetComponentInChildren<Rigidbody>().drag = 10f;
            LeftArmRB.GetComponentInChildren<Rigidbody>().angularDrag = 20f;
            RightArmRB.GetComponentInChildren<Rigidbody>().drag = 10f;
            RightArmRB.GetComponentInChildren<Rigidbody>().angularDrag = 20f;
            RightArmRB.drag = 5f;
            RightArmRB.angularDrag = 20f;
            LeftArmRB.GetComponentInChildren<Rigidbody>().AddRelativeTorque(Torque * WavingArmsForce * k, ForceMode.VelocityChange);
            RightArmRB.GetComponentInChildren<Rigidbody>().AddRelativeTorque(Torque * WavingArmsForce * k, ForceMode.VelocityChange);
        }
        if (CM)
        {
            Vector3 Torque = Vector3.zero;
            Torque.x = q2.x;
            Torque.y = 0;
            Torque.z = q2.z;
            CM.AddTorque(Torque * CMControl);
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(25, 285, 100, 30), "Max Step speed");
        MaxStepSpeed = GUI.HorizontalSlider(new Rect(25, 300, 100, 30), MaxStepSpeed, 0.0F, 20.0F);
    }

    void CalculateCP()
    {
        RaycastHit Hit;

    }

    private void AddFootForces()
    {
        Vector3 d1 = (transform.position - LeftFoot.position).normalized;
        Vector3 d2 = (transform.position - RightFoot.position).normalized;
        Ray r1 = new Ray(LeftFoot.position, Vector3.down);
        Ray r2 = new Ray(RightFoot.position, Vector3.down);
        RaycastHit hit1, hit2;
        Vector3 direction = Vector3.up;
        Vector3 P1 = OriginalLeftFootRelativeCM + LeftFoot.position;
        Vector3 P2 = OriginalRightFootRelativeCM + RightFoot.position;
        Vector3 OriginalCM = (P1 + P2) / 2f;
        if (Physics.Raycast(r1, out hit1, stepCheckDistance, lm))
        {
            float magnitude = Vector3.Dot(d1, Vector3.up)  * force;
            CM.AddForce(direction * magnitude, ForceMode.VelocityChange);
            CM.AddForce((P1 - CM.position) * force, ForceMode.VelocityChange);
        }
        if (Physics.Raycast(r2, out hit2, stepCheckDistance, lm))
        {
            float magnitude = Vector3.Dot(d2, Vector3.up) * force;
            CM.AddForce(direction*magnitude, ForceMode.VelocityChange);
            CM.AddForce((P2 - CM.position) * magnitude, ForceMode.VelocityChange);
        }
    }

    private void CheckBalance()
    {
        if (Driver.GetBalanceFactor() < 0.05f)
        {
            Driver.SetUnbalanced();
            TwoBoneIK[] IK = gameObject.GetComponentsInChildren<TwoBoneIK>();
            foreach (TwoBoneIK ik in IK)
            {
                ik.enabled = false;
            }
            this.enabled = false;
        }
    }

    private void LocomotionIdleState()
    {
        if (!Driver.CheckCP(GetDesiredPS(ordl, ordh)))
        {
            LocomotionState = ELocomotionState.KeepBalancing;
            Vector3 GoalPos = GetDesiredPS(ordl * hf, ordh * hf);
            DesiredStep.position = GoalPos;
            StartCoroutine("PerformStep", CheckIsRightFoot());
        }
    }

    private void KeepBalancingState()
    {
        if(!Driver.CheckCP(GetDesiredPS(ordl, ordh)))
        {
            Vector3 GoalPos = GetDesiredPS(ordl * hf, ordh * hf);
            DesiredStep.position = GoalPos;
            StartCoroutine("PerformStep", CheckIsRightFoot());
        }
        else
        {
            LocomotionState = ELocomotionState.RecoverFoot;
        }
    }

    private void RecoverOtherFootState()
    {
        DesiredStep.position = lastStepRight ? RightFoot.position + RightFootRelativePos : LeftFoot.position - RightFootRelativePos;
        StartCoroutine("PerformStep", !lastStepRight);
        LocomotionState = ELocomotionState.Idle;
    }

    private void LocomotionStateMachine()
    {
        if (!CanMoveFoot)
            return;
        switch (LocomotionState)
        {
            case ELocomotionState.Idle:
                Debug.Log("IdleState");
                LocomotionIdleState();
                break;
            case ELocomotionState.KeepBalancing:
                Debug.Log("Balancing State");
                KeepBalancingState();
                break;
            case ELocomotionState.RecoverFoot:
                Debug.Log("Recover foot state");
                RecoverOtherFootState();
                break;
            default:
                break;
        }
    }

    private void FixedUpdate()
    {
        if (Input.GetButtonDown("Fire2"))
        {
            CM.AddForce(CM.transform.up * ImpulseForce, ForceMode.Impulse);
        }
        //Driver.AddDrag();
        if (CanMoveFoot && Input.GetButtonDown("Fire1"))
        {
            StartCoroutine("PerformStep", CheckIsRightFoot());
        }
        MaintainSpineBalance();
        CheckBalance();
        AddFootForces();
        LocomotionStateMachine();
    }
}
