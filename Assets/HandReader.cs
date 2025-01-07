using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Linq;

public class HandReader : MonoBehaviour
{
    Dictionary<int, FrameData> framePoses = new();
    GameObject[] bodyJointSpheres;
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
    LineRenderer[] bodyLines;

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

    static readonly (SmplJoint, SmplJoint)[] bodyConnections = new[]
    {
        // Spine
        (SmplJoint.Pelvis, SmplJoint.Spine1),
        (SmplJoint.Spine1, SmplJoint.Spine2),
        (SmplJoint.Spine2, SmplJoint.Spine3),
        (SmplJoint.Spine3, SmplJoint.Neck),
        (SmplJoint.Neck, SmplJoint.Head),

        // Left leg
        (SmplJoint.Pelvis, SmplJoint.L_Hip),
        (SmplJoint.L_Hip, SmplJoint.L_Knee),
        (SmplJoint.L_Knee, SmplJoint.L_Ankle),
        (SmplJoint.L_Ankle, SmplJoint.L_Foot),

        // Right leg
        (SmplJoint.Pelvis, SmplJoint.R_Hip),
        (SmplJoint.R_Hip, SmplJoint.R_Knee),
        (SmplJoint.R_Knee, SmplJoint.R_Ankle),
        (SmplJoint.R_Ankle, SmplJoint.R_Foot),

        // Left arm
        (SmplJoint.Spine3, SmplJoint.L_Collar),
        (SmplJoint.L_Collar, SmplJoint.L_Shoulder),
        (SmplJoint.L_Shoulder, SmplJoint.L_Elbow),
        (SmplJoint.L_Elbow, SmplJoint.L_Wrist),
        (SmplJoint.L_Wrist, SmplJoint.L_Hand),

        // Right arm
        (SmplJoint.Spine3, SmplJoint.R_Collar),
        (SmplJoint.R_Collar, SmplJoint.R_Shoulder),
        (SmplJoint.R_Shoulder, SmplJoint.R_Elbow),
        (SmplJoint.R_Elbow, SmplJoint.R_Wrist),
        (SmplJoint.R_Wrist, SmplJoint.R_Hand),
    };

    void Start()
    {
        // Read and parse JSON file
        string jsonPath = Path.Combine(Application.streamingAssetsPath, "subject_0.json");
        string jsonContent = File.ReadAllText(jsonPath);
        
        // Parse the full subject data
        var subjectData = JsonConvert.DeserializeObject<SubjectData>(jsonContent);
        foreach (var kvp in subjectData.frames)
        {
            framePoses[int.Parse(kvp.Key)] = kvp.Value;
        }

        // Create body joint visualization
        bodyJointSpheres = new GameObject[24]; // SMPL model has 24 joints
        for (int i = 0; i < 24; i++)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = $"BodyJoint_{i}";
            sphere.transform.localScale = Vector3.one * 0.02f; // 2cm diameter
            sphere.GetComponent<Renderer>().material.color = Color.green;
            sphere.transform.parent = transform;
            bodyJointSpheres[i] = sphere;
        }

        // Create hand visualizations
        leftHandTransform = new GameObject("LeftHandTransform");
        rightHandTransform = new GameObject("RightHandTransform");
        leftHandTransform.transform.parent = transform;
        rightHandTransform.transform.parent = transform;

        leftHandSpheres = CreateHandSpheres(Color.red, leftHandTransform.transform);
        rightHandSpheres = CreateHandSpheres(Color.blue, rightHandTransform.transform);
        
        leftHandLines = CreateHandLines(Color.red, leftHandTransform.transform);
        rightHandLines = CreateHandLines(Color.blue, rightHandTransform.transform);

