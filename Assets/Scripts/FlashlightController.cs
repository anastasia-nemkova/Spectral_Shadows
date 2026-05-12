using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightController : MonoBehaviour
{
    [Header("Настройки вращения")]
    public float inertiaStrength = 0.08f;
    public float damping = 0.92f;
    public float mouseSensitivity = 0.1f;

    [Header("Стабилизация")]
    public float stabilizationStrength = 0.05f;
    public float stabilizationThreshold = 5f;

    [Header("Свет")]
    public Light flashlightLight;

    [Header("Состояние")]
    public bool isFlashlightOn = false;

    private Vector2 angularVelocity;
    private Vector2 currentRotation;

    void Start()
    {
        currentRotation = Vector2.zero;
        angularVelocity = Vector2.zero;

        if (flashlightLight == null)
        {
            flashlightLight = GetComponent<Light>();
            if (flashlightLight == null)
                Debug.LogError($"[{gameObject.name}] Не найден компонент Light!");
        }

        isFlashlightOn = false;
        if (flashlightLight != null)
            flashlightLight.enabled = false;
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mouseDelta = mouse.delta.ReadValue();
        float inputX = mouseDelta.x * mouseSensitivity;
        float inputY = mouseDelta.y * mouseSensitivity;

        angularVelocity.x += inputX * inertiaStrength;
        angularVelocity.y += inputY * inertiaStrength;
        angularVelocity *= damping;

        if (angularVelocity.magnitude < 0.001f) angularVelocity = Vector2.zero;

        currentRotation.x += angularVelocity.x;
        currentRotation.y += angularVelocity.y;
        currentRotation.y = Mathf.Clamp(currentRotation.y, -80f, 80f);

        transform.localRotation = Quaternion.Euler(currentRotation.y, currentRotation.x, 0f);

        if (mouseDelta.magnitude < 0.1f)
        {
            float deviation = Mathf.Abs(currentRotation.x) + Mathf.Abs(currentRotation.y);
            if (deviation > stabilizationThreshold)
            {
                currentRotation.x = Mathf.Lerp(currentRotation.x, 0f, stabilizationStrength);
                currentRotation.y = Mathf.Lerp(currentRotation.y, 0f, stabilizationStrength);
            }
        }
    }

    public void ToggleFlashlight()
    {
        if (flashlightLight == null)
        {
            return;
        }

        isFlashlightOn = !isFlashlightOn;
        flashlightLight.enabled = isFlashlightOn;
    }

    public void SetLightColor(Color newColor)
    {
        if (flashlightLight != null)
            flashlightLight.color = newColor;
    }

    public void SetColorByIndex(int index)
    {
        Color c = index == 1 ? Color.red : 
                  index == 2 ? Color.green : 
                  index == 3 ? Color.blue : Color.white;
        SetLightColor(c);
    }

    public Vector3 GetBeamDirection() => transform.forward;
}