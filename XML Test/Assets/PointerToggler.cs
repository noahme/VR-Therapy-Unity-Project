using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;


public class PointerToggler : MonoBehaviour
{
    public void OnPressDown()
    {
        if (gameObject.activeInHierarchy == true)
        {
            gameObject.SetActive(false);
        }
        else if (gameObject.activeInHierarchy == false)
        {
            gameObject.SetActive(true);
        }
    }
}
