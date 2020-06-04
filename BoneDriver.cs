using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;


public class BoneDriver : MonoBehaviour
{

    struct BodyPart
    {
        public float weight;
        public Transform followTransform;
        public Quaternion lastOrientation;
        public float drag;
        public float angularDrag;
    }

    public Transform animationsRoot;
    public Transform LeftFoot;
    public Transform RightFoot;
    public Transform Chest;
    public Transform Head;
    public Vector3 GroundNormal = Vector3.up;
    public float FootLength;
    public float FootVerticalOffset;
    public float FootHorizontalOffset;
    public float k1 = 2f;
    public float k2 = 1f;
    public float angularDrag = 10f;
    public float Drag = 20f;
    IDictionary<Transform, BodyPart> BodyMapper;
    Animator AnimController;

    void Mapping()
    {
        Transform[] bonesAnimations = animationsRoot.GetComponentsInChildren<Transform>(true);
        Transform[] bonesRagdoll = transform.GetComponentsInChildren<Transform>(true);
        BodyMapper = new Dictionary<Transform, BodyPart>();
        var result = bonesRagdoll.Zip(bonesAnimations, (first, second) => new Tuple<Transform, Transform>(first, second));
        foreach (Tuple<Transform, Transform> t in result)
        {
            Rigidbody rb = t.Item2.GetComponent<Rigidbody>();
            BodyPart b = new BodyPart();
            b.weight = 1.0f;
            b.followTransform = t.Item2;
            b.lastOrientation = t.Item2.rotation;
            if (rb)
            {
                b.drag = rb.drag;
                b.angularDrag = rb.angularDrag;
            }
            BodyMapper[t.Item1] = b;
        }
    }

    void Start()
    {
        Mapping();
        AnimController = animationsRoot.root.GetComponent<Animator>();
        if (!AnimController)
            Debug.LogError("Animations root has not an animator component in its parent!!");
    }

    Vector3 ComputeDesiredAngularSpeed(Quaternion start, Quaternion end)
    {
        Quaternion deltaRotation = end * Quaternion.Inverse(start);
        if (deltaRotation == Quaternion.identity)
            return Vector3.zero;
        Vector3 dir; float angle;
        MathHelpers.Quat2VecAngle(deltaRotation, out dir, out angle);
        Vector3 eulerRot = new Vector3(Mathf.DeltaAngle(0, deltaRotation.eulerAngles.x), Mathf.DeltaAngle(0, deltaRotation.eulerAngles.y), Mathf.DeltaAngle(0, deltaRotation.eulerAngles.z));
        return eulerRot / Time.fixedDeltaTime;
    }


    void FollowBone(Rigidbody rb, Transform AnimBone, Quaternion lastOrientation)
    {
        Vector3 dir;
        float angle;
        Vector3 TargetAngularSpeed = ComputeDesiredAngularSpeed(lastOrientation, AnimBone.rotation);
        Quaternion q2 = lastOrientation * Quaternion.Inverse(rb.rotation);
        if (q2 != Quaternion.identity)
        {
            CharacterJoint Cj = rb.GetComponent<CharacterJoint>();
            if (Cj)
                Cj.enableProjection = true;
            rb.isKinematic = false;
            MathHelpers.Quat2VecAngle(q2, out dir, out angle);
            rb.angularDrag = angularDrag;
            rb.drag = Drag;
            //rb.detectCollisions = false;
            Vector3 Torque = new Vector3(q2.x, q2.y, q2.z);
            rb.AddTorque(Torque* k1 - k2 * rb.angularVelocity, ForceMode.VelocityChange);
        }
    }

    //TODO: Make drags equal to original
    public void SetUnbalanced()
    {
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        if (!rb)
            Debug.LogError("Root has not a ribidbody!");
        else
        {
            rb.constraints = RigidbodyConstraints.None;
            k1 = 0;
            foreach (KeyValuePair<Transform, BodyPart> k in BodyMapper.ToList())
            {
                Rigidbody rb2 = k.Key.GetComponent<Rigidbody>();
                if (!rb2) continue;
                rb2.drag = k.Value.drag;
                rb2.angularDrag = k.Value.angularDrag;
            }
        }
    }

    float CheckPointRatio(Vector3 bp1, Vector3 bp2, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        Vector2 segmentInter = Vector2.zero;
        float totalLength = Vector3.Distance(bp1,bp2);
        float intersectionLength = 0.0f;
        List<Vector2> segments = new List<Vector2>();
        if (MathHelpers.SegmentIntersection(bp1, bp2, p1, p2, out segmentInter)) segments.Add(new Vector2(segmentInter.x,segmentInter.y));
        if (MathHelpers.SegmentIntersection(bp1, bp2, p2, p3, out segmentInter)) segments.Add(new Vector2(segmentInter.x, segmentInter.y));
        if (MathHelpers.SegmentIntersection(bp1, bp2, p3, p4, out segmentInter)) segments.Add(new Vector2(segmentInter.x, segmentInter.y));
        if (MathHelpers.SegmentIntersection(bp1, bp2, p4, p1, out segmentInter)) segments.Add(new Vector2(segmentInter.x, segmentInter.y));

        if (segments.Count == 2)
        {
            intersectionLength = Vector2.Distance(segments[0], segments[1]);
        }
        else
        {
            Vector2 bp12d = new Vector2(bp1.x, bp1.z);
            Vector2 bp22d = new Vector2(bp2.x, bp2.z);
            if (MathHelpers.CheckPointInQuad(bp1, p1, p2, p3, p4))
                intersectionLength = Vector2.Distance(bp12d, segments[0]);
            else
                intersectionLength = Vector2.Distance(bp22d, segments[0]);
        }
        return intersectionLength / totalLength;
    }

