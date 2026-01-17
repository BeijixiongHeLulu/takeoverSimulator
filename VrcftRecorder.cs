using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using System;
using System.Collections.Generic;

public class VrcftRecorder : MonoBehaviour
{
    [Header("设置")]
    public int listenPort = 9000;
    public string fileName = "EyeTracking_Full";

    // --- 核心变量 ---
    private float _leftEyeX, _leftEyeY;
    private float _rightEyeX, _rightEyeY;
    private float _leftOpenness, _rightOpenness;

    // 这一帧收到的原始 Hex 数据
    private string _currentFrameHex = "";
    // 这一帧解析到的所有参数 (JSON 风格字符串，防止遗漏未知参数)
    private string _currentFrameAllParams = "";

    // 线程控制
    private UdpClient _udpClient;
    private Thread _receiveThread;
    private bool _isRunning = false;
    private string _filePath;

    // 线程安全锁
    private object _lock = new object();
    private bool _newDataAvailable = false;

    void Start()
    {
        string folder = Path.Combine(Application.dataPath, "../ExperimentLogs");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        string timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        _filePath = Path.Combine(folder, $"{fileName}_{timeStamp}.csv");

        // 写入表头：
        // 前面是方便分析的常用列
        // AllParameters: 记录所有解析到的地址和值
        // RawHex: 记录最原始的二进制数据
        string header = "Time,LeftGazeX,LeftGazeY,RightGazeX,RightGazeY,LeftOpenness,RightOpenness,AllParameters,RawHex\n";
        File.WriteAllText(_filePath, header);

        Debug.Log($"[Recorder Pro] 全量数据记录模式已启动: {_filePath}");

        _isRunning = true;
        _receiveThread = new Thread(ReceiveLoop);
        _receiveThread.IsBackground = true;
        _receiveThread.Start();
    }

    void Update()
    {
        // 如果后台线程收到了新数据，就在主线程写入 CSV
        // (为了保证时间戳和 Unity 物理帧对齐)
        lock (_lock)
        {
            if (_newDataAvailable)
            {
                LogData();
                _newDataAvailable = false;
            }
        }
    }

    void LogData()
    {
        // 格式化 CSV 行
        // 注意：AllParameters 里面可能包含逗号，所以我们用引号包起来，或者把逗号换成 |
        string cleanParams = _currentFrameAllParams.Replace(",", "|");

        string line = string.Format("{0:F4},{1:F4},{2:F4},{3:F4},{4:F4},{5:F4},{6:F4},{7},{8}\n",
            Time.time,
            _leftEyeX, _leftEyeY,
            _rightEyeX, _rightEyeY,
            _leftOpenness, _rightOpenness,
            cleanParams,    // 这一列记录所有解析到的键值对
            _currentFrameHex // 这一列记录原始 Hex
        );

        File.AppendAllText(_filePath, line);
    }

    void ReceiveLoop()
    {
        _udpClient = new UdpClient(listenPort);
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, listenPort);

        while (_isRunning)
        {
            try
            {
                byte[] data = _udpClient.Receive(ref remoteEP);
                if (data.Length > 0)
                {
                    lock (_lock)
                    {
                        // 1. 保存原始 HEX (不带横杠，节省空间)
                        _currentFrameHex = BitConverter.ToString(data).Replace("-", "");

                        // 2. 清空参数记录器
                        _currentFrameAllParams = "";

                        // 3. 解析数据
                        if (IsBundle(data)) ParseBundle(data);
                        else
                        {
                            int index = 0;
                            ParseMessage(data, ref index, data.Length);
                        }

                        _newDataAvailable = true;
                    }
                }
            }
            catch (Exception) { }
        }
    }

    // --- 核心解析逻辑 ---

    bool IsBundle(byte[] data)
    {
        if (data.Length < 8) return false;
        return Encoding.ASCII.GetString(data, 0, 7) == "#bundle";
    }

    void ParseBundle(byte[] data)
    {
        int index = 16; // 跳过 #bundle + TimeTag
        while (index < data.Length)
        {
            int size = ReadBigEndianInt(data, ref index);
            if (index + size > data.Length) break;
            ParseMessage(data, ref index, size);
        }
    }

    void ParseMessage(byte[] data, ref int start, int length)
    {
        int end = start + length;
        try
        {
            string address = ReadOscString(data, ref start);
            string typeTag = ReadOscString(data, ref start);

            // 记录到 AllParameters 列，格式如: [地址:数值]
            string paramLog = "";

            if (address == "/tracking/eye/LeftRightPitchYaw" && typeTag == ",ffff")
            {
                float val1 = ReadBigEndianFloat(data, ref start);
                float val2 = ReadBigEndianFloat(data, ref start);
                float val3 = ReadBigEndianFloat(data, ref start);
                float val4 = ReadBigEndianFloat(data, ref start);

                _leftEyeX = val1; _leftEyeY = val2;
                _rightEyeX = val3; _rightEyeY = val4;

                paramLog = $"[Gaze4:{val1:F3}|{val2:F3}|{val3:F3}|{val4:F3}]";
            }
            else if (address == "/tracking/eye/EyesClosedAmount" && typeTag.Length >= 2)
            {
                float val = ReadBigEndianFloat(data, ref start);
                _leftOpenness = 1.0f - val;
                _rightOpenness = 1.0f - val;

                paramLog = $"[Blink:{val:F3}]";
            }
            else
            {
                // 🔥 如果发现了未知的地址，也会被记录下来！
                // 尝试读取第一个 float (如果是 float 的话)
                if (typeTag.Length >= 2 && typeTag[1] == 'f')
                {
                    float val = ReadBigEndianFloat(data, ref start);
                    paramLog = $"[UNKNOWN:{address}={val:F3}]";
                }
                else
                {
                    paramLog = $"[UNKNOWN_TYPE:{address}]";
                }

                // 跳过剩余部分
                start = end;
            }

            // 追加到全量记录字符串
            _currentFrameAllParams += paramLog;
        }
        catch
        {
            start = end;
        }
    }

    // --- 辅助读取 ---
    int ReadBigEndianInt(byte[] data, ref int start)
    {
        if (start + 4 > data.Length) return 0;
        byte[] bytes = { data[start + 3], data[start + 2], data[start + 1], data[start] };
        start += 4;
        return BitConverter.ToInt32(bytes, 0);
    }
    float ReadBigEndianFloat(byte[] data, ref int start)
    {
        if (start + 4 > data.Length) return 0;
        byte[] bytes = { data[start + 3], data[start + 2], data[start + 1], data[start] };
        start += 4;
        return BitConverter.ToSingle(bytes, 0);
    }
    string ReadOscString(byte[] data, ref int start)
    {
        int end = start;
        while (end < data.Length && data[end] != 0) end++;
        string str = Encoding.ASCII.GetString(data, start, end - start);
        start = end + 1;
        while (start % 4 != 0) start++;
        return str;
    }

    void OnDestroy()
    {
        _isRunning = false;
        if (_udpClient != null) _udpClient.Close();
        if (_receiveThread != null) _receiveThread.Abort();
    }
}