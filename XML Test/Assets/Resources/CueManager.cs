using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Xml.Serialization;
using System.Collections;
using Valve.VR.InteractionSystem;
using Valve.VR;
using System;
using TMPro;

public enum GameState { START, WAITING, CUE, NOTRUNNING, RESTARTING}
public class CueManager : MonoBehaviour
{
    float gameNumber;
    public bool showTimeIntervals;
    public TextMeshProUGUI text;

    public bool useOutlines;
    public bool useGravity;
    public int numControls;
    public int numOrderedCues;
    public int numRandomCues;
    public bool alternateCues;
    public float maxDistanceFromSpawn = 2f;
    public Material material;
    [HideInInspector]
    public GameState state;

    public SteamVR_Action_Boolean gripAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip");
    public SteamVR_Input_Sources leftHandSource;
    public SteamVR_Input_Sources rightHandSource;

    public float cueDelayTime;
    public float restartTime = 0.3f;
    public Transform spawnPoint;
    public GameObject[] prefabList;
    public float handDestroyDeltaDistance;

    public GameObject leftHand;
    public GameObject rightHand;
    public BroadCastMessages playerMessager;

    //used for text object but makes code confusing
    /*
    [HideInInspector]
    public List<GameObject> selectedPrefabs = new List<GameObject>();
    */

    //CueContainer cueContainer;
    GameObject currentCue;

    public event Action OnPreviousReactionTimeUpdate;
    public event Action OnGameEnd;

    [HideInInspector]
    public List<float> reactionTimes = new List<float>();
    [HideInInspector]
    public float previousRawReactionTime = 0f;
    [HideInInspector]
    public bool restarting = false;

    public bool showDebugText;

    bool isGrabbing = false;

    float initialHandSeparationDistance;

    Hand rightHandHand;
    Hand leftHandHand;

    List<float> waitTimes;

    /*
    [HideInInspector]
    public List<ReactionData> reactionData = new List<ReactionData>();
    */

    private string xmlFilePath;

    private void LateUpdate()
    {
        //Debug.LogWarning("next frame");
    }

    private void Start()
    {
        gameNumber = 1;
        xmlFilePath = Application.dataPath + "/Resources/cue.xml";
        material.color = Color.blue;
        playerMessager.OnTwoHandGrabStart += StartGrab;
        state = GameState.NOTRUNNING;
        rightHandHand = rightHand.GetComponent<Hand>();
        leftHandHand = leftHand.GetComponent<Hand>();
    }

    public void OnStartButtonPress()
    {
        if (state == GameState.NOTRUNNING)
        {
            StartGame();
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(RestartGame());
        }
    }

    IEnumerator RestartGame()
    {
        state = GameState.RESTARTING;
        if (currentCue != null)
        {
            //In case player somehow uses Vive pointers to hit button while gripping cue
            Hand rightHandHand = rightHand.GetComponent<Hand>();
            Hand leftHandHand = leftHand.GetComponent<Hand>();
            if (rightHandHand.ObjectIsAttached(currentCue))
            {
                rightHandHand.DetachObject(currentCue);
            }
            if (leftHandHand.ObjectIsAttached(currentCue))
            {
                leftHandHand.DetachObject(currentCue);
            }
            
        }
        
        Destroy(currentCue);
        yield return new WaitForSeconds(2f);
        state = GameState.START;
        StartGame();
    }