        bodyLines = CreateBodyLines(Color.green, transform);
    }

    static GameObject[] CreateHandSpheres(Color color, Transform parent)
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

    static LineRenderer[] CreateHandLines(Color color, Transform parent)
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

    static LineRenderer[] CreateBodyLines(Color color, Transform parent)
    {
        LineRenderer[] lines = new LineRenderer[bodyConnections.Length];
        for (int i = 0; i < bodyConnections.Length; i++)
        {
            GameObject lineObj = new($"BodyLine_{i}");
            lineObj.transform.parent = parent;
            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.startWidth = 0.01f;
            line.endWidth = 0.01f;
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
            if (isPlaying && !framePoses.ContainsKey(currentFrame))
            {
                currentFrame = 0; // Reset to start if at end
            }
        }

        if (isPlaying && framePoses.ContainsKey(currentFrame))
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= frameInterval)
            {
                UpdateHandPositions();
                currentFrame++;
                if (!framePoses.ContainsKey(currentFrame))
                {
                    currentFrame = 0; // Loop back to start
                }
                frameTimer = 0f;
            }
        }
    }

    void UpdateHandPositions()
    {
        if (!framePoses.TryGetValue(currentFrame, out FrameData frame))
        {
            Debug.LogWarning($"No data for frame {currentFrame}");
            return;
        }

        // Update body joints and lines first
        UpdateFullBodyJointsAndLines(frame.body_joints, bodyJointSpheres, bodyLines);

        // Get wrist positions from body joints
        Vector3 leftWristPos = bodyJointSpheres[(int)SmplJoint.L_Wrist].transform.position;
        Vector3 rightWristPos = bodyJointSpheres[(int)SmplJoint.R_Wrist].transform.position;

        // Update hand parent transforms to wrist positions
        leftHandTransform.transform.position = leftWristPos;
        rightHandTransform.transform.position = rightWristPos;

        // Update hands with their world positions
        if (frame.left_hand != null)
        {
            var joints = frame.left_hand.Select(j => new Vector3(j[0], j[1], j[2])).ToList();
            UpdateHandJointsAndLines(joints, leftHandSpheres, leftHandLines, leftWristPos);
        }

        if (frame.right_hand != null)
        {
            var joints = frame.right_hand.Select(j => new Vector3(j[0], j[1], j[2])).ToList();
            UpdateHandJointsAndLines(joints, rightHandSpheres, rightHandLines, rightWristPos);
        }
    }

    static void UpdateHandJointsAndLines(List<Vector3> joints, GameObject[] spheres, LineRenderer[] lines, Vector3 wristPos)
    {
        if (joints.Count == 0) return;

        // Calculate the offset from the first joint (wrist) to the body wrist position
        Vector3 offset = wristPos - joints[0];

        // Apply offset to all joints to maintain relative positions while matching body position
        var worldJoints = joints.Select(j => j + offset).ToArray();

        // Update spheres with world positions
        for (int i = 0; i < worldJoints.Length && i < spheres.Length; i++)
        {
            spheres[i].transform.position = worldJoints[i];
        }

        // Update lines with world positions
        for (int i = 0; i < fingerConnections.Length && i < lines.Length; i++)
        {
            (HandJoint start, HandJoint end) = fingerConnections[i];
            int startIdx = (int)start;
            int endIdx = (int)end;
            if (startIdx < worldJoints.Length && endIdx < worldJoints.Length)
            {
                lines[i].SetPosition(0, worldJoints[startIdx]);
                lines[i].SetPosition(1, worldJoints[endIdx]);
            }
        }
    }

    static void UpdateFullBodyJointsAndLines(List<List<float>> jointData, GameObject[] spheres, LineRenderer[] lines)
    {
        var worldJoints = new Vector3[jointData.Count];
        for (int i = 0; i < jointData.Count; i++)
        {
            var joint = jointData[i];
            Vector3 position = new(joint[0], joint[1], joint[2]);
            worldJoints[i] = position;
            if (i < spheres.Length)
            {
                spheres[i].transform.position = position;
            }
        }

        for (int i = 0; i < bodyConnections.Length && i < lines.Length; i++)
        {
            (SmplJoint start, SmplJoint end) = bodyConnections[i];
            int startIdx = (int)start;
            int endIdx = (int)end;
            if (startIdx < worldJoints.Length && endIdx < worldJoints.Length)
            {
                lines[i].SetPosition(0, worldJoints[startIdx]);
                lines[i].SetPosition(1, worldJoints[endIdx]);
            }
        }
    }
}
