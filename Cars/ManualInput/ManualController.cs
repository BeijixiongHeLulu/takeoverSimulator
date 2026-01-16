using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

[Serializable]
public class ManualController : MonoBehaviour
{
    public enum InputType: int {Keyboard, XboxOneController, SteeringWheel}
    [HideInInspector] public InputType InputControlIndex;
    private CarController _carController;
    private bool _manualDriving = false;
    private bool toggleReverse;
    private SteeringWheelForceFeedback steeringWheelForceFeedback;
   
    private int _RealInputController;
    public delegate void OnReceivedInput(float steeringInput, float accelerationInput, float brakeInput);
    public event OnReceivedInput NotifyInputObservers;

    private float accelerationInput;
    private float brakeInput;
    private float steeringInput;
    private float reverse; //I know a bool would be better, but input systems are strange
    /*[Range(1, 5)] [SerializeField]*/ private float brakeFactor = 1.1f;


    private void Start()
    {
        if (GetComponent<SteeringWheelForceFeedback>() != null)
            steeringWheelForceFeedback = GetComponent<SteeringWheelForceFeedback>();

        _carController = GetComponent<CarController>();

        if (GetComponent<ControlSwitch>() != null)
            _manualDriving = GetComponent<ControlSwitch>().GetManualDrivingState();
        else
            _manualDriving = true;

        // 关键：拿不到配置就默认键盘，别让 Enum.Parse 把程序炸掉
        string dev = null;
        if (CalibrationManager.Instance != null)
            dev = CalibrationManager.Instance.GetSteeringInputDevice();

        SetInputSourceSafe(dev);
    }

    // Update is called once per frame
    void Update()
    {
        switch (InputControlIndex)
        {
            case InputType.Keyboard:
                // 方向键：上=油门，下=刹车，左右=转向；R 切换倒车（可选）
                accelerationInput = Input.GetKey(KeyCode.UpArrow) ? 1f : 0f;
                brakeInput = Input.GetKey(KeyCode.DownArrow) ? 1f : 0f;

                float steerTarget = 0f;
                if (Input.GetKey(KeyCode.LeftArrow)) steerTarget -= 1f;
                if (Input.GetKey(KeyCode.RightArrow)) steerTarget += 1f;

                // 直接赋值也行；MoveTowards 让转向不那么“跳”
                steeringInput = Mathf.MoveTowards(steeringInput, steerTarget, 4f * Time.deltaTime);

                if (Input.GetKeyDown(KeyCode.R))
                    toggleReverse = !toggleReverse;
                break;

            case InputType.XboxOneController:
                accelerationInput = Input.GetAxis("XOne_Trigger Right");
                steeringInput = Input.GetAxis("Horizontal");
                // 你这里 brakeInput 本来就写一半，会导致不可预期，至少先设为0
                brakeInput = 0f;
                reverse = Input.GetAxis("Fire3");
                if (reverse > 0f) toggleReverse = !toggleReverse;
                break;

            case InputType.SteeringWheel:
                steeringInput = Mathf.Clamp(Input.GetAxis("Horizontal (Steering)"), -1f, 1f);
                accelerationInput = Mathf.Clamp01(Input.GetAxis("Pedal0"));
                brakeInput = Mathf.Clamp01(Input.GetAxis("Pedal1"));
                break;
        }

        if (toggleReverse)
            accelerationInput = -accelerationInput;

        NotifyInputObservers?.Invoke(steeringInput, accelerationInput, brakeInput * brakeFactor);

        if (_manualDriving)
        {
            _carController.MoveVehicle(accelerationInput, brakeInput * brakeFactor, steeringInput);
            if (steeringWheelForceFeedback != null)
                steeringWheelForceFeedback.SetManualForceFeedbackEffect(8000 * steeringInput);
        }
    }

    public void SetManualDriving(bool state)
    {
        _manualDriving = state;
    }

    void FixedUpdate()
    {
        
    }

    public float GetSteeringInput()
    {
        return steeringInput;
    }

    private void SetInputSourceSafe(string inputDevice)
    {
        // 关键：允许 null/空/拼写不一致，统一回退到键盘
        if (string.IsNullOrWhiteSpace(inputDevice) ||
            !Enum.TryParse<InputType>(inputDevice, true, out var input))
        {
            InputControlIndex = InputType.Keyboard;
            return;
        }
        InputControlIndex = input;
    }
}