    public void StartGame()
    {
        state = GameState.START;
        reactionTimes.Clear();

        //Gets cues from XML file and sorts them lowest to highest by weight
        CueContainer cueContainer = LoadCueContainerFromXMLFile();
        cueContainer.cues.Sort(); //highest to lowest weight

        if (showDebugText)
        {
            Debug.Log("Cue list from XML file:");
            for (int i = 0; i < cueContainer.cues.Count; i++)
            {
                Debug.Log(cueContainer.cues[i].prefabName + " " + cueContainer.cues[i].weight);
            }
        }

        List<Cue> originalCueList = cueContainer.cues;
        List<Cue> selectedCues = SelectCues(cueContainer.cues);
        List<GameObject> selectedPrefabs = SelectPrefabs(selectedCues);

        if (showDebugText)
        {
            Debug.Log("PREFABLIST:");
            for (int i = 0; i < selectedPrefabs.Count; i++)
            {
                if (showDebugText) Debug.Log(selectedPrefabs[i].name);
            }
            Debug.Log("CUETYPES");
            for (int i = 0; i < cueContainer.cues.Count; i++)
            {
                Debug.Log(cueContainer.cues[i].type);
            }
        }

        StartCoroutine(PlayThroughGame(selectedCues, selectedPrefabs, originalCueList));
    }

    IEnumerator PlayThroughGame(List<Cue> selectedCues, List<GameObject> selectedPrefabs, List<Cue> originalCueList)
    {
        waitTimes = new List<float>();

        float timeOfLastCheck = 0;
        List<float> timeUntilGrabPositive = new List<float>();
        List<float> timeUntilGrabNegative = new List<float>();
        List<float> timeUntilDestroyPositive = new List<float>();
        List<float> timeUntilDestroyNegative = new List<float>();

        List<ReactionData> reactionData = new List<ReactionData>();

        Rigidbody currentCueRB;
        CueBehavior currentCueBehavior;
        
        for (int i = 0; i < selectedPrefabs.Count; i++)
        {
            float interactionStartTime;
            float interactionEndTime;
            float grabStartTime;

            state = GameState.WAITING;
            yield return new WaitForSecondsRealtime(cueDelayTime);
            state = GameState.CUE;

            currentCue = Instantiate(selectedPrefabs[i], spawnPoint.position, Quaternion.identity);

            interactionStartTime = Time.realtimeSinceStartup;
            currentCueRB = currentCue.GetComponent<Rigidbody>();
            currentCueBehavior = currentCue.GetComponent<CueBehavior>();
            currentCueRB.useGravity = useGravity;

            //This loop handles destruction of the cue gameobject
            while (currentCue != null)
            {
                text.text = (Time.realtimeSinceStartup - interactionStartTime).ToString();
                if (isGrabbing)
                {
                    float currentHandSeparationDistance = (rightHand.transform.position - leftHand.transform.position).magnitude;
                    grabStartTime = Time.realtimeSinceStartup; //ideally should be called in the StartGrab() method but that only moves it like two lines of code forward

                    if (currentCueBehavior.cueType == CueType.Positive)
                    {
                        if (showDebugText) Debug.Log("this cue is a MAX cue");
                        while (currentHandSeparationDistance - initialHandSeparationDistance < handDestroyDeltaDistance && isGrabbing)
                        {
                            currentHandSeparationDistance = (rightHand.transform.position - leftHand.transform.position).magnitude;
                            isGrabbing = !IsGrabEnd(currentCue);
                            text.text = (Time.realtimeSinceStartup - grabStartTime).ToString();
                            if (showTimeIntervals) Debug.Log(Time.realtimeSinceStartup - timeOfLastCheck);
                            //waitTimes.Add(Time.realtimeSinceStartup - timeOfLastCheck);
                            timeOfLastCheck = Time.realtimeSinceStartup;
                            yield return new WaitForSecondsRealtime(0.02f);
                        }
                    }
                    else if (currentCueBehavior.cueType == CueType.Negative)
                    {
                        if (showDebugText) Debug.Log("this cue is a MIN cue");
                        while (currentHandSeparationDistance - initialHandSeparationDistance > -handDestroyDeltaDistance && isGrabbing)
                        {
                            currentHandSeparationDistance = (rightHand.transform.position - leftHand.transform.position).magnitude;
                            isGrabbing = !IsGrabEnd(currentCue);
                            text.text = (Time.realtimeSinceStartup - grabStartTime).ToString();
                            if (showTimeIntervals) Debug.Log(Time.realtimeSinceStartup - timeOfLastCheck);
                            //waitTimes.Add(Time.realtimeSinceStartup - timeOfLastCheck);
                            timeOfLastCheck = Time.realtimeSinceStartup;
                            yield return new WaitForSecondsRealtime(0.02f);
                        }
                    }
                    //if isGrabbing is true, the only way the while loop could be broken is if hands moved enough, so this if statement is only called when the cue should be destroyed
                    if (isGrabbing)
                    {
                        //Detach object from both hands before destroying to prevent issues
                        if (rightHandHand.ObjectIsAttached(currentCue))
                        {
                            rightHandHand.DetachObject(currentCue);
                        }
                        if (leftHandHand.ObjectIsAttached(currentCue))
                        {
                            leftHandHand.DetachObject(currentCue);
                        }
                        interactionEndTime = Time.realtimeSinceStartup;
                        previousRawReactionTime = interactionEndTime - interactionStartTime;
                        float timeUntilGrab = grabStartTime - interactionStartTime;
                        float timeUntilDestroy = interactionEndTime - grabStartTime;

                        reactionData.Add(new ReactionData(previousRawReactionTime, timeUntilGrab, timeUntilDestroy));

                        if (currentCueBehavior.cueType == CueType.Positive && !selectedCues[i].category.Equals("control"))
                        {
                            timeUntilGrabPositive.Add(timeUntilGrab);
                            timeUntilDestroyPositive.Add(timeUntilDestroy);
                        }
                        else if (currentCueBehavior.cueType == CueType.Negative && !selectedCues[i].category.Equals("control"))
                        {
                            timeUntilGrabNegative.Add(timeUntilGrab);
                            timeUntilDestroyNegative.Add(timeUntilDestroy);
                        }
                        if (OnPreviousReactionTimeUpdate != null)
                        {
                            OnPreviousReactionTimeUpdate();
                        }
                        Destroy(currentCue);
                        isGrabbing = false;
                    }
                }
                else if (currentCue != null && (currentCue.transform.position - spawnPoint.position).magnitude > maxDistanceFromSpawn)
                {
                    currentCue.transform.position = spawnPoint.position;
                    currentCueRB.velocity = Vector3.zero;
                    currentCueRB.angularVelocity = Vector3.zero;
                    currentCue.transform.rotation = Quaternion.identity;
                }
                yield return new WaitForSecondsRealtime(0.02f);
            }
        }
        if (OnGameEnd != null)
        {
            OnGameEnd();
        }
        Debug.Log("Selected prefabs count is " + selectedPrefabs.Count.ToString());
        Debug.Log("Reactiondatacount is " + reactionData.Count.ToString());
        Debug.Log("Selected cue count is " + selectedCues.Count.ToString());

        //WriteWaitTimesToTextFile(waitTimes);
        WriteDataToTextFile(timeUntilGrabPositive, timeUntilDestroyPositive, timeUntilGrabNegative, timeUntilDestroyNegative);
        EndGame(reactionData, originalCueList, selectedCues); //later put reaction times into this
    }

