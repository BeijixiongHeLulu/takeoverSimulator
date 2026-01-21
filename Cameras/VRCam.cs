using UnityEngine;

public class VRCam : MonoBehaviour
{
    [Header("--- 诊断面板 ---")]
    public string status = "初始化中...";
    public Transform targetSeat;
    public Transform foundCar;

    [Header("--- 设置 ---")]
    public KeyCode recenterKey = KeyCode.R; // 按 R 键校准方向

    // 内部变量
    private Camera _vrCamera;
    private Quaternion _rotationOffset = Quaternion.identity; // 记录校准时的偏差

    void Start()
    {
        _vrCamera = GetComponentInChildren<Camera>();
        if (_vrCamera == null) Debug.LogError("VRCam: 找不到子物体Camera！");
        Invoke("FindSeat", 0.5f); // 延迟一点找，给Manager一点时间
    }

    void Update()
    {
        // 允许随时按 R 键重置视角中心
        if (Input.GetKeyDown(recenterKey)) Recenter();
    }

    void LateUpdate()
    {
        // 1. 还没找到座位就继续找
        if (targetSeat == null) FindSeat();

        // 2. 核心跟随逻辑
        if (targetSeat != null && _vrCamera != null)
        {
            // 位置：眼睛对齐座位
            transform.position = targetSeat.position - (transform.rotation * _vrCamera.transform.localPosition);

            // 旋转：只跟随车身，【绝对不要】减去头部旋转
            // 除非你按了 R 键，否则 _rotationOffset 是固定的
            transform.rotation = targetSeat.rotation * _rotationOffset;
        }
    }

    void Recenter()
    {
        if (targetSeat == null || _vrCamera == null) return;

        // 计算当前头显相对于车身歪了多少度
        float currentHeadY = _vrCamera.transform.localEulerAngles.y;

        // 记录这个偏差，以后一直保持这个偏差
        // 效果：现在的朝向 = 正前方
        _rotationOffset = Quaternion.Euler(0, -currentHeadY, 0);

        Debug.Log("VR 视角已校准！");
    }

    void FindSeat()
    {
        if (foundCar == null)
        {
            GameObject carObj = GameObject.FindGameObjectWithTag("Player");
            if (carObj != null) foundCar = carObj.transform;
            else if (ExperimentManager.Instance != null && ExperimentManager.Instance.GetParticipantsCar() != null)
                foundCar = ExperimentManager.Instance.GetParticipantsCar().transform;
        }

        if (foundCar != null)
        {
            // 深度查找 SeatPosition
            targetSeat = FindDeepChild(foundCar, "SeatPosition");
            if (targetSeat != null && _rotationOffset == Quaternion.identity)
            {
                Recenter(); // 第一次找到座位时，自动校准一次
            }
        }
    }

    Transform FindDeepChild(Transform parent, string name)
    {
        Transform result = parent.Find(name);
        if (result != null) return result;
        foreach (Transform child in parent)
        {
            result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.green;
        GUILayout.Label("按 'R' 重置视角中心", style);
    }
}