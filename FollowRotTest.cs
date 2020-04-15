using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowRotTest : MonoBehaviour
{
    public Transform Follow;

    private void Start()
    {
        Follow.rotation = Quaternion.Inverse(Follow.parent.rotation)*Follow.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 dir;
        float angle;
        Rigidbody rb = GetComponent<Rigidbody>();
        Quaternion q2 = Follow.rotation*Quaternion.Inverse(transform.rotation);
        if (q2 == Quaternion.identity)
            return;
        MathHelpers.Quat2VecAngle(q2, out dir, out angle);
        Debug.Log(dir);
        Debug.Log(angle);
        rb.AddTorque(dir * angle*0.7f, ForceMode.Acceleration);
    }
}
