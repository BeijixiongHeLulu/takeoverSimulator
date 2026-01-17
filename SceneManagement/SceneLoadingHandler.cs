using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class SceneLoadingHandler : MonoBehaviour
{
    public static SceneLoadingHandler Instance;

    [Header("Settings")]
    public GameObject participantsCar;
    public bool isAdditiveLoading = false;

    // 如果调用 LoadExperimentScenes() 不传参，默认加载这个场景
    public string defaultExperimentSceneName = "Westbrueck";

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
    }

    public GameObject GetParticipantsCar()
    {
        if (participantsCar == null)
        {
            participantsCar = GameObject.FindGameObjectWithTag("Player");
            if (participantsCar == null)
                participantsCar = GameObject.Find("ParticipantsCar");
        }
        return participantsCar;
    }

    public bool GetAdditiveLoadingState()
    {
        return isAdditiveLoading;
    }

    // ★★★ 修复 CS7036: 添加默认参数 ★★★
    public void LoadExperimentScenes(string sceneName = "")
    {
        // 如果传入空字符串，使用默认场景名
        string targetScene = string.IsNullOrEmpty(sceneName) ? defaultExperimentSceneName : sceneName;
        StartCoroutine(LoadSceneSequence(targetScene));
    }

    public void SceneChange(string sceneName)
    {
        LoadExperimentScenes(sceneName);
    }

    private IEnumerator LoadSceneSequence(string sceneName)
    {
        isAdditiveLoading = true;
        Debug.Log($"[SceneLoadingHandler] Loading: {sceneName}");

        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.FadeOut(1.0f);
            yield return new WaitForSeconds(1.0f);
        }

        // 简单的场景加载逻辑
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"[SceneLoadingHandler] Scene '{sceneName}' not found in Build Settings!");
            isAdditiveLoading = false;
            if (CameraManager.Instance != null) CameraManager.Instance.FadeIn(1.0f);
            yield break;
        }

        op.allowSceneActivation = false;
        while (op.progress < 0.9f)
        {
            yield return null;
        }
        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            yield return null;
        }

        Debug.Log($"[SceneLoadingHandler] Loaded: {sceneName}");

        // 重置状态
        participantsCar = null;
        GetParticipantsCar();
        isAdditiveLoading = false;

        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.FadeIn(1.0f);
        }
    }
}