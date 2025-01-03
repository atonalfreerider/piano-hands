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
public class SingleHandData
{
    public List<JointPosition> joints { get; set; }
    public JointPosition wrist_orientation { get; set; }
}

[Serializable]
public class HandFrameData
{
    [JsonProperty("left_hands")]
    public List<SingleHandData> left_hands { get; set; } = new();

    [JsonProperty("right_hands")]
    public List<SingleHandData> right_hands { get; set; } = new();

    public (List<Vector3>, Vector3?) GetLeftHandJoints()
    {
        return ConvertToVector3List(left_hands);
    }

    public (List<Vector3>, Vector3?) GetRightHandJoints()
    {
        return ConvertToVector3List(right_hands);
    }

    private static (List<Vector3>, Vector3?) ConvertToVector3List(List<SingleHandData> handData)
    {
        var joints = new List<Vector3>();
        Vector3? wristOrientation = null;

        if (handData?.Count > 0 && handData[0]?.joints != null)
        {
            foreach (var joint in handData[0].joints)
            {
                joints.Add(joint.ToVector3());
            }
            
            if (handData[0].wrist_orientation != null)
            {
                wristOrientation = handData[0].wrist_orientation.ToVector3();
            }
        }

        return (joints, wristOrientation);
    }
}