using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public enum CueType {Positive, Negative}

[RequireComponent(typeof(Throwable))]
[RequireComponent(typeof(VelocityEstimator))]
[RequireComponent(typeof(InteractableHoverEvents))]
public class CueBehavior : MonoBehaviour
{

    CueManager cueManager;
    public CueType cueType;
    Interactable interactable;
    InteractableHoverEvents interactableHoverEvents;
    MeshRenderer meshRenderer;

    void Start()
    {
        cueManager = FindObjectOfType<CueManager>().GetComponent<CueManager>();
        interactable = GetComponent<Interactable>();
        interactableHoverEvents = GetComponent<InteractableHoverEvents>();
        meshRenderer = GetComponentsInChildren<MeshRenderer>()[1];
        interactable.highlightOnHover = false;

        if (cueManager.useOutlines)
        {
            meshRenderer.enabled = true;
            interactableHoverEvents.onHandHoverBegin.AddListener(ChangeOutlineToSelect);
            if (cueType == CueType.Positive)
            {
                ChangeOutlineToPositive();
                interactableHoverEvents.onHandHoverEnd.AddListener(ChangeOutlineToPositive);
            }
            else
            {
                ChangeOutlineToNegative();
                interactableHoverEvents.onHandHoverEnd.AddListener(ChangeOutlineToNegative);
            }
        }
        else
        {
            ChangeOutlineToSelect();
            interactableHoverEvents.onHandHoverBegin.AddListener(EnableMeshRenderer);
            interactableHoverEvents.onHandHoverEnd.AddListener(DisableMeshRenderer);
            meshRenderer.enabled = false;
            

        }
    }

    void ChangeOutlineToSelect()
    {
        meshRenderer.material = Resources.Load("Materials/HoverHighlight", typeof(Material)) as Material;
    }

    void ChangeOutlineToPositive()
    {
        meshRenderer.material = Resources.Load("Materials/PositiveCueHighlight", typeof(Material)) as Material;
    }

    void ChangeOutlineToNegative()
    {
        meshRenderer.material = Resources.Load("Materials/NegativeCueHighlight", typeof(Material)) as Material;
    }

    void EnableMeshRenderer()
    {
        meshRenderer.enabled = true;
    }

    void DisableMeshRenderer()
    {
        meshRenderer.enabled = false;
    }




}
