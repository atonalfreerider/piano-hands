using System;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class HandFrameData
{
    [JsonProperty("left_hands")]
    public double[][][][] left_hands { get; set; } = Array.Empty<double[][][]>();

    [JsonProperty("right_hands")]
    public double[][][][] right_hands { get; set; } = Array.Empty<double[][][]>();

    public List<Vector3> GetLeftHandJoints()
    {
        return ConvertToVector3List(left_hands);
    }

    public List<Vector3> GetRightHandJoints()
    {
        return ConvertToVector3List(right_hands);
    }

    private List<Vector3> ConvertToVector3List(double[][][][] handData)
    {
        List<Vector3> joints = new List<Vector3>();
        if (handData?.Length > 0 && 
            handData[0]?.Length > 0 && 
            handData[0][0]?.Length > 0)
        {
            double[][] jointArray = handData[0][0];
            foreach (double[] joint in jointArray)
            {
                if (joint?.Length >= 3)
                {
                    joints.Add(new Vector3(
                        (float)joint[0],
                        (float)joint[1],
                        (float)joint[2]
                    ));
                }
            }
        }
        return joints;
    }
}