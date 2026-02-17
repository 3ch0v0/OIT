using System;
using UnityEngine;
using UnityEngine.InputSystem; 

public class CameraController : MonoBehaviour
{   
    [Header("General Settings")]
    public Transform target;
    public float defaultYaw = 0f;
    public float defaultPitch = 45f;
    public float distance=10.0f;
    public float minDistance=2.0f;
    public float maxDistance=100.0f;
    
    [Header("Moving Setting")]
    public float rotateSpeed = 10f;
    public float zoomSpeed = 20.0f;
    
    [Header("Input Actions ")]
    public InputActionReference inputAction;
    
    private float currentYaw = 0f;
    private float currentPitch = 0f;

    void Start()
    {
        currentYaw = defaultYaw;
        currentPitch = defaultPitch;

    }
    private void OnEnable()
    {
        if(inputAction != null) inputAction.action.Enable();
    }

    private void OnDisable()
    {
        if(inputAction != null) inputAction.action.Disable();
    }

    void Update()
    {
        RotateCamera();
        UpdatePosition();
    }

    void RotateCamera()
    {
        Vector2 input = inputAction.action.ReadValue<Vector2>();
        currentYaw += input.x * rotateSpeed * Time.deltaTime;
        currentPitch +=input.y* rotateSpeed * Time.deltaTime;
        currentPitch =Mathf.Clamp(currentPitch,5,85);
    }

    void UpdatePosition()
    {
        Quaternion rotation=Quaternion.Euler(currentPitch,currentYaw,0);
        Vector3 position = target.position + rotation*Vector3.back*distance;
        transform.position = position;
        transform.LookAt(target.position);
    }
}