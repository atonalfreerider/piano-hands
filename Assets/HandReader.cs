using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class HandReader : MonoBehaviour
{
    readonly Dictionary<int, HandFrameData> handPoses = new();
    int currentFrame = 0;
    bool isPlaying = false;
    GameObject[] leftHandSpheres;
    GameObject[] rightHandSpheres;

    void Start()
    {
        // Read and parse JSON file
        string jsonPath = Path.Combine(Application.streamingAssetsPath, "hand_poses.json");
        string jsonContent = File.ReadAllText(jsonPath);
        
        // Use JsonConvert instead of JsonUtility
        Dictionary<string, HandFrameData> rawData = JsonConvert.DeserializeObject<Dictionary<string, HandFrameData>>(jsonContent);
        foreach (KeyValuePair<string, HandFrameData> kvp in rawData)
        {
            handPoses[int.Parse(kvp.Key)] = kvp.Value;
        }

        leftHandSpheres = CreateHandSpheres(Color.red);
        rightHandSpheres = CreateHandSpheres(Color.blue);
    }

    GameObject[] CreateHandSpheres(Color color)
    {
        GameObject[] spheres = new GameObject[15];
        for (int i = 0; i < 15; i++)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = Vector3.one * 0.03f; // 3cm diameter
            sphere.GetComponent<Renderer>().material.color = color;
            sphere.transform.parent = transform;
            spheres[i] = sphere;
        }
        return spheres;
    }

    void Update()
    {
        // Toggle animation with spacebar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isPlaying = !isPlaying;
            if (isPlaying && !handPoses.ContainsKey(currentFrame))
            {
                currentFrame = 0; // Reset to start if at end
            }
        }

        if (isPlaying && handPoses.ContainsKey(currentFrame))
        {
            UpdateHandPositions();
            currentFrame++;
        }
    }

    void UpdateHandPositions()
    {
        HandFrameData frame = handPoses[currentFrame];

        // Update left hand
        List<Vector3> leftJoints = frame.GetLeftHandJoints();
        for (int i = 0; i < leftJoints.Count && i < 15; i++)
        {
            leftHandSpheres[i].transform.position = leftJoints[i];
        }

        // Update right hand
        List<Vector3> rightJoints = frame.GetRightHandJoints();
        for (int i = 0; i < rightJoints.Count && i < 15; i++)
        {
            rightHandSpheres[i].transform.position = rightJoints[i];
        }
    }
}