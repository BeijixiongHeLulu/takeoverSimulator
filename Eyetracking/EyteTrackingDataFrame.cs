using UnityEngine;
using System;

[Serializable]
public class EyteTrackingDataFrame
{
    public float TimeStamp;

    // 头部数据
    public Vector3 HeadPosition;
    public Quaternion HeadRotation;

    // 眼动核心数据
    public Vector3 GazeDirection; // 结合视线方向
    public Vector3 GazeOrigin;    // 视线起点

    // 眼睛开合度 (0=闭, 1=睁)
    public float LeftEyeOpenness;
    public float RightEyeOpenness;

    // 瞳孔直径 (毫米)
    public float LeftPupilDiameter;
    public float RightPupilDiameter;
}