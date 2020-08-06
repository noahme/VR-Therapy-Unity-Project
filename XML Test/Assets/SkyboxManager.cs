using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxManager : MonoBehaviour
{
    public Material[] skyboxes;
    private int currentSkyboxIndex = 0;

    public void changeSkybox()
    {
        currentSkyboxIndex++;
        if (currentSkyboxIndex >= skyboxes.Length)
        {
            currentSkyboxIndex = 0;
        }
        RenderSettings.skybox = skyboxes[currentSkyboxIndex];
    }
}
