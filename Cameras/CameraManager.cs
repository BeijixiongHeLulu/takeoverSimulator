using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    [Header("Target Settings")]
    public Transform seatPosition;     // 驾驶座位置 (Transform)
    public GameObject objectToFollow;  // 车辆对象

    [Header("Camera Objects")]
    public Camera VRCam;       // VR相机
    public Camera NonVRCam;    // 调试用普通相机

    // 内部变量：用于黑屏淡入淡出
    private Canvas _fadeCanvas;
    private Image _fadeImage;
    private Coroutine _currentFadeRoutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupFadeCanvas(); // 初始化黑屏UI
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (objectToFollow == null)
        {
            var car = GameObject.FindGameObjectWithTag("Player");
            if (car != null)
            {
                objectToFollow = car;
                var seat = car.transform.Find("SeatPosition");
                seatPosition = seat != null ? seat : car.transform;
            }
        }
    }

    // ★★★ 修复 CS0029: VRCam 需要 GameObject ★★★
    public GameObject GetSeatPosition()
    {
        if (seatPosition != null) return seatPosition.gameObject;
        return this.gameObject;
    }

    // 兼容可能有的 GetSeatTransform 调用
    public Transform GetSeatTransform()
    {
        return seatPosition != null ? seatPosition : transform;
    }

    public GameObject GetObjectToFollow()
    {
        return objectToFollow;
    }

    public void VRModeCameraSetUp()
    {
        if (VRCam != null) VRCam.gameObject.SetActive(true);
        if (NonVRCam != null) NonVRCam.gameObject.SetActive(false);
    }

    public void NonVRModeCameraSetUp()
    {
        if (VRCam != null) VRCam.gameObject.SetActive(false);
        if (NonVRCam != null) NonVRCam.gameObject.SetActive(true);
    }

    // ★★★ 修复 CS7036: 添加默认参数 duration = 2.0f ★★★

    public void FadeOut(float duration = 2.0f)
    {
        StartFade(Color.black, duration);
    }

    public void FadeIn(float duration = 2.0f)
    {
        StartFade(Color.clear, duration);
    }

    // 兼容旧接口：AlphaFadeIn/Out
    public void AlphaFadeOut(float duration = 2.0f)
    {
        FadeOut(duration);
    }

    public void AlphaFadeIn(float duration = 2.0f)
    {
        FadeIn(duration);
    }

    public void RespawnBehavior()
    {
        if (_fadeImage != null) _fadeImage.color = Color.black;
        FadeIn(1.0f);
    }

    // --- 内部实现 ---

    private void SetupFadeCanvas()
    {
        GameObject canvasObj = new GameObject("CameraFadeCanvas");
        canvasObj.transform.SetParent(transform);
        _fadeCanvas = canvasObj.AddComponent<Canvas>();
        _fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _fadeCanvas.sortingOrder = 9999;

        CanvasGroup cg = canvasObj.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false; // 允许穿透点击

        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform);
        _fadeImage = imageObj.AddComponent<Image>();
        _fadeImage.color = Color.clear;
        _fadeImage.raycastTarget = false;

        RectTransform rt = _fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        DontDestroyOnLoad(canvasObj);
    }

    private void StartFade(Color targetColor, float duration)
    {
        if (_currentFadeRoutine != null) StopCoroutine(_currentFadeRoutine);
        _currentFadeRoutine = StartCoroutine(FadeRoutine(targetColor, duration));
    }

    private IEnumerator FadeRoutine(Color targetColor, float duration)
    {
        if (_fadeImage == null) yield break;

        Color startColor = _fadeImage.color;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            _fadeImage.color = Color.Lerp(startColor, targetColor, timer / duration);
            yield return null;
        }
        _fadeImage.color = targetColor;
    }
}