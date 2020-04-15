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
    }

    public Transform animationsRoot;
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
        var result = bonesRagdoll.Zip(bonesAnimations, (first, second) => new Tuple<Transform, Transform>(first,second));
        foreach (Tuple<Transform, Transform> t in result)
        {
            BodyPart b = new BodyPart();
            b.weight = 1.0f;
            b.followTransform = t.Item2;
            b.lastOrientation = t.Item2.rotation;
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
        Quaternion q2 = AnimBone.rotation * Quaternion.Inverse(rb.rotation);
        if (q2 != Quaternion.identity)
        {
            CharacterJoint Cj = rb.GetComponent<CharacterJoint>();
            if(Cj)
                Cj.enableProjection = true;
            rb.isKinematic = false;
            MathHelpers.Quat2VecAngle(q2, out dir, out angle);
            rb.angularDrag = angularDrag;
            rb.drag = Drag;
            //rb.detectCollisions = false;
            rb.AddTorque(dir * angle * k1 + k2*(TargetAngularSpeed-rb.angularVelocity), ForceMode.VelocityChange);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        foreach(KeyValuePair<Transform, BodyPart> k in BodyMapper.ToList())
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
