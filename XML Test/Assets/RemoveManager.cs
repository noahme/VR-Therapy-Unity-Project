using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class RemoveManager : MonoBehaviour
{
    // Start is called before the first frame update
    Interactable interactable;
    void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    private void OnDestroy()
    {
        //interactable.hideSkeletonOnAttach = false;
        interactable.hideHandOnAttach = false;
    }
}
