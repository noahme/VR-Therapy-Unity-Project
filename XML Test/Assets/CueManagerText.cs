using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameStatusText : MonoBehaviour
{
    public CueManager cueManager;
    TextMeshProUGUI text;
    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if (cueManager.state == GameState.CUE)
        {
            text.text = "Playing";
            text.color = Color.green;
        }
        else if (cueManager.state == GameState.NOTRUNNING)
        {
            text.text = "Not Playing";
            text.color = Color.red;
        }
        else if (cueManager.state == GameState.RESTARTING)
        {
            text.text = "Restarting...";
            text.color = Color.yellow;
        }
        else if (cueManager.state == GameState.WAITING)
        {
            text.text = "Spawning next cue...";
            text.color = Color.blue;
        }
    }
}
