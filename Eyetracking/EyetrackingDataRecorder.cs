using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EyetrackingDataRecorder : MonoBehaviour
{
    private List<EyteTrackingDataFrame> _dataFrames;
    private bool _isRecording = false;
    private float _timer;
    private float _timeStep;

    private void Start()
    {
        _dataFrames = new List<EyteTrackingDataFrame>();
        _timeStep = 1f / 90f; // 90Hz 采样率
    }

    public void StartRecording()
    {
        _dataFrames.Clear();
        _isRecording = true;
        Debug.Log("[Recorder] Eye Tracking Started.");
    }

    public void StopRecording()
    {
        _isRecording = false;
        Debug.Log($"[Recorder] Stopped. Frames captured: {_dataFrames.Count}");
    }

    private void Update()
    {
        if (!_isRecording) return;

        _timer += Time.deltaTime;
        if (_timer >= _timeStep)
        {
            RecordFrame();
            _timer = 0f;
        }
    }

    private void RecordFrame()
    {
        // 现在可以直接通过 Instance 找到我们刚才写的 Manager 了
        if (EyetrackingManager.Instance == null) return;

        EyteTrackingDataFrame frame = new EyteTrackingDataFrame();

        // 修复 CS1061 报错：直接使用 Unity 运行时间，不再依赖 TimeManager
        frame.TimeStamp = Time.time;

        // 从 Manager 获取数据
        frame.GazeDirection = EyetrackingManager.Instance.combineGazeVector;
        frame.GazeOrigin = EyetrackingManager.Instance.combineGazeOrigin;
        frame.LeftEyeOpenness = EyetrackingManager.Instance.leftEyeOpenness;
        frame.RightEyeOpenness = EyetrackingManager.Instance.rightEyeOpenness;
        frame.LeftPupilDiameter = EyetrackingManager.Instance.leftPupilDiameter;
        frame.RightPupilDiameter = EyetrackingManager.Instance.rightPupilDiameter;

        // 记录头显位置
        if (Camera.main != null)
        {
            frame.HeadPosition = Camera.main.transform.position;
            frame.HeadRotation = Camera.main.transform.rotation;
        }

        _dataFrames.Add(frame);
    }

    public List<EyteTrackingDataFrame> GetDataFrames()
    {
        return _dataFrames;
    }
}