using UnityEngine;

public class ChaseCam : MonoBehaviour
{
    private GameObject _objectToFollow;
    [Range(0f, 10f)] public float damping = 3.0f;

    private void LateUpdate()
    {
        // 1. 安全检查：相机管理器是否存在？
        if (CameraManager.Instance == null) return;

        // 2. 获取目标
        GameObject target = CameraManager.Instance.GetObjectToFollow();
        if (target == null)
        {
            // 还没有找到车，静默等待，不要报错
            return;
        }
        _objectToFollow = target;

        // 3. 获取控制器（带防空检查）
        CarController carController = _objectToFollow.GetComponent<CarController>();

        // 4. 决定跟随点
        Vector3 targetPos;
        Quaternion targetRot;

        if (carController != null && carController.GetSeatPosition() != null)
        {
            // 完美情况：有车，有座位
            targetPos = carController.GetSeatPosition().transform.position;
        }
        else
        {
            // 降级情况：有车，但没座位（防止报错！）
            // 临时跟随车身中心，并打印一次警告帮助排查
            if (carController != null && carController.GetSeatPosition() == null)
            {
                Debug.LogWarning($"[ChaseCam] 警告：车辆 {target.name} 的 Seat Position 是空的！请检查 Prefab。");
            }
            targetPos = target.transform.position;
        }
        targetRot = _objectToFollow.transform.rotation;

        // 5. 执行移动
        this.transform.position = targetPos;
        this.transform.rotation = Quaternion.Lerp(this.transform.rotation, targetRot, Time.deltaTime * damping);
    }

    public void ForceChaseCamRotation()
    {
        if (_objectToFollow != null)
            this.transform.rotation = _objectToFollow.transform.rotation;
    }
}