    public float GetBalanceFactor()
    {
        float counterPos = 0;
        float counterTot = 0;
        Vector3 p1 = Vector3.ProjectOnPlane(LeftFoot.transform.position + LeftFoot.transform.up * FootVerticalOffset - LeftFoot.transform.forward * FootHorizontalOffset, Vector3.up);
        Vector3 p2 = Vector3.ProjectOnPlane(RightFoot.transform.position - RightFoot.transform.up * FootVerticalOffset + LeftFoot.transform.forward * FootHorizontalOffset, Vector3.up);
        Vector3 p3 = Vector3.ProjectOnPlane(p2 + RightFoot.transform.up * FootLength, Vector3.up);
        Vector3 p4 = Vector3.ProjectOnPlane(p1 - LeftFoot.transform.up * FootLength, Vector3.up);

        foreach (KeyValuePair<Transform, BodyPart> k in BodyMapper.ToList())
        {
            FABRIKJoint FB = k.Key.GetComponent<FABRIKJoint>();
            Collider c = k.Key.GetComponent<Collider>();
            Rigidbody rb = k.Key.GetComponent<Rigidbody>();
            if (FB && c && rb)
            {
                counterTot += rb.mass;
                Vector3 bp1 = Vector3.ProjectOnPlane(FB.transform.position, Vector3.up);
                Vector3 bp2 = Vector3.ProjectOnPlane(FB.transform.position + FB.transform.rotation * FB.Forward * c.bounds.size.y, Vector3.up);
                Debug.DrawLine(FB.transform.position, FB.transform.position + FB.transform.rotation * FB.Forward * c.bounds.size.y, Color.green);
                Debug.DrawLine(Vector3.ProjectOnPlane(FB.transform.position, Vector3.up), Vector3.ProjectOnPlane(FB.transform.position + FB.transform.rotation * FB.Forward * c.bounds.size.y, Vector3.up), Color.red);
                bool b1 = MathHelpers.CheckPointInQuad(bp1, p1, p2, p3, p4);
                bool b2 = MathHelpers.CheckPointInQuad(bp2, p1, p2, p3, p4);
                if (b1 && b2)
                {
                    Debug.DrawLine(bp1, bp2, Color.green);
                    counterPos += rb.mass;
                }
                else if (b1 != b2)
                {
                    Debug.DrawLine(bp1, bp2, Color.yellow);
                    counterPos += rb.mass*CheckPointRatio(bp1,bp2,p1,p2,p3,p4);
                }
                else
                {
                    Debug.DrawLine(bp1, bp2, Color.red);
                }
            }
        }
        float balanceFactor = counterPos / counterTot;
        Debug.DrawLine(p1, p2, Color.blue);
        Debug.DrawLine(p3, p4, Color.blue);
        Debug.DrawLine(p2, p3, Color.blue);
        Debug.DrawLine(p4, p1, Color.blue);
        return balanceFactor;
    }

    public bool CheckCP(Vector3 CP)
    {
        Debug.DrawRay(CP, Vector3.up, Color.blue);
        CP = Vector3.ProjectOnPlane(CP, Vector3.up);
        Vector3 p1 = Vector3.ProjectOnPlane(LeftFoot.transform.position + LeftFoot.transform.up * FootVerticalOffset - LeftFoot.transform.forward * FootHorizontalOffset, Vector3.up);
        Vector3 p2 = Vector3.ProjectOnPlane(RightFoot.transform.position - RightFoot.transform.up * FootVerticalOffset + LeftFoot.transform.forward * FootHorizontalOffset, Vector3.up);
        Vector3 p3 = Vector3.ProjectOnPlane(p2 + RightFoot.transform.up * FootLength, Vector3.up);
        Vector3 p4 = Vector3.ProjectOnPlane(p1 - LeftFoot.transform.up * FootLength, Vector3.up);
        return MathHelpers.CheckPointInQuad(CP, p1, p2, p3, p4);
    }

    public void AddDrag()
    {
        foreach (Rigidbody rb in gameObject.GetComponentsInChildren<Rigidbody>()) {
            rb.drag = Drag;
            rb.angularDrag = angularDrag;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        return;
        foreach (KeyValuePair<Transform, BodyPart> k in BodyMapper.ToList())
        {
            Transform RagdollBone = k.Key;
            BodyPart BodyP = k.Value;
            Transform AnimBone = BodyP.followTransform;
            float weight = BodyP.weight;
            Rigidbody rb = RagdollBone.GetComponent<Rigidbody>();
            if (rb)
                FollowBone(rb, AnimBone, BodyP.lastOrientation);
            BodyP.lastOrientation = AnimBone.rotation;
            BodyMapper[k.Key] = BodyP;
        }
    }
}
