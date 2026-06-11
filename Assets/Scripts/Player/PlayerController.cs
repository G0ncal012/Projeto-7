using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float runSpeed = 10f;
    public float jumpForce = 4f;

    [Header("Swimming")]
    public float swimSpeed = 3f;
    public float floatForce = 8f;
    public float waterDrag = 3f;
    public float waterLevel = 0.15f;

    private const string SensitivityPrefKey = "MouseSensitivity";
    public const float MinSensitivity = 0.5f;
    public const float MaxSensitivity = 5f;
    private const float DefaultSensitivity = 2f;

    private Rigidbody rb;
    private Camera cam;
    private float xRotation = 0f;
    private bool isInWater = false;
    private GameObject underwaterOverlay;

    void Awake()
    {
        enabled = true;
        gameObject.SetActive(true);
    }

    void Start()
    {
        transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

        rb = GetComponent<Rigidbody>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.freezeRotation = true;

        CapsuleCollider col = GetComponent<CapsuleCollider>();

        if (col == null)
            col = gameObject.AddComponent<CapsuleCollider>();

        col.height = 2f;
        col.radius = 0.5f;
        col.center = new Vector3(0f, 1f, 0f);

        Animator animator = GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.gameObject.SetActive(true);
            animator.enabled = true;

            AlignFeetToGround(animator);
        }

        SetupCamera(animator);

        if (GetComponent<AxeTool>() == null) gameObject.AddComponent<AxeTool>();
        if (GetComponent<PickaxeTool>() == null) gameObject.AddComponent<PickaxeTool>();

        Health health = GetComponent<Health>();
        if (health == null) health = gameObject.AddComponent<Health>();
        health.SetMaxHP(100f, refillHP: true);

        if (GetComponent<PlayerRespawn>() == null) gameObject.AddComponent<PlayerRespawn>();
        if (GetComponent<HungerSystem>() == null) gameObject.AddComponent<HungerSystem>();
        if (GetComponent<FoodConsumption>() == null) gameObject.AddComponent<FoodConsumption>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("PlayerController ativo e PlayerCamera criada/ativada");
    }

    /// <summary>
    /// Desloca o modelo (filho com o Animator) verticalmente para que o ponto mais baixo
    /// do seu mesh fique alinhado com a base do CapsuleCollider (pés no chão, não debaixo dele).
    /// </summary>
    void AlignFeetToGround(Animator animator)
    {
        if (animator.transform == transform) return;

        Renderer[] renderers = animator.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);

        if (transform.lossyScale.y == 0f) return;

        float worldOffset = transform.position.y - bounds.min.y;

        Vector3 modelLocalPos = animator.transform.localPosition;
        modelLocalPos.y += worldOffset / transform.lossyScale.y;
        animator.transform.localPosition = modelLocalPos;
    }

    void SetupCamera(Animator animator)
    {
        Transform existingCam = transform.Find("PlayerCamera");
        GameObject camObj;

        if (existingCam != null)
        {
            camObj = existingCam.gameObject;
        }
        else
        {
            camObj = new GameObject("PlayerCamera");
            camObj.transform.SetParent(transform);
        }

        camObj.SetActive(true);

        // Posiciona a câmara à frente da cabeça (em vez de dentro do crânio),
        // calculado a partir do osso da cabeça do Avatar Humanoid.
        Vector3 camLocalPos = new Vector3(0f, 1.25f, 0.15f);
        Transform head = animator != null ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
        if (head != null)
        {
            Vector3 headLocal = transform.InverseTransformPoint(head.position);
            camLocalPos = headLocal + new Vector3(0f, 0f, 0.25f);
        }

        camObj.transform.localPosition = camLocalPos;
        camObj.transform.localRotation = Quaternion.identity;
        camObj.tag = "MainCamera";

        cam = camObj.GetComponent<Camera>();

        if (cam == null)
            cam = camObj.AddComponent<Camera>();

        cam.enabled = true;
        cam.nearClipPlane = 0.03f;

        AudioListener listener = camObj.GetComponent<AudioListener>();

        if (listener == null)
            listener = camObj.AddComponent<AudioListener>();

        listener.enabled = true;

        Camera[] allCams = FindObjectsByType<Camera>(FindObjectsSortMode.None);

        foreach (Camera c in allCams)
        {
            if (c != cam)
                c.enabled = false;
        }
    }

    void Update()
    {
        if (InventoryUI.IsOpen) return;

        HandleLook();

        isInWater = transform.position.y < waterLevel;

        if (!isInWater)
            HandleJump();

        HandleUnderwaterEffect();
    }

    void FixedUpdate()
    {
        if (InventoryUI.IsOpen) return;

        if (isInWater)
            HandleSwimming();
        else
            HandleMovement();
    }

    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        float weight = InventorySystem.Instance != null ? InventorySystem.Instance.GetTotalWeight() : 0f;
        bool isOverloaded = weight >= InventorySystem.MaxWeight;
        bool isHeavy = weight >= InventorySystem.HeavyThreshold;

        float currentSpeed;
        if (isOverloaded)
            currentSpeed = moveSpeed / 3f;
        else if (isHeavy)
            currentSpeed = moveSpeed / 1.5f;
        else
        {
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            currentSpeed = isRunning ? runSpeed : moveSpeed;
        }

        Vector3 move = transform.right * x + transform.forward * z;
        Vector3 newVelocity = move * currentSpeed;
        newVelocity.y = rb.linearVelocity.y;

        rb.linearVelocity = newVelocity;
    }

    void HandleSwimming()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = cam.transform.right * x + cam.transform.forward * z;
        Vector3 newVelocity = move * swimSpeed;

        if (Input.GetButton("Jump"))
            newVelocity.y += swimSpeed;
        else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C))
            newVelocity.y -= swimSpeed;

        rb.linearVelocity = newVelocity;
        rb.linearDamping = waterDrag;
    }

    void HandleLook()
    {
        if (cam == null) return;

        float sensitivity = GetSensitivity();
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void HandleJump()
    {
        CapsuleCollider col = GetComponent<CapsuleCollider>();

        bool isGrounded = Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            0.25f,
            ~LayerMask.GetMask("Player")
        );

        float weight = InventorySystem.Instance != null ? InventorySystem.Instance.GetTotalWeight() : 0f;

        if (weight < InventorySystem.HeavyThreshold && Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        rb.linearDamping = 0f;
    }

    void CreateUnderwaterOverlay()
    {
        if (cam == null) return;

        underwaterOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
        underwaterOverlay.name = "UnderwaterOverlay";
        underwaterOverlay.transform.SetParent(cam.transform);
        underwaterOverlay.transform.localPosition = new Vector3(0f, 0f, 0.15f);
        underwaterOverlay.transform.localRotation = Quaternion.identity;
        underwaterOverlay.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        Destroy(underwaterOverlay.GetComponent<Collider>());

        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(0.0f, 0.2f, 0.6f, 0.4f);

        underwaterOverlay.GetComponent<Renderer>().material = mat;
        underwaterOverlay.SetActive(false);
    }

    void HandleUnderwaterEffect()
    {
        if (underwaterOverlay == null)
            CreateUnderwaterOverlay();

        if (underwaterOverlay == null) return;

        if (isInWater)
        {
            underwaterOverlay.SetActive(true);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.0f, 0.2f, 0.5f, 1f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.08f;
        }
        else
        {
            underwaterOverlay.SetActive(false);
            RenderSettings.fog = false;
        }
    }

    /// <summary>Sensibilidade guardada (0.5–5), usada para inicializar sliders nos menus.</summary>
    public static float GetSensitivity() => PlayerPrefs.GetFloat(SensitivityPrefKey, DefaultSensitivity);

    /// <summary>Aplica e guarda uma nova sensibilidade do rato.</summary>
    public static void SetSensitivity(float value)
    {
        value = Mathf.Clamp(value, MinSensitivity, MaxSensitivity);
        PlayerPrefs.SetFloat(SensitivityPrefKey, value);
    }
}