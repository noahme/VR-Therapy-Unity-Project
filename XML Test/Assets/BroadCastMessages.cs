using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BroadCastMessages : MonoBehaviour
{
    public event Action OnTwoHandGrabStart;

    void TwoHandGrabStart()
    {
        if (OnTwoHandGrabStart != null)
        {
            OnTwoHandGrabStart();
        }
        Debug.Log("Player called TwoHandGrabStart");
    }

    
}
