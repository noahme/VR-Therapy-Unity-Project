using UnityEngine;

public class StartButton : MonoBehaviour
{
    public CueManager cueManager;

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time > 1f)
        {
            cueManager.OnStartButtonPress();
        }
    }
}
