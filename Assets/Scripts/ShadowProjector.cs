using UnityEngine;

public class ShadowProjector : MonoBehaviour
{
    [Header("Настройки проекции")]
    public float shadowLength = 25f;
    public float minShadowWidth = 2.5f;

    [Header("Таймер исчезновения")]
    public float persistDuration = 10f;

    [Header("Префаб и материалы")]
    public GameObject shadowPlatformPrefab;
    
    [Tooltip("Базовый материал для визуализации (будет клонироваться и окрашиваться)")]
    public Material shadowVisualMaterial;
    
    public PhysicsMaterial matNormal;
    public PhysicsMaterial matBouncy;
    public PhysicsMaterial matGrippy;
    public PhysicsMaterial matSlippery;

    [Header("Ссылки на фонарь")]
    public Transform flashlightTransform;
    public FlashlightController flashlightController;

    private GameObject shadowPlatform;
    private Collider platformCollider;
    private MeshRenderer shadowRenderer;
    private Material shadowVisualInstance;
    private bool isShadowActive = false;
    private float timeSinceUnlit = 0f;
    
    private Vector3 lockedShadowDir = Vector3.forward;
    private Vector3 lockedShadowScale = Vector3.one;
    private bool isProjectionLocked = false;

    void Start()
    {
        isShadowActive = false;
        isProjectionLocked = false;
        timeSinceUnlit = 0f;

        if (flashlightTransform == null)
        {
            GameObject fl = GameObject.FindGameObjectWithTag("Flashlight");
            if (fl != null) flashlightTransform = fl.transform;
        }
        if (flashlightController == null && flashlightTransform != null)
        {
            flashlightController = flashlightTransform.GetComponent<FlashlightController>();
        }
    }

    void Update()
    {
        if (flashlightTransform == null || flashlightController == null || shadowPlatformPrefab == null) return;

        float distanceToLight = Vector3.Distance(transform.position, flashlightTransform.position);
        Vector3 toObject = (transform.position - flashlightTransform.position).normalized;
        float angleDot = Vector3.Dot(flashlightTransform.forward, toObject);
        
        bool isLightEnabled = flashlightController.isFlashlightOn;
        bool isLit = isLightEnabled && (distanceToLight < 30f) && (angleDot > 0.4f);

        if (isLit)
        {
            timeSinceUnlit = 0f;
            if (!isShadowActive)
            {
                ActivateShadow();
                isProjectionLocked = false;
            }
            UpdateShadowPosition();
            UpdateShadowPhysics();
            UpdateShadowVisuals();
        }
        else
        {
            if (isShadowActive)
            {
                timeSinceUnlit += Time.deltaTime;
                if (timeSinceUnlit >= persistDuration)
                {
                    DeactivateShadow();
                }
            }
        }
    }

    void ActivateShadow()
    {
        shadowPlatform = Instantiate(shadowPlatformPrefab, transform.position, Quaternion.identity);
        shadowPlatform.name = "Shadow_" + gameObject.name;
        shadowPlatform.transform.SetParent(null);
        
        platformCollider = shadowPlatform.GetComponent<Collider>();
        if (platformCollider != null && matNormal != null)
            platformCollider.material = matNormal;
        
        shadowRenderer = shadowPlatform.GetComponent<MeshRenderer>();
        
        if (shadowRenderer != null && shadowVisualMaterial != null)
        {
            shadowVisualInstance = new Material(shadowVisualMaterial);
            shadowRenderer.material = shadowVisualInstance;
            shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shadowRenderer.receiveShadows = false;
        }
        
        isShadowActive = true;
    }

    void UpdateShadowPosition()
    {
        if (shadowPlatform == null) return;

        if (!isProjectionLocked)
        {
            CalculateShadowProjection();
            isProjectionLocked = true;
        }

        Vector3 startPos = transform.position;
        Vector3 centerPos = startPos + lockedShadowDir * (lockedShadowScale.z * 0.5f);
        
        shadowPlatform.transform.position = centerPos;
        
        Vector3 safeUp = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(lockedShadowDir, Vector3.up)) > 0.95f)
            safeUp = Vector3.forward;
            
        shadowPlatform.transform.rotation = Quaternion.LookRotation(lockedShadowDir, safeUp);
        shadowPlatform.transform.localScale = lockedShadowScale;
    }

    void CalculateShadowProjection()
    {
        Vector3 lightDir = flashlightTransform.forward.normalized;
        if (lightDir.sqrMagnitude < 0.01f) lightDir = Vector3.forward;
        lockedShadowDir = lightDir;

        float dist = Vector3.Distance(transform.position, flashlightTransform.position);
        float lengthFactor = Mathf.Clamp(dist / 15f, 0.6f, 2.0f);
        float finalLength = Mathf.Max(shadowLength * lengthFactor, 5f);

        float finalWidth = minShadowWidth;
        Renderer objRenderer = GetComponent<Renderer>();
        if (objRenderer != null && objRenderer.bounds.extents.magnitude > 0.5f)
        {
            finalWidth = Mathf.Max(objRenderer.bounds.size.x, objRenderer.bounds.size.z) * 0.8f + minShadowWidth;
        }

        lockedShadowScale = new Vector3(finalWidth, 0.15f, finalLength);
    }

    void UpdateShadowPhysics()
    {
        if (shadowPlatform == null || platformCollider == null) return;
        if (flashlightController == null || !flashlightController.isFlashlightOn) return;

        Color lightColor = flashlightController.flashlightLight.color;
        PhysicsMaterial newMat = GetMaterialByColor(lightColor);
        
        if (newMat != null && platformCollider.material != newMat)
        {
            platformCollider.material = newMat;
        }
    }

    void UpdateShadowVisuals()
    {
        if (shadowVisualInstance == null || shadowRenderer == null) return;
        if (flashlightController == null || flashlightController.flashlightLight == null) return;
        if (!flashlightController.isFlashlightOn) return;

        Color lightColor = flashlightController.flashlightLight.color;
        
        Color visualColor = new Color(lightColor.r, lightColor.g, lightColor.b, 0.1f);
        shadowVisualInstance.color = visualColor;

        if (shadowVisualInstance.HasProperty("_EmissionColor"))
        {
            Color emission = new Color(lightColor.r * 0.5f, lightColor.g * 0.5f, lightColor.b * 0.5f);
            shadowVisualInstance.SetColor("_EmissionColor", emission);
        }
    }

    PhysicsMaterial GetMaterialByColor(Color color)
    {
        float r = color.r, g = color.g, b = color.b;
        if (r > 0.7f && g > 0.7f && b > 0.7f) return matNormal;
        if (g > r && g > b && g > 0.5f) return matBouncy;
        if (r > g && r > b && r > 0.5f) return matGrippy;
        if (b > r && b > g && b > 0.5f) return matSlippery;
        return matNormal;
    }

    void DeactivateShadow()
    {
        if (shadowPlatform != null) Destroy(shadowPlatform);
        shadowPlatform = null;
        platformCollider = null;
        shadowRenderer = null;
        
        if (shadowVisualInstance != null)
            Destroy(shadowVisualInstance);
            
        shadowVisualInstance = null;
        isShadowActive = false;
        timeSinceUnlit = 0f;
        isProjectionLocked = false;
    }

    void OnDestroy() { DeactivateShadow(); }

    void OnDrawGizmosSelected()
    {
        if (flashlightTransform == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(flashlightTransform.position, transform.position);
        
        Vector3 lightDir = flashlightTransform.forward.normalized;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + lightDir * shadowLength);
    }
}