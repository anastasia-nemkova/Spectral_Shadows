using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [Header("Настройки движения")]
    public float moveSpeed = 7f;
    public float jumpForce = 10f;
    public float gravity = 25f;

    [Header("Настройки камеры")]
    public float mouseSensitivity = 0.15f; 

    [Header("Фонарь")]
    public FlashlightController flashlightController;
    public Transform cameraTransform;

    [Header("Победа")]
    public VictoryManager victoryManager; 

    private CharacterController controller;
    private Vector3 moveDirection;
    private float verticalVelocity;
    private float xRotation = 0f;

    public static int crystalsCollected = 0;
    public static int totalCrystals = 3;

    private float currentMoveSpeed;
    private float currentJumpForce;
    private float currentGravity;
    private PhysicsMaterial currentSurfaceMat;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        crystalsCollected = 0;
        
        currentMoveSpeed = moveSpeed;
        currentJumpForce = jumpForce;
        currentGravity = gravity;
        
        victoryManager = FindFirstObjectByType<VictoryManager>();
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        if (mouse == null || keyboard == null) return;

        Vector2 mouseDelta = mouse.delta.ReadValue();
        xRotation -= mouseDelta.y * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseDelta.x * mouseSensitivity);

        float x = keyboard.dKey.ReadValue() - keyboard.aKey.ReadValue();
        float z = keyboard.wKey.ReadValue() - keyboard.sKey.ReadValue();
        Vector3 move = transform.right * x + transform.forward * z;
        if (move.magnitude > 1f) move.Normalize();
        
        moveDirection = move * currentMoveSpeed;

        DetectSurface();

        if (controller.isGrounded)
        {
            verticalVelocity = -1f;
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                verticalVelocity = currentJumpForce;
                
                PlayerAudioManager audio = GetComponent<PlayerAudioManager>();
                if (audio != null) audio.PlayJump();
            }
        }
        else
        {
            verticalVelocity -= currentGravity * Time.deltaTime;
        }
        moveDirection.y = verticalVelocity;

        controller.Move(moveDirection * Time.deltaTime);

        if (keyboard.digit1Key.wasPressedThisFrame && flashlightController != null)
            flashlightController.SetColorByIndex(1);
        if (keyboard.digit2Key.wasPressedThisFrame && flashlightController != null)
            flashlightController.SetColorByIndex(2);
        if (keyboard.digit3Key.wasPressedThisFrame && flashlightController != null)
            flashlightController.SetColorByIndex(3);
        if (keyboard.digit0Key.wasPressedThisFrame && flashlightController != null)
            flashlightController.SetColorByIndex(0);
        if (keyboard.fKey.wasPressedThisFrame && flashlightController != null)
            flashlightController.ToggleFlashlight();
    }

    void DetectSurface()
    {
        currentMoveSpeed = moveSpeed;
        currentJumpForce = jumpForce;
        currentGravity = gravity;

        if (!controller.isGrounded) return;

        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.15f, Vector3.down, out hit, 1.5f))
        {
            PhysicsMaterial mat = hit.collider.material;
            if (mat != null)
            {
                if (mat.bounciness > 0.5f)
                {
                    currentJumpForce = jumpForce * 2.2f;
                    currentGravity = gravity * 0.65f;
                }
                else if (mat.dynamicFriction > 0.8f)
                {
                    currentMoveSpeed = moveSpeed * 0.45f;
                    currentGravity = gravity * 1.4f;
                }
                else if (mat.dynamicFriction < 0.1f)
                {
                    currentMoveSpeed = moveSpeed * 2.0f;
                    currentGravity = gravity * 0.5f;
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Crystal"))
        {
            crystalsCollected++;
            Debug.Log($"Кристалл собран: {crystalsCollected}/{totalCrystals}");
            
            PlayerAudioManager audio = GetComponent<PlayerAudioManager>();
            if (audio != null) audio.PlayCollect();
            
            Destroy(other.gameObject);

            if (crystalsCollected >= totalCrystals)
            {
                Debug.Log("ПОБЕДА!");
                
                if (victoryManager != null)
                {
                    victoryManager.ShowVictory();
                }
                else
                {
                    Debug.LogError("VictoryManager не найден!");
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
            }
        }
    }
}