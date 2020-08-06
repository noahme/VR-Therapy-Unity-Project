using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Indicator : MonoBehaviour
{
    public CueManager cueManager;
    MeshRenderer meshRenderer;
    // Start is called before the first frame update
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (cueManager.state == GameState.CUE)
        {
            meshRenderer.material.color = Color.green;
        }
        else if (cueManager.state == GameState.NOTRUNNING)
        {
            meshRenderer.material.color = Color.red;
        }
        else if (cueManager.state == GameState.RESTARTING)
        {
            meshRenderer.material.color = Color.yellow;
        }
        else if (cueManager.state == GameState.WAITING)
        {
            meshRenderer.material.color = Color.blue;
        }
    }
}
