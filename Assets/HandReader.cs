using System;
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

    GameObject leftHandTransform;
    GameObject rightHandTransform;

    // Define finger connections using MANO joint indices
    static readonly (HandJoint, HandJoint)[] fingerConnections = new[]
    {
        // Thumb chain
        (HandJoint.Wrist, HandJoint.Thumb1),
        (HandJoint.Thumb1, HandJoint.Thumb2),
        (HandJoint.Thumb2, HandJoint.Thumb3),
        (HandJoint.Thumb3, HandJoint.Thumb4),
        
        // Index finger chain
        (HandJoint.Wrist, HandJoint.Index1),
        (HandJoint.Index1, HandJoint.Index2),
        (HandJoint.Index2, HandJoint.Index3),
        (HandJoint.Index3, HandJoint.Index4),
        
        // Middle finger chain
        (HandJoint.Wrist, HandJoint.Middle1),
        (HandJoint.Middle1, HandJoint.Middle2),
        (HandJoint.Middle2, HandJoint.Middle3),
        (HandJoint.Middle3, HandJoint.Middle4),
        
        // Ring finger chain
        (HandJoint.Wrist, HandJoint.Ring1),
        (HandJoint.Ring1, HandJoint.Ring2),
        (HandJoint.Ring2, HandJoint.Ring3),
        (HandJoint.Ring3, HandJoint.Ring4),
        
        // Pinky chain
        (HandJoint.Wrist, HandJoint.Pinky1),
        (HandJoint.Pinky1, HandJoint.Pinky2),
        (HandJoint.Pinky2, HandJoint.Pinky3),
        (HandJoint.Pinky3, HandJoint.Pinky4)
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

        // Create separate transforms for each hand
        leftHandTransform = new GameObject("LeftHandTransform");
        rightHandTransform = new GameObject("RightHandTransform");
        leftHandTransform.transform.parent = transform;
        rightHandTransform.transform.parent = transform;

        leftHandSpheres = CreateHandSpheres(Color.red, leftHandTransform.transform);
        rightHandSpheres = CreateHandSpheres(Color.blue, rightHandTransform.transform);
        
        leftHandLines = CreateHandLines(Color.red, leftHandTransform.transform);
        rightHandLines = CreateHandLines(Color.blue, rightHandTransform.transform);
    }

    GameObject[] CreateHandSpheres(Color color, Transform parent)
    {
        GameObject[] spheres = new GameObject[Enum.GetValues(typeof(HandJoint)).Length];
        foreach (HandJoint joint in Enum.GetValues(typeof(HandJoint)))
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = $"{(color == Color.red ? "Left" : "Right")}_{joint}";
            sphere.transform.localScale = Vector3.one * 0.01f; // 1cm diameter
            sphere.GetComponent<Renderer>().material.color = color;
            sphere.transform.parent = parent;
            spheres[(int)joint] = sphere;
        }
        return spheres;
    }

    LineRenderer[] CreateHandLines(Color color, Transform parent)
    {
        LineRenderer[] lines = new LineRenderer[fingerConnections.Length];
        for (int i = 0; i < fingerConnections.Length; i++)
        {
            GameObject lineObj = new($"HandLine_{i}");
            lineObj.transform.parent = parent;
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
        var (leftJoints, leftWristOrientation) = frame.GetLeftHandJoints();
        if (leftWristOrientation.HasValue)
        {
            leftHandTransform.transform.position = leftWristOrientation.Value;
            UpdateHandJointsAndLines(leftJoints, leftHandSpheres, leftHandLines);
        }

        // Update right hand
        var (rightJoints, rightWristOrientation) = frame.GetRightHandJoints();
        if (rightWristOrientation.HasValue)
        {
            rightHandTransform.transform.position = rightWristOrientation.Value;
            UpdateHandJointsAndLines(rightJoints, rightHandSpheres, rightHandLines);
        }
    }

    static void UpdateHandJointsAndLines(List<Vector3> joints, GameObject[] spheres, LineRenderer[] lines)
    {
        // Convert local positions to world space
        var worldJoints = new Vector3[joints.Count];
        for (int i = 0; i < joints.Count; i++)
        {
            worldJoints[i] = spheres[i].transform.parent.TransformPoint(joints[i]);
        }

        // Update spheres and use world positions
        for (int i = 0; i < joints.Count && i < spheres.Length; i++)
        {
            spheres[i].transform.position = worldJoints[i];
        }

        // Update lines with world positions
        for (int i = 0; i < fingerConnections.Length && i < lines.Length; i++)
        {
            (HandJoint start, HandJoint end) = fingerConnections[i];
            int startIdx = (int)start;
            int endIdx = (int)end;
            if (startIdx < joints.Count && endIdx < joints.Count)
            {
                lines[i].SetPosition(0, worldJoints[startIdx]);
                lines[i].SetPosition(1, worldJoints[endIdx]);
            }
        }
    }
}