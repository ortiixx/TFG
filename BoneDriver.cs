using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoneDriver : MonoBehaviour
{
    public Rigidbody EndEffector;
    public Transform Target;
    public float force;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("I Exist!");
    }

    void Solve(Rigidbody Effector, int count)
    {
        if(count>0)
        {
            Debug.Log(Effector.name);
            Vector3 dir = Target.position - Effector.position;
            if(Effector.name == "Spine1_M")
                Effector.AddForceAtPosition(dir * force, Effector.position-Effector.transform.right, ForceMode.Acceleration);
            else
                Effector.AddForce(dir * force * Effector.mass, ForceMode.Acceleration);
            Transform currentP = Effector.transform.parent;
            while (!currentP.GetComponent<Rigidbody>())
                currentP = currentP.transform.parent;

            Solve(currentP.GetComponent<Rigidbody>(), --count);
            if (Effector.name == "Spine1_M")
                Debug.DrawRay(Effector.position - Effector.transform.right*0.25f, dir, Color.red);
            else
                Debug.DrawRay(Effector.position, dir, Color.red);
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (CharacterJoint cj in transform.GetComponentsInChildren<CharacterJoint>())
            cj.enableProjection = true;
        Solve(EndEffector, 4);
        //EndEffector.freezeRotation = true;
    }
}
