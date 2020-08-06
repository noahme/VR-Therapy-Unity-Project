using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitialRotation : MonoBehaviour
{
    public Vector3 initialRotation;
    void Start()
    {
        transform.rotation = Quaternion.Euler(initialRotation);
    }
}
