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

    [Header("Look")]
    public float mouseSensitivity = 2f;

    private Rigidbody rb;
    private Camera cam;
    private float xRotation = 0f;
    private bool isInWater = false;
    private GameObject underwaterOverlay;

    void Start()
    {
        transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        rb = GetComponent<Rigidbody>();

        GameObject camObj = new GameObject("PlayerCamera");
        camObj.transform.SetParent(transform);
        camObj.transform.localPosition = new Vector3(0f, 2f, 0f);
        camObj.transform.localRotation = Quaternion.identity;
        cam = camObj.AddComponent<Camera>();
        camObj.tag = "MainCamera";
        camObj.AddComponent<AudioListener>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Não processa input de jogo com inventário aberto
        if (InventoryUI.IsOpen) return;

        HandleLook();

        isInWater = transform.position.y < waterLevel;

        if (!isInWater)
            HandleJump();

        HandleUnderwaterEffect();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
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
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void HandleJump()
    {
        bool isGrounded = Physics.Raycast(transform.position, Vector3.down, GetComponent<CapsuleCollider>().height * transform.localScale.y * 0.6f, ~LayerMask.GetMask("Player"));

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
        underwaterOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
        underwaterOverlay.name = "UnderwaterOverlay";
        underwaterOverlay.transform.SetParent(cam.transform);
        underwaterOverlay.transform.localPosition = new Vector3(0f, 0f, 0.15f);
        underwaterOverlay.transform.localRotation = Quaternion.identity;
        underwaterOverlay.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        Destroy(underwaterOverlay.GetComponent<Collider>());

        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(0.0f, 0.2f, 0.6f, 0.4f);
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3000;

        underwaterOverlay.GetComponent<Renderer>().material = mat;
        underwaterOverlay.SetActive(false);
    }

    void HandleUnderwaterEffect()
    {
        if (underwaterOverlay == null)
            CreateUnderwaterOverlay();

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
}