    void WriteWaitTimesToTextFile(List<float> waitTimes)
    {
        String path = Application.dataPath + "/Resources/waittimes.txt";
        File.AppendAllText(path, "GAMENUMBER: " + gameNumber.ToString() + "\n");
        for (int i = 0; i < waitTimes.Count; i++)
        {
            File.AppendAllText(path, waitTimes[i].ToString() + "\n");
        }
    }

    void WriteDataToTextFile(List<float> timeUntilGrabPositive, List<float> timeUntilDestroyPositive, List<float> timeUntilGrabNegative, List<float> timeUntilDestroyNegative)
    {
        String pathTUGP = Application.dataPath + "/Resources/tugp.txt";
        String pathTUDP = Application.dataPath + "/Resources/tudp.txt";
        String pathTUGN = Application.dataPath + "/Resources/tugn.txt";
        String pathTUDN = Application.dataPath + "/Resources/tudn.txt";

        ///*
        File.AppendAllText(pathTUGP, gameNumber.ToString() + "NEW 8 2 RUN Time Until Grab Positive:\n");
        File.AppendAllText(pathTUDP, gameNumber.ToString() + "NEW 8 2 RUN Time Until Destroy Positive:\n");
        File.AppendAllText(pathTUGN, gameNumber.ToString() + "NEW 8 2 RUN Time Until Grab Negative:\n");
        File.AppendAllText(pathTUDN, gameNumber.ToString() + "NEW 8 2 RUN Time Until Destroy Negative:\n");
        ///*

        /*
        File.WriteAllText(pathTUGP, "Time Until Grab Positive:\n");
        File.WriteAllText(pathTUDP, "Time Until Destroy Positive:\n");
        File.WriteAllText(pathTUGN, "Time Until Grab Negative:\n");
        File.WriteAllText(pathTUDN, "Time Until Destroy Negative:\n");
        //*/

        for (int i = 0; i < timeUntilGrabPositive.Count; i++)
        {
            File.AppendAllText(pathTUGP, timeUntilGrabPositive[i].ToString() + "\n");
        }
        for (int i = 0; i < timeUntilDestroyPositive.Count; i++)
        {
            File.AppendAllText(pathTUDP, timeUntilDestroyPositive[i].ToString() + "\n");
        }
        for (int i = 0; i < timeUntilGrabNegative.Count; i++)
        {
            File.AppendAllText(pathTUGN, timeUntilGrabNegative[i].ToString() + "\n");
        }
        for (int i = 0; i < timeUntilDestroyNegative.Count; i++)
        {
            File.AppendAllText(pathTUDN, timeUntilDestroyNegative[i].ToString() + "\n");
        }
    }

