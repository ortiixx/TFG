using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovableCube : MonoBehaviour
{
    private void OnMouseDrag()
    {
        Vector3 MousePos = Input.mousePosition;
        MousePos.z = Camera.main.nearClipPlane;
        Vector3 WorldMousePos = Camera.main.ScreenToWorldPoint(MousePos);
        WorldMousePos.z = transform.position.z+Input.GetAxis("Vertical")*Time.deltaTime;
        transform.position = WorldMousePos;
    }
}
