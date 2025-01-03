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
    LineRenderer[] leftHandLines;
    LineRenderer[] rightHandLines;
    const float TARGET_FPS = 30f;
    float frameTimer = 0f;
    const float frameInterval = 1f / TARGET_FPS;

    // Define finger connections (indices of joints to connect)
    static readonly (int, int)[] fingerConnections = new[]
    {
        // Thumb
        (0, 1), (1, 2), (2, 3),
        // Index
        (0, 4), (4, 5), (5, 6),
        // Middle
        (0, 7), (7, 8), (8, 9),
        // Ring
        (0, 10), (10, 11), (11, 12),
        // Pinky
        (0, 13), (13, 14), (14, 15)
    };

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
        
        leftHandLines = CreateHandLines(Color.red);
        rightHandLines = CreateHandLines(Color.blue);
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

    LineRenderer[] CreateHandLines(Color color)
    {
        LineRenderer[] lines = new LineRenderer[fingerConnections.Length];
        for (int i = 0; i < fingerConnections.Length; i++)
        {
            GameObject lineObj = new($"HandLine_{i}");
            lineObj.transform.parent = transform;
            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.startWidth = 0.005f;
            line.endWidth = 0.005f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = color;
            line.endColor = color;
            line.positionCount = 2;
            lines[i] = line;
        }
        return lines;
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
            frameTimer += Time.deltaTime;
            if (frameTimer >= frameInterval)
            {
                UpdateHandPositions();
                currentFrame++;
                frameTimer = 0f;
            }
        }
    }

    void UpdateHandPositions()
    {
        HandFrameData frame = handPoses[currentFrame];

        // Update left hand
        List<Vector3> leftJoints = frame.GetLeftHandJoints();
        UpdateHandJointsAndLines(leftJoints, leftHandSpheres, leftHandLines);

        // Update right hand
        List<Vector3> rightJoints = frame.GetRightHandJoints();
        UpdateHandJointsAndLines(rightJoints, rightHandSpheres, rightHandLines);
    }

    static void UpdateHandJointsAndLines(List<Vector3> joints, GameObject[] spheres, LineRenderer[] lines)
    {
        // Update spheres
        for (int i = 0; i < joints.Count && i < spheres.Length; i++)
        {
            spheres[i].transform.position = joints[i];
        }

        // Update lines
        for (int i = 0; i < fingerConnections.Length && i < lines.Length; i++)
        {
            (int start, int end) = fingerConnections[i];
            if (start < joints.Count && end < joints.Count)
            {
                lines[i].SetPosition(0, joints[start]);
                lines[i].SetPosition(1, joints[end]);
            }
        }
    }
}