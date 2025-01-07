using System;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class JointPosition
{
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }

    public Vector3 ToVector3() => new(x, y, z);
}

[Serializable]
public class FrameData
{
    public int frame { get; set; }
    public List<List<float>> body_joints { get; set; }
    [JsonProperty("left_hand")]
    public List<List<float>> left_hand { get; set; }
    [JsonProperty("right_hand")]
    public List<List<float>> right_hand { get; set; }
}

[Serializable]
public class SubjectData
{
    public Dictionary<string, FrameData> frames { get; set; }
}