    bool IsGrabEnd(GameObject go)
    {
        if (rightHandHand.ObjectIsAttached(go) && leftHandHand.ObjectIsAttached(go))
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    void EndGame(List<ReactionData> cueData, List<Cue> originalCueList, List<Cue> selectedCues)
    {
        gameNumber++;
        StopAllCoroutines(); //just in case, to satisfy my OCD
        state = GameState.NOTRUNNING;
        SaveGameDataToXMLFile(cueData, originalCueList, selectedCues); //later this will include reaction times
    }

    void StartGrab()
    {
        initialHandSeparationDistance = (rightHand.transform.position - leftHand.transform.position).magnitude;
        isGrabbing = true;
    }

    public List<Cue> SelectCues(List<Cue> cues)
    {
        List<Cue> selectedCues = new List<Cue>();
        System.Random randomNumberGenerator = new System.Random();
        List<Cue> controlCuesQueue = new List<Cue>();
        List<Cue> positiveCuesQueue = new List<Cue>();
        List<Cue> negativeCuesQueue = new List<Cue>();
        List<Cue> excludeControlsQueue = new List<Cue>();

        List<Cue> sortedOptions = cues; //Keeping this line incase I want to do other type of sorting in the future (currently just leaves cues sorted lowest to highest by weight)
        if (showDebugText)
        {
            Debug.Log("SORTEDOPTIONS");
            for (int i = 0; i < sortedOptions.Count; i++)
            {
                Debug.Log(sortedOptions[i].prefabName);
            }
        }

        for (int i = 0; i < sortedOptions.Count; i++)
        {
            switch (sortedOptions[i].category)
            {
                case "control":
                    controlCuesQueue.Add(sortedOptions[i]);
                    break;
                case "positive":
                    positiveCuesQueue.Add(sortedOptions[i]);
                    break;
                case "negative":
                    negativeCuesQueue.Add(sortedOptions[i]);
                    break;
                default:
                    Debug.LogError("category for cue in XML file not valid");
                    break;
            }
            if (!sortedOptions[i].category.Equals("control"))
            {
                excludeControlsQueue.Add(sortedOptions[i]);
            }
        }

        if (numControls > controlCuesQueue.Count)
        {
            Debug.LogWarning("numControls larger than number of controls, used max number of controls instead");
            numControls = controlCuesQueue.Count;
        }
        if (numOrderedCues > excludeControlsQueue.Count)
        {
            Debug.LogWarning("numOrderedCues larger than number of cues, used max number of cues instead and set numRandomCues to 0");
            numOrderedCues = excludeControlsQueue.Count;
            numRandomCues = 0;
        }
        else if (numOrderedCues + numRandomCues > excludeControlsQueue.Count)
        {
            Debug.LogWarning("numOrderedCues + numRandomCues greater than number of cues, reduced numRandomCues to use just all cues");
            numRandomCues = excludeControlsQueue.Count - numOrderedCues;
        }



        //Placeholder code, ML algorithm will handle actual selection order
        for (int i = 0; i < numControls; i++)
        {
            int randomControlIndex = randomNumberGenerator.Next(controlCuesQueue.Count - 1);
            selectedCues.Add(controlCuesQueue[randomControlIndex]);
            controlCuesQueue.RemoveAt(randomControlIndex);
        }

        if (alternateCues)
        {
            for (int i = 0; i < numOrderedCues; i++)
            {
                if (i % 2 == 0)
                {
                    selectedCues.Add(negativeCuesQueue[0]);
                    negativeCuesQueue.Remove(negativeCuesQueue[0]);
                }
                else
                {
                    selectedCues.Add(positiveCuesQueue[0]);
                    positiveCuesQueue.Remove(positiveCuesQueue[0]);
                }
            }
            for (int i = numOrderedCues; i < numRandomCues + numOrderedCues; i++)
            {
                if (i % 2 == 0)
                {
                    int negativeCuesQueueIndex = randomNumberGenerator.Next(negativeCuesQueue.Count - 1);
                    selectedCues.Add(negativeCuesQueue[negativeCuesQueueIndex]);
                    negativeCuesQueue.Remove(negativeCuesQueue[negativeCuesQueueIndex]);
                }
                else
                {
                    int positiveCuesQueueIndex = randomNumberGenerator.Next(positiveCuesQueue.Count - 1);
                    selectedCues.Add(positiveCuesQueue[positiveCuesQueueIndex]);
                    positiveCuesQueue.Remove(positiveCuesQueue[positiveCuesQueueIndex]);
                }
            }
        }
        else
        {
            for (int i = 0; i < numOrderedCues; i++)
            {
                if (i < excludeControlsQueue.Count)
                {
                    selectedCues.Add(excludeControlsQueue[i]);
                    excludeControlsQueue.Remove(excludeControlsQueue[i]);
                }
            }
            for (int i = 0; i < numRandomCues; i++)
            {
                int selectedCuesIndex = randomNumberGenerator.Next(numControls, selectedCues.Count);
                int excludeControlsQueueIndex = randomNumberGenerator.Next(excludeControlsQueue.Count - 1);
                selectedCues.Insert(selectedCuesIndex, excludeControlsQueue[excludeControlsQueueIndex]);
                excludeControlsQueue.Remove(excludeControlsQueue[excludeControlsQueueIndex]);
            }
        }

        if (showDebugText)
        {
            Debug.Log("SELECTEDCUES");
            for (int i = 0; i < selectedCues.Count; i++)
            {
                Debug.Log(selectedCues[i].prefabName);
            }
        }

        return selectedCues;
    }

    public List<GameObject> SelectPrefabs(List<Cue> selectedCues) {
        List<GameObject> selectedPrefabs = new List<GameObject>();

        for (int i = 0; i < selectedCues.Count; i++)
        {
            for (int j = 0; j < prefabList.Length; j++)
            {
                if (prefabList[j].name.Equals(selectedCues[i].prefabName))
                {
                    selectedPrefabs.Add(prefabList[j]);
                    if (showDebugText) Debug.Log("Cue " + selectedCues[i].prefabName + " AKA " + prefabList[j].name + " has been selected");
                }
            }
        }

        return selectedPrefabs;
}

    private void OnDestroy()
    {
        //not really needed but good practice i guess
        playerMessager.OnTwoHandGrabStart -= StartGrab;
    }

    List<Cue> UpdateWeights(List<Cue> originalCueList, List<ReactionData> cueData, List<Cue> selectedCues)
    {
        //Could multiply reaction time and other data by some sort of constant that acts as a weight in a NN
        List<Cue> unusedCues = new List<Cue>();
        bool match;

        for (int i = 0; i < originalCueList.Count; i++)
        {
            match = false;
            for (int j = 0; j < selectedCues.Count; j++)
            {
                if (selectedCues[j].prefabName.Equals(originalCueList[i].prefabName))
                {
                    match = true;
                }
            }
            if (!match)
            {
                unusedCues.Add(originalCueList[i]);
            }
        }

        for (int i = 0; i < selectedCues.Count; i++)
        {
            if (selectedCues[i].type.Equals("MIN"))
            {
                //fastest timeUntilGrab and slowest time to destroy should have lowest weight
                selectedCues[i].weight = cueData[i].timeUntilGrab - cueData[i].timeUntilDestroy;
            }
            else if (selectedCues[i].type.Equals("MAX"))
            {
                //fastest timeUntilGrab and fastest time to destroy should have lowest weight (want to use most effective positive cues)
                selectedCues[i].weight = cueData[i].timeUntilGrab + cueData[i].timeUntilDestroy;
            }
        }
        for (int i = 0; i < unusedCues.Count; i++)
        {
            selectedCues.Add(unusedCues[i]);
        }

        return selectedCues;
    }
    void SaveGameDataToXMLFile(List<ReactionData> cueData, List<Cue> originalCueList, List<Cue> selectedCues)
    {
        List<Cue> updatedCueList = UpdateWeights(originalCueList, cueData, selectedCues);
        CueContainer sampleCueContainer = new CueContainer();
        sampleCueContainer.cues = updatedCueList;
        WriteCueContainerToXMLFile(sampleCueContainer);

        /*
        var xmls = new XmlSerializer(typeof(CueContainer));
        var stream = new FileStream(xmlFilePath, FileMode.Create);

        xmls.Serialize(stream, sampleCueContainer);
        stream.Close();
        */
        
    }

    void WriteCueContainerToXMLFile(CueContainer cueContainer)
    {
        var xmls = new XmlSerializer(typeof(CueContainer));
        var stream = new FileStream(xmlFilePath, FileMode.Create);

        xmls.Serialize(stream, cueContainer);
        stream.Close();
    }

    CueContainer LoadCueContainerFromXMLFile()
    {
        CueContainer cc = new CueContainer();
        var xmls = new XmlSerializer(typeof(CueContainer));
        var stream = new FileStream(xmlFilePath, FileMode.Open);
        cc = xmls.Deserialize(stream) as CueContainer;
        stream.Close();
        return cc;

    }

    public void RandomizeWeights()
    {
        gameNumber = 1;
        System.Random randomNumberGenerator = new System.Random();
        if (state == GameState.NOTRUNNING)
        {
            CueContainer cc = LoadCueContainerFromXMLFile();
            for (int i = 0; i < cc.cues.Count; i++)
            {
                cc.cues[i].weight = (float)randomNumberGenerator.NextDouble() * 1000;
            }
            WriteCueContainerToXMLFile(cc);
        }
    }
}

public struct ReactionData
{
    //In the future would put in variables for things like average stress level, could then use this data in the UpdateWeights section
    public float rawReactionTime;
    public float timeUntilGrab;
    public float timeUntilDestroy;

    public ReactionData(float rawReactionTime, float timeUntilGrab, float timeUntilDestroy)
    {
        this.rawReactionTime = rawReactionTime;
        this.timeUntilGrab = timeUntilGrab;
        this.timeUntilDestroy = timeUntilDestroy;
    }
}