using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class GroundShadowProjector : MonoBehaviour
{
    [Tooltip("Слой земли")]
    public string groundLayer = "Ground";
    
    [Tooltip("Максимальная дистанция поиска земли")]
    public float groundCheckDistance = 100f;
    
    [Tooltip("Буфер высоты")]
    public float groundOffset = 0.02f;
    
    [Tooltip("Насколько тень меньше объекта")]
    [Range(0.3f, 1.5f)] public float sizeMultiplier = 0.8f;

    [Header("Время жизни тени")]
    public float disappearDelay = 10f;
    
    [Tooltip("Задержка перед первым появлением тени")]
    public float appearDelay = 0.3f;

    [Header("Фонарик")]
    public Transform flashlightTransform;
    public FlashlightController flashlightController;

    [Header("Физические материалы")]
    public PhysicsMaterial matNormal;
    public PhysicsMaterial matBouncy;
    public PhysicsMaterial matGrippy;
    public PhysicsMaterial matSlippery;

    [Header("Визуал")]
    public Material shadowMaterial;
    
    [Tooltip("Плавность появления/исчезновения (0-1)")]
    [Range(0, 1)] public float fadeSpeed = 5f;

    private GameObject shadowObj;
    private Collider shadowCollider;
    private MeshRenderer shadowRenderer;
    private Material shadowMatInstance;
    
    private float lifeTimer = 0f;
    private float appearTimer = 0f;
    private bool isActive = false;
    private bool isFullyVisible = false;
    
    private Vector3 lastGroundPoint;
    private Vector3 lastShadowDirection;
    private int groundLayerMask;
    
    private Vector3 smoothPosition;
    private float smoothSize;

    void Start()
    {
        if (flashlightTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                flashlightTransform = cam.transform.Find("Flashlight");
            
            if (flashlightTransform == null)
            {
                GameObject fl = GameObject.FindWithTag("Flashlight");
                if (fl != null) flashlightTransform = fl.transform;
                else fl = GameObject.Find("Flashlight");
                if (fl != null) flashlightTransform = fl.transform;
            }
        }

        if (flashlightController == null && flashlightTransform != null)
            flashlightController = flashlightTransform.GetComponent<FlashlightController>();

        groundLayerMask = LayerMask.GetMask(groundLayer);
        
        if (groundLayerMask == 0)
            Debug.LogWarning($"Слой '{groundLayer}' не найден!");
    }

    void Update()
    {
        if (flashlightTransform == null) return;
        if (flashlightController == null) return;
        
        Light light = flashlightController.flashlightLight;
        if (light == null || !light.enabled)
        {
            HandleLightOff();
            return;
        }

        bool isLit = CheckIsLitStable(light);

        if (isLit)
        {
            HandleLightOn();
        }
        else
        {
            HandleLightOff();
        }
        
        if (isActive)
        {
            UpdateShadowSmooth();
        }
    }
    bool CheckIsLitStable(Light light)
    {
        Vector3 toObject = transform.position - flashlightTransform.position;
        float dist = toObject.magnitude;
        
        if (dist > light.range * 1.1f) return false;

        Vector3 toObjNorm = toObject.normalized;
        float angle = Vector3.Angle(flashlightTransform.forward, toObjNorm);
        float lightAngle = light.spotAngle > 0 ? light.spotAngle * 0.5f : 45f;
        
        if (angle > lightAngle + 15f) return false;

        Vector3 rayOrigin = flashlightTransform.position;
        Vector3 rayDir = toObjNorm;
        
        if (GetComponent<Collider>() is Collider col)
        {
            int objectLayerMask = 1 << gameObject.layer;
            int combinedMask = objectLayerMask | groundLayerMask;
            
            if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, dist + 2f, combinedMask))
            {
                if (hit.collider == col || hit.collider.gameObject == gameObject || 
                    IsChildOf(hit.collider.gameObject, gameObject))
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    bool IsChildOf(GameObject child, GameObject parent)
    {
        Transform t = child.transform;
        while (t != null)
        {
            if (t.gameObject == parent) return true;
            t = t.parent;
        }
        return false;
    }

    void HandleLightOn()
    {
        lifeTimer = disappearDelay;
        
        if (!isActive)
        {
            appearTimer += Time.deltaTime;
            if (appearTimer >= appearDelay)
            {
                CreateShadow();
                appearTimer = 0f;
            }
        }
        else
        {
            appearTimer = 0f;
        }
    }

    void HandleLightOff()
    {
        appearTimer = 0f;
        
        if (isActive)
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
            {
                DestroyShadow();
            }
            else
            {
                isFullyVisible = false;
            }
        }
    }

        void CreateShadow()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null) return;

        Bounds bounds = rend.bounds;
        float objectSize = Mathf.Max(bounds.size.x, bounds.size.z);
        float radius = Mathf.Clamp(objectSize * 0.5f * sizeMultiplier, 0.3f, 5f);

        Vector3 groundPoint = GetGroundPointStable(transform.position);
        if (groundPoint == Vector3.zero) return;

        shadowObj = new GameObject($"Shadow_{gameObject.name}");
        shadowObj.transform.SetParent(null);
        shadowObj.layer = gameObject.layer;

        MeshFilter mf = shadowObj.AddComponent<MeshFilter>();
        mf.mesh = CreateFlatDiscMesh(radius);
        shadowRenderer = shadowObj.AddComponent<MeshRenderer>();
        if (shadowMaterial != null)
        {
            shadowMatInstance = new Material(shadowMaterial);
            shadowRenderer.material = shadowMatInstance;
            shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shadowRenderer.receiveShadows = false;
        }

        BoxCollider boxCol = shadowObj.AddComponent<BoxCollider>();
        boxCol.center = new Vector3(0, 0.025f, 0); 
        boxCol.size = new Vector3(radius * 2f, 0.05f, radius * 2f);
        boxCol.isTrigger = false;
        shadowCollider = boxCol;

        lastGroundPoint = groundPoint;
        lastShadowDirection = CalculateShadowDirection();
        smoothPosition = groundPoint + lastShadowDirection * 0.3f;
        shadowObj.transform.position = smoothPosition;
        shadowObj.transform.rotation = Quaternion.identity;

        ApplyShadowAppearance();
        isActive = true;
        isFullyVisible = true;
    }

    Vector3 GetGroundPointStable(Vector3 fromPosition)
    {
        if (Physics.Raycast(fromPosition + Vector3.up * 0.5f, Vector3.down, 
            out RaycastHit hit, groundCheckDistance + 10f, groundLayerMask))
        {
            lastGroundPoint = hit.point + Vector3.up * groundOffset;
            return lastGroundPoint;
        }
        
        if (lastGroundPoint != Vector3.zero)
            return lastGroundPoint + Vector3.up * groundOffset;
            
        return Vector3.zero;
    }

    void UpdateShadowSmooth()
    {
        if (shadowObj == null) return;

        Vector3 groundPoint = GetGroundPointStable(transform.position);
        if (groundPoint == Vector3.zero) return;

        Vector3 newDirection = CalculateShadowDirection();

        Vector3 targetPosition = groundPoint + newDirection * 0.3f;

        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            float targetSize = Mathf.Clamp(
                Mathf.Max(rend.bounds.size.x, rend.bounds.size.z) * 0.5f * sizeMultiplier, 0.3f, 5f);

            smoothSize = Mathf.Lerp(smoothSize, targetSize, Time.deltaTime * 10f);
            shadowObj.transform.localScale = Vector3.one * smoothSize;

            if (shadowCollider is SphereCollider sc)
            {
                sc.radius = smoothSize;
                sc.center = new Vector3(0, smoothSize * 0.1f, 0);
            }
        }

        smoothPosition = Vector3.Lerp(smoothPosition, targetPosition, Time.deltaTime * 8f);
        shadowObj.transform.position = smoothPosition;

        lastShadowDirection = newDirection;

        UpdateShadowAppearanceSmooth();
    }

    Vector3 CalculateShadowDirection()
    {
        Vector3 lightDir = (transform.position - flashlightTransform.position).normalized;
        Vector3 dir = new Vector3(-lightDir.x, 0, -lightDir.z).normalized;
        return dir.sqrMagnitude > 0.01f ? dir : Vector3.forward;
    }
    void UpdateShadowAppearanceSmooth()
    {
        if (flashlightController?.flashlightLight == null) return;
        
        Color lightColor = flashlightController.flashlightLight.color;
        float targetAlpha = isFullyVisible ? 0.4f : Mathf.Lerp(0.4f, 0f, (disappearDelay - lifeTimer) / disappearDelay);

        if (shadowMatInstance != null)
        {
            Color currentColor = shadowMatInstance.color;
            Color targetColor = new Color(lightColor.r * 0.5f, lightColor.g * 0.5f, lightColor.b * 0.5f, targetAlpha);
            shadowMatInstance.color = Color.Lerp(currentColor, targetColor, Time.deltaTime * fadeSpeed);
        }

        if (shadowCollider != null)
        {
            PhysicsMaterial newMat = GetPhysicMaterialByColor(lightColor);
            if (newMat != null && shadowCollider.material != newMat)
            {
                shadowCollider.material = newMat;
            }
        }
    }

    void ApplyShadowAppearance()
    {
        if (flashlightController?.flashlightLight == null) return;
        
        Color c = flashlightController.flashlightLight.color;
        
        if (shadowMatInstance != null)
            shadowMatInstance.color = new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, 0.4f);
            
        if (shadowCollider != null)
            shadowCollider.material = GetPhysicMaterialByColor(c);
    }

    PhysicsMaterial GetPhysicMaterialByColor(Color c)
    {
        if (c.r > 0.6f && c.r > c.g && c.r > c.b) return matGrippy;
        if (c.g > 0.6f && c.g > c.r && c.g > c.b) return matBouncy;
        if (c.b > 0.6f && c.b > c.r && c.b > c.g) return matSlippery;
        if (c.r > 0.5f && c.g > 0.5f && c.b > 0.5f) return matNormal;
        return matNormal;
    }

    Mesh CreateFlatDiscMesh(float radius)
    {
        Mesh mesh = new Mesh();
        int segments = 24;
        
        Vector3[] verts = new Vector3[segments + 1];
        Vector2[] uvs = new Vector2[segments + 1];
        int[] tris = new int[segments * 3];

        verts[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < segments; i++)
        {
            float a = (float)i / segments * Mathf.PI * 2f;
            verts[i + 1] = new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
            uvs[i + 1] = new Vector2(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f);
        }

        for (int i = 0; i < segments; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = (i + 1) % segments + 1;
        }

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    void DestroyShadow()
    {
        if (shadowObj != null) Destroy(shadowObj);
        if (shadowMatInstance != null) Destroy(shadowMatInstance);
        
        shadowObj = null; shadowCollider = null; shadowRenderer = null; shadowMatInstance = null;
        isActive = false; isFullyVisible = false;
        appearTimer = 0f;
        
        Debug.Log($"Тень удалена: {gameObject.name}");
    }

    void OnDestroy() => DestroyShadow();

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isActive ? Color.yellow : Color.gray;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        
        if (isActive && shadowObj != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(shadowObj.transform.position, shadowObj.transform.localScale.x);
        }
    }
}