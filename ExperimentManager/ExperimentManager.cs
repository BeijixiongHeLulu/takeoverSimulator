using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PathCreation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.Utility;

[DisallowMultipleComponent]
public class ExperimentManager : MonoBehaviour
{
    #region Fields

    public static ExperimentManager Instance { get; private set; }

    [Space] [Header("Necessary Elements")]
    private GameObject _participantsCar;
    [Tooltip("0 to 10 seconds")] [Range(0, 10)] [SerializeField] private float startExperimentDelay = 3f;
    [Tooltip("0 to 10 seconds")] [Range(0, 10)] [SerializeField] private float respawnDelay = 5f;

    private enum Scene
    {
        MainMenu,
        Experiment
    }
    
    private List<ActivationTrigger> _activationTriggers;
    private CriticalEventController _criticalEventController;
    private Vector3 _respawnPosition;
    private Quaternion _respawnRotation;
    private Scene _scene;
    private bool _activatedEvent;
    private bool _vRScene;
    private bool _isStartPressed;
    
    #endregion

    #region Private Methods
    
    private void Awake()
    {
        _activationTriggers = new List<ActivationTrigger>();
        
        //singleton pattern a la Unity
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if (SavingManager.Instance != null)
        {
            SavingManager.Instance.SetParticipantCar(_participantsCar);    
        }
    }

