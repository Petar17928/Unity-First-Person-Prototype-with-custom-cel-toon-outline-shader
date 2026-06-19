using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour 
{
    private PlayerInput playerInput;
    private PlayerInput.OnFootActions onFoot;
    private PlayerMotor motor;
    private PlayerLook look;
    
    void Awake()
    {
        playerInput = new PlayerInput();
        onFoot = playerInput.OnFoot;
        motor = GetComponent<PlayerMotor>();
        look = GetComponent<PlayerLook>();

        onFoot.Jump.performed += ctx => motor.Jump();
        onFoot.Crouch.performed += ctx => motor.Crouch();

        onFoot.Sprint.performed += ctx => motor.StartSprint();
        onFoot.Sprint.canceled += ctx => motor.StopSprint();

        onFoot.Aim.performed += ctx => motor.StartAim();
        onFoot.Aim.canceled += ctx => motor.StopAim();
    }
    void FixedUpdate()
    {
        motor.ProccessMove(onFoot.Movement.ReadValue<Vector2>());
    }
    private void Update()
    {
        look.ProccessLook(onFoot.Look.ReadValue<Vector2>());
    }
    private void OnEnable()
    {
        onFoot.Enable();
    }
    private void OnDisable()
    {
        onFoot.Disable();
    }
}
