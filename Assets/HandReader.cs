using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

public class HandReader : MonoBehaviour
{
    private Dictionary<int, HandPoseFrame> handPoses = new Dictionary<int, HandPoseFrame>();
    private int currentFrame = 0;
    private bool isPlaying = false;
    private GameObject[] leftHandSpheres;
    private GameObject[] rightHandSpheres;

    [System.Serializable]
    private class HandPoseFrame
    {
        public JToken left_hands { get; set; }
        public JToken right_hands { get; set; }
    }

    void Start()
    {
        // Read JSON file
        string jsonPath = Path.Combine(Application.streamingAssetsPath, "hand_poses.json");
        string jsonContent = File.ReadAllText(jsonPath);
        
        // Parse JSON
        JObject data = JObject.Parse(jsonContent);
        foreach (var frame in data)
        {
            int frameNumber = int.Parse(frame.Key);
            var pose = new HandPoseFrame
            {
                left_hands = frame.Value["left_hands"],
                right_hands = frame.Value["right_hands"]
            };
            handPoses[frameNumber] = pose;
        }

        // Initialize spheres (15 joints per hand)
        leftHandSpheres = CreateHandSpheres(Color.red);
        rightHandSpheres = CreateHandSpheres(Color.blue);
    }

    private GameObject[] CreateHandSpheres(Color color)
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

    private void UpdateHandPositions()
    {
        HandPoseFrame frame = handPoses[currentFrame];

        // Update left hand
        try 
        {
            var leftHandData = frame.left_hands;
            if (leftHandData != null && leftHandData.HasValues && leftHandData[0].HasValues)
            {
                var joints = leftHandData[0][0][0] as JArray;
                if (joints != null)
                {
                    for (int i = 0; i < joints.Count && i < 15; i++)
                    {
                        var coords = joints[i];
                        Vector3 position = new Vector3(
                            coords[0].Value<float>(),
                            coords[1].Value<float>(),
                            coords[2].Value<float>()
                        );
                        leftHandSpheres[i].transform.position = position;
                    }
                }
            }
        }
        catch (System.Exception) { } // Ignore empty or malformed data

        // Update right hand
        try 
        {
            var rightHandData = frame.right_hands;
            if (rightHandData != null && rightHandData.HasValues && rightHandData[0].HasValues)
            {
                var joints = rightHandData[0][0][0] as JArray;
                if (joints != null)
                {
                    for (int i = 0; i < joints.Count && i < 15; i++)
                    {
                        var coords = joints[i];
                        Vector3 position = new Vector3(
                            coords[0].Value<float>(),
                            coords[1].Value<float>(),
                            coords[2].Value<float>()
                        );
                        rightHandSpheres[i].transform.position = position;
                    }
                }
            }
        }
        catch (System.Exception) { } // Ignore empty or malformed data
    }
}