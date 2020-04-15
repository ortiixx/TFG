using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropCubes : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetButtonDown("Fire1"))
        {
            GameObject gam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Rigidbody rb = gam.AddComponent<Rigidbody>();
            GameObject.Instantiate(gam, transform.position, transform.rotation);
            rb.AddForce(transform.forward * 100f, ForceMode.Impulse);
        }
    }
}
