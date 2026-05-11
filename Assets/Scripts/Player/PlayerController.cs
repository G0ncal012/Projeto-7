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

    void Awake()
    {
        enabled = true;
        gameObject.SetActive(true);
    }

    void Start()
    {
        transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

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
        }

        SetupCamera();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("PlayerController ativo e PlayerCamera criada/ativada");
    }

    void SetupCamera()
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
        camObj.transform.localPosition = new Vector3(0f, 2f, 0f);
        camObj.transform.localRotation = Quaternion.identity;
        camObj.tag = "MainCamera";

        cam = camObj.GetComponent<Camera>();

        if (cam == null)
            cam = camObj.AddComponent<Camera>();

        cam.enabled = true;

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

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunning ? runSpeed : moveSpeed;

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

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

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
            col.height * transform.localScale.y * 0.7f,
            ~LayerMask.GetMask("Player")
        );

        if (Input.GetButtonDown("Jump") && isGrounded)
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
}