    public void OnSceneLoaded()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            AssignParticipantsCar();
            RunMainMenu();
        }
    }


    private void Start()
    {
        _vRScene = CalibrationManager.Instance.GetVRActivationState();
        
        if (_activationTriggers.Count == 0)
        {
            Debug.Log("<color=red>Error: </color>Please ensure that ActivationTrigger is being executed before ExperimentManager if there are triggers present in the scene.");
        }

        if (EyetrackingManager.Instance == null)
        {
            Debug.Log("<color=red>Error: </color>EyetrackingManager should be present in the scene.");
        }
        
        if (CalibrationManager.Instance == null)
        {
            Debug.Log("<color=red>Error: </color>CalibrationManager should be present in the scene.");
        }
        
        if (SavingManager.Instance == null)
        {
            Debug.Log("<color=red>Error: </color>SavingManager should be present in the scene.");
        }
        
        if (CameraManager.Instance == null)
        {
            Debug.Log("<color=red>Error: </color>CameraManager should be present in the scene.");
        }
        
        try
        {
            InformTriggers();
            AssignParticipantsCar();
            RunMainMenu();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e);
            throw;
        }
    }

    private void RunMainMenu()
    {
        _scene = Scene.MainMenu;
        _participantsCar.GetComponent<Rigidbody>().isKinematic = true;
        _participantsCar.GetComponent<CarController>().TurnOffEngine();
    }
    
    // inform all triggers to disable their game objects at the beginning of the experiment
    private void InformTriggers()
    {
        foreach (var trigger in _activationTriggers)
        {
            trigger.DeactivateTheGameObjects();
        }
    }

    // starting the experiment
    // 把原来的 StartExperiment 替换成这个带监控的版本
    private IEnumerator StartExperiment()
    {
        Debug.Log("【流程监控】1. 实验协程启动...");

        // 1. 检查 TimeManager
        if (TimeManager.Instance == null)
        {
            Debug.LogError("【严重错误】TimeManager 没找到！协程在此中断。");
            yield break;
        }
        TimeManager.Instance.SetExperimentStartTime();
        _isStartPressed = true;

        // 2. 检查场景加载
        Debug.Log("【流程监控】2. 等待场景加载...");
        while (SceneLoadingHandler.Instance.GetAdditiveLoadingState())
        {
            // 如果这里死循环，说明 GetAdditiveLoadingState 一直是 true
            yield return null;
        }

        _scene = Scene.Experiment;

        // 3. 检查 SavingManager 和 CameraManager
        Debug.Log("【流程监控】3. 开始录制数据...");
        if (SavingManager.Instance == null) Debug.LogError("【严重错误】SavingManager 缺失！");
        else SavingManager.Instance.StartRecordingData();

        if (CameraManager.Instance == null) Debug.LogError("【严重错误】CameraManager 缺失！");
        else CameraManager.Instance.FadeIn();

        // 4. 倒计时
        Debug.Log($"【流程监控】4. 倒计时 {startExperimentDelay} 秒...");
        yield return new WaitForSeconds(startExperimentDelay);

        // 5. 关键一步
        Debug.Log("【流程监控】5. >>> 解锁车辆物理！ <<<");
        if (_participantsCar == null)
        {
            Debug.LogError("【严重错误】_participantsCar 是空的！无法解锁物理。");
        }
        else
        {
            _participantsCar.GetComponent<Rigidbody>().isKinematic = false;
            _participantsCar.GetComponent<CarController>().TurnOnEngine();
            Debug.Log("【流程监控】成功：引擎已启动，物理已解锁。");
        }
    }

    private IEnumerator ReSpawnParticipant(float seconds)
    {
        _participantsCar.GetComponent<Rigidbody>().velocity = Vector3.zero;
        _participantsCar.GetComponent<Rigidbody>().isKinematic = true;
        yield return new WaitForSeconds(seconds);
        _participantsCar.GetComponent<Rigidbody>().isKinematic = false;
        
        // ConditionManager.Instance.EndEvent(false);

        CameraManager.Instance.AlphaFadeIn();
        _participantsCar.GetComponent<CarController>().TurnOnEngine();
    }

    private void AssignParticipantsCar()
    {
        // 1. 根据场景名找到车辆引用
        switch (SceneManager.GetActiveScene().name)
        {
            case "SceneLoader":
                _participantsCar = SceneLoadingSceneManager.Instance.GetParticipantsCar();
                break;
            case "MountainRoad":
                _participantsCar = MountainRoadManager.Instance.GetParticipantsCar();
                break;
            case "Westbrueck":
                _participantsCar = WestbrueckManager.Instance.GetParticipantsCar();
                break;
            case "CountryRoad":
                _participantsCar = CountryRoadManager.Instance.GetParticipantsCar();
                break;
            case "Autobahn":
                _participantsCar = AutobahnManager.Instance.GetParticipantsCar();
                break;
        }

        // 2. 这里的检查至关重要：确保车找到了
        if (_participantsCar == null)
        {
            Debug.LogError("【严重错误】在当前场景中找不到 ParticipantsCar！请检查 SceneManagers。");
            return;
        }

        // 3. 将车辆引用传递给其他系统
        PersistentTrafficEventManager.Instance.SetParticipantsCar(_participantsCar);

        // ★★★【修复核心】：必须把车传给 SavingManager，否则 InputRecorder 会报错崩溃 ★★★
        if (SavingManager.Instance != null)
        {
            SavingManager.Instance.SetParticipantCar(_participantsCar);
            Debug.Log("【系统连接】已将车辆引用传递给 SavingManager。");
        }
        else
        {
            Debug.LogError("【严重错误】SavingManager 缺失！数据无法记录。");
        }
    }

    #endregion

    #region Public Methods

    public void ParticipantFailed()
    {
        _activatedEvent = false;

        CameraManager.Instance.AlphaFadeOut();
        
        ConditionManager.Instance.EndEvent(false); // todo check

        PersistentTrafficEventManager.Instance.FinalizeEvent();
        _participantsCar.GetComponent<CarController>().TurnOffEngine();
        _participantsCar.GetComponent<Rigidbody>().isKinematic = true;
        _participantsCar.GetComponent<Rigidbody>().velocity = Vector3.zero;
        _participantsCar.transform.SetPositionAndRotation(_respawnPosition, _respawnRotation);
        CameraManager.Instance.RespawnBehavior();
        _participantsCar.GetComponent<Rigidbody>().isKinematic = false;
        _participantsCar.GetComponent<AIController>().SetLocalTargetAndCurveDetection();
        StartCoroutine(ReSpawnParticipant(respawnDelay));
    }
    
    // ending the experiment
    public void EndOfExperiment()
    {        
        CameraManager.Instance.FadeOut();

        _participantsCar.transform.parent.gameObject.SetActive(false);
        
        CalibrationManager.Instance.URIRequest();
        CalibrationManager.Instance.ExperimentEnded();
        SceneManager.LoadSceneAsync("MainMenu");
    }
    
    // Reception desk for ActivationTriggers to register themselves
    public void RegisterToExperimentManager(ActivationTrigger listener)
    {
        _activationTriggers.Add(listener);
    }

    #endregion
    
    #region Setters

    public void SetRespawnPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        _respawnPosition = position;
        _respawnRotation = rotation;
    }
    
    public void SetInitialTransform(Vector3 position, Quaternion rotation)
    {
        _participantsCar.transform.SetPositionAndRotation(position, rotation);
    }
    
    public void SetInitialTransform(Vector3 position)
    {
        _participantsCar.transform.SetPositionAndRotation(position, _participantsCar.transform.rotation);
    }

    public void SetCarPath(PathCreator newPath)
    {
        _participantsCar.GetComponent<AIController>().SetNewPath(newPath);
    }

    public void SetEventActivationState(bool activationState)
    {
        _activatedEvent = activationState;
    }
    
    public void SetParticipantsCar(GameObject car)
    {
        _participantsCar = car;
    }
    
    public void SetController(CriticalEventController criticalEventController)
    {
        _criticalEventController = criticalEventController;
    }

    #endregion

    #region Getters

    public bool GetEventActivationState()
    {
        return _activatedEvent;
    }

    public GameObject GetSeatPosition()
    {
        return _participantsCar.GetComponent<CarController>().GetSeatPosition();
    }

    public GameObject GetParticipantsCar()
    {
        return _participantsCar;
    }

    #endregion
    
    #region GUI

    public void OnGUI()
    {
        float height = Screen.height;
        float width = Screen.width;
        
        float xForButtons = width / 12f;
        float yForButtons = height / 7f;
        
        float xForLable = (width / 12f);
        float yForLable = height / 1.35f;

        float buttonWidth = 200f;
        float buttonHeight = 30f;
        float heightDifference = 40f;
        
        int labelFontSize = 33;

        
        // Lable
        GUI.color = Color.white;
        GUI.skin.label.fontSize = labelFontSize;
        GUI.skin.label.fontStyle = FontStyle.Bold;
        
        // Buttons
        GUI.backgroundColor = Color.cyan;
        GUI.color = Color.white;
        
        if (_scene == Scene.MainMenu)
        {
            if (!_isStartPressed)
            {
                GUI.Label(new Rect(xForLable, yForLable, 500, 100),  "Main Experiment");

                if (GUI.Button(new Rect(xForButtons, yForButtons, buttonWidth, buttonHeight), "Start"))
                {
                    StartCoroutine(StartExperiment());
                }
            }

            if (_isStartPressed && _scene != Scene.Experiment)
            {
                GUI.Label(new Rect(width / 4f, height / 8f, 500, 100),  "Main Experiment is Loading...");
            }
            
            // Reset Button
            GUI.backgroundColor = Color.red;
            GUI.color = Color.white;
        
            if (GUI.Button(new Rect(xForButtons*9, yForButtons, buttonWidth, buttonHeight), "Abort"))
            {
                SavingManager.Instance.StopAndSaveData(SceneManager.GetActiveScene().name);
                CalibrationManager.Instance.AbortExperiment();
            }
        } 
        else if (_scene == Scene.Experiment)
        {
            // GUI.backgroundColor = Color.red;
            GUI.color = Color.white;
            
            /*if (GUI.Button(new Rect(xForButtons*9, yForButtons, buttonWidth, buttonHeight), "End"))
            {
                SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
                _scene = Scene.MainMenu;
            }*/

            if (_activatedEvent)
            {
                GUI.backgroundColor = Color.magenta;

                if (GUI.Button(new Rect(xForButtons, yForButtons, buttonWidth, buttonHeight), "Respawn Manually"))
                {
                    ParticipantFailed();
                }
            }
        }
    }

    #endregion
}