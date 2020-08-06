using TMPro;
using UnityEngine;

public class PreviousGameTimesText : MonoBehaviour
{
    public CueManager cueManager;
    TextMeshProUGUI text;

    private void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        cueManager.OnGameEnd += UpdateText;
    }

    void UpdateText()
    {
        text.text = "";
        /*
        for (int i = 0; i < cueManager.reactionData.Count; i++)
        {
            text.text += cueManager.selectedPrefabs[i].name + ": " + cueManager.reactionData[i].rawReactionTime + " seconds\n";
        
        }
        */
    }
}
