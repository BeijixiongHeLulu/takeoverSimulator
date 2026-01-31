using UnityEngine;
using Unity.XR.PXR; // PICO SDK
using System.Collections.Generic;

public class EyetrackingManager : MonoBehaviour
{
    public static EyetrackingManager Instance;
    public EyetrackingDataRecorder dataRecorder;

    [Header("Realtime Data (Debug)")]
    public Vector3 combineGazeVector;
    public Vector3 combineGazeOrigin;
    public float leftEyeOpenness;
    public float rightEyeOpenness;
    public float leftPupilDiameter = -1f;
    public float rightPupilDiameter = -1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if (dataRecorder == null)
            dataRecorder = GetComponent<EyetrackingDataRecorder>();
        if (dataRecorder == null)
            dataRecorder = FindObjectOfType<EyetrackingDataRecorder>();
    }

    void Update()
    {
        if (PXR_Manager.Instance == null)
        {
            return;
        }
        // 1. 获取注视数据
        PXR_EyeTracking.GetCombineEyeGazeVector(out combineGazeVector);
        PXR_EyeTracking.GetCombineEyeGazePoint(out combineGazeOrigin);

        // 2. 获取开合度
        PXR_EyeTracking.GetLeftEyeGazeOpenness(out leftEyeOpenness);
        PXR_EyeTracking.GetRightEyeGazeOpenness(out rightEyeOpenness);

        // 3. 尝试获取瞳孔 (PC串流可能无效)
        leftPupilDiameter = -1f; // 占位
        rightPupilDiameter = -1f;
    }

    // --- 兼容旧代码的方法 ---

    public void StartRecording()
    {
        if (dataRecorder != null) dataRecorder.StartRecording();
    }

    public void StopRecording()
    {
        if (dataRecorder != null) dataRecorder.StopRecording();
    }

    public List<EyteTrackingDataFrame> GetEyeTrackingData()
    {
        if (dataRecorder != null) return dataRecorder.GetDataFrames();
        return new List<EyteTrackingDataFrame>();
    }

    public float GetAverageSceneFPS()
    {
        return 1.0f / Time.smoothDeltaTime;
    }

    public void StartCalibration()
    {
        Debug.Log("[PICO] System calibration handles this.");
    }

    // ★★★ 核心修改：返回值改为 Vector3 ★★★
    public Vector3 GetEyeValidationErrorAngles()
    {
        // 由于没有外置校准流程，返回零向量表示无误差
        return Vector3.zero;
    }
}