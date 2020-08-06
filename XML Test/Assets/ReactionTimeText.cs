using TMPro;
using UnityEngine;

public class ReactionTimeText : MonoBehaviour
{
    public CueManager cueManager;
    TextMeshProUGUI text;
    // Start is called before the first frame update
    void Start()
    {
        cueManager.OnPreviousReactionTimeUpdate += UpdateReactionTime;
        text = GetComponent<TextMeshProUGUI>();
    }

    void UpdateReactionTime()
    {
        text.text = cueManager.previousRawReactionTime.ToString() + " seconds";
    }

    private void OnDestroy()
    {
        cueManager.OnPreviousReactionTimeUpdate -= UpdateReactionTime;
    }
}
