using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum SelectedBuildingType { floor, wall }

public class BuildingManager : MonoBehaviour
{
    [Header("Build Objects")]
    [SerializeField] private List<GameObject> floorObjects = new List<GameObject>();
    [SerializeField] private List<GameObject> wallObjects = new List<GameObject>();

    [Header("Build Settings")]
    [SerializeField] private SelectedBuildingType currentBuildType;
    [SerializeField] private LayerMask connectorLayer;

    [Header("Ghost Settings")]
    [SerializeField] private Material ghostMaterialValid;
    [SerializeField] private Material ghostMaterialInvalid;
    [SerializeField] private float connectorOverlapRadius = 1f;
    [SerializeField] private float maxGroundAngle = 45f;

    [Header("Distance Settings")]
    [SerializeField] private float maxBuildDistance = 5f;

    [Header("Internal State — read only")]
    [SerializeField] private int currentBuildingIndex;

    [System.Serializable]
    public class HotbarSlot
    {
        public string displayName;
        public SelectedBuildingType buildType;
        public int objectIndex;
        public Sprite icon;
    }

    [Header("Hotbar")]
    [SerializeField] private List<HotbarSlot> hotbarSlots = new List<HotbarSlot>();
    [SerializeField] private int selectedHotbarIndex = 0;

    public System.Action<int> OnHotbarSelectionChanged;

    private Camera playerCamera;
    private Transform playerTransform;
    private GameObject ghostBuildGameobject;
    private bool isGhostInValidPosition = false;
    private Transform modelParent = null;
    private bool buildModeActive = false;

    private void Start()
    {
        playerCamera = FindFirstObjectByType<Camera>();
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) playerTransform = p.transform;

        if (hotbarSlots.Count == 0) AutoPopulateHotbar();

        buildModeActive = false;
        ApplyHotbarSelection();
    }

    private void AutoPopulateHotbar()
    {
        for (int i = 0; i < floorObjects.Count; i++)
            hotbarSlots.Add(new HotbarSlot
            {
                displayName = floorObjects[i] != null ? floorObjects[i].name : $"Floor {i + 1}",
                buildType = SelectedBuildingType.floor,
                objectIndex = i
            });

        for (int i = 0; i < wallObjects.Count; i++)
            hotbarSlots.Add(new HotbarSlot
            {
                displayName = wallObjects[i] != null ? wallObjects[i].name : $"Wall {i + 1}",
                buildType = SelectedBuildingType.wall,
                objectIndex = i
            });
    }

    private void Update()
    {
        if (playerCamera == null) playerCamera = FindFirstObjectByType<Camera>();
        if (playerCamera == null) return;

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        // Hotbar input gerido pelo InventoryUI — não processar aqui

        if (buildModeActive)
        {
            if (!HasEquippedBuildItem())
            {
                DeactivateBuildMode();
                return;
            }

            GhostBuild();
            if (Input.GetMouseButtonDown(0)) PlaceBuild();
        }
        else if (ghostBuildGameobject)
        {
            Destroy(ghostBuildGameobject);
            ghostBuildGameobject = null;
        }
    }

    private bool HasEquippedBuildItem()
    {
        if (InventorySystem.Instance == null) return false;
        var stack = InventorySystem.Instance.hotbar[InventoryUI.SelectedHotbarSlot];
        if (stack == null) return false;
        return stack.itemName == "Floor" || stack.itemName == "Wall";
    }

    public void SelectHotbarSlot(int index)
    {
        if (index < 0 || index >= hotbarSlots.Count) return;
        selectedHotbarIndex = index;
        buildModeActive = true;

        ApplyHotbarSelection();

        if (ghostBuildGameobject != null)
        {
            Destroy(ghostBuildGameobject);
            ghostBuildGameobject = null;
        }

        OnHotbarSelectionChanged?.Invoke(selectedHotbarIndex);
    }

    public void DeactivateBuildMode()
    {
        buildModeActive = false;
        if (ghostBuildGameobject != null)
        {
            Destroy(ghostBuildGameobject);
            ghostBuildGameobject = null;
        }
    }

    private void ApplyHotbarSelection()
    {
        if (hotbarSlots.Count == 0) return;
        currentBuildType = hotbarSlots[selectedHotbarIndex].buildType;
        currentBuildingIndex = hotbarSlots[selectedHotbarIndex].objectIndex;
    }

    public List<HotbarSlot> GetHotbarSlots() => hotbarSlots;
    public int GetSelectedHotbarIndex() => selectedHotbarIndex;
    public bool IsBuildModeActive() => buildModeActive;

    private void GhostBuild()
    {
        GameObject currentBuild = GetCurrentBuild();
        if (currentBuild == null) return;

        CreateGhostPrefab(currentBuild);
        MoveGhostPrefabToRaycast();
        CheckBuildValidity();
    }

    private void CreateGhostPrefab(GameObject currentBuild)
    {
        if (ghostBuildGameobject == null)
        {
            ghostBuildGameobject = Instantiate(currentBuild);
            modelParent = ghostBuildGameobject.transform.GetChild(0);
            GhostifyModel(modelParent, ghostMaterialInvalid);
            GhostifyModel(ghostBuildGameobject.transform);
        }
    }

    private void MoveGhostPrefabToRaycast()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxBuildDistance))
            ghostBuildGameobject.transform.position = hit.point;
        else
            ghostBuildGameobject.transform.position = ray.GetPoint(maxBuildDistance);
    }

    private void CheckBuildValidity()
    {
        if (!IsWithinBuildDistance())
        {
            GhostifyModel(modelParent, ghostMaterialInvalid);
            isGhostInValidPosition = false;
            return;
        }

        Collider[] colliders = Physics.OverlapSphere(
            ghostBuildGameobject.transform.position, connectorOverlapRadius, connectorLayer);

        if (colliders.Length > 0) GhostConnectBuild(colliders);
        else GhostSeparateBuild();
    }

    private bool IsWithinBuildDistance()
    {
        if (playerTransform == null) return true;
        return Vector3.Distance(playerTransform.position,
                                ghostBuildGameobject.transform.position) <= maxBuildDistance;
    }

    private void GhostConnectBuild(Collider[] colliders)
    {
        Connector bestConnector = null;
        foreach (Collider col in colliders)
        {
            Connector c = col.GetComponent<Connector>();
            if (c != null && c.canConnectTo) { bestConnector = c; break; }
        }

        if (bestConnector == null ||
            (currentBuildType == SelectedBuildingType.floor && bestConnector.isConnectedToFloor) ||
            (currentBuildType == SelectedBuildingType.wall && bestConnector.isConnectedToWall))
        {
            GhostifyModel(modelParent, ghostMaterialInvalid);
            isGhostInValidPosition = false;
            return;
        }

        SnapGhostPrefabToConnector(bestConnector);
    }

    private void SnapGhostPrefabToConnector(Connector connector)
    {
        Transform ghostConnector = FindSnapConnector(
            connector.transform, ghostBuildGameobject.transform.GetChild(1));
        if (ghostConnector == null) return;

        ghostBuildGameobject.transform.position =
            connector.transform.position - (ghostConnector.position - ghostBuildGameobject.transform.position);

        if (currentBuildType == SelectedBuildingType.wall)
        {
            Vector3 euler = ghostBuildGameobject.transform.rotation.eulerAngles;
            euler.y = connector.transform.rotation.eulerAngles.y;
            ghostBuildGameobject.transform.rotation = Quaternion.Euler(euler);
        }

        if (!IsWithinBuildDistance())
        {
            GhostifyModel(modelParent, ghostMaterialInvalid);
            isGhostInValidPosition = false;
            return;
        }

        GhostifyModel(modelParent, ghostMaterialValid);
        isGhostInValidPosition = true;
    }

    private void GhostSeparateBuild()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, maxBuildDistance)) return;

        if (currentBuildType == SelectedBuildingType.wall ||
            hit.collider.transform.root.CompareTag("Buildables"))
        {
            GhostifyModel(modelParent, ghostMaterialInvalid);
            isGhostInValidPosition = false;
            return;
        }

        bool valid = Vector3.Angle(hit.normal, Vector3.up) < maxGroundAngle;
        GhostifyModel(modelParent, valid ? ghostMaterialValid : ghostMaterialInvalid);
        isGhostInValidPosition = valid;
    }

    private Transform FindSnapConnector(Transform snapConnector, Transform ghostConnectorParent)
    {
        ConnectorPosition opp = GetOppositePosition(snapConnector.GetComponent<Connector>());
        foreach (Connector c in ghostConnectorParent.GetComponentsInChildren<Connector>())
            if (c.position == opp) return c.transform;
        return null;
    }

    private ConnectorPosition GetOppositePosition(Connector connector)
    {
        ConnectorPosition pos = connector.position;

        if (currentBuildType == SelectedBuildingType.wall &&
            connector.connectorParentType == SelectedBuildingType.floor)
            return ConnectorPosition.bottom;

        if (currentBuildType == SelectedBuildingType.floor &&
            connector.connectorParentType == SelectedBuildingType.wall &&
            connector.position == ConnectorPosition.top)
        {
            return connector.transform.root.rotation.eulerAngles.y == 0
                ? GetConnectorClosestToPlayer(true)
                : GetConnectorClosestToPlayer(false);
        }

        switch (pos)
        {
            case ConnectorPosition.left: return ConnectorPosition.right;
            case ConnectorPosition.right: return ConnectorPosition.left;
            case ConnectorPosition.bottom: return ConnectorPosition.top;
            case ConnectorPosition.top: return ConnectorPosition.bottom;
            default: return ConnectorPosition.bottom;
        }
    }

    private ConnectorPosition GetConnectorClosestToPlayer(bool topBottom)
    {
        Transform cam = playerCamera.transform;
        if (topBottom)
            return cam.position.z >= ghostBuildGameobject.transform.position.z
                ? ConnectorPosition.bottom : ConnectorPosition.top;
        else
            return cam.position.x >= ghostBuildGameobject.transform.position.x
                ? ConnectorPosition.left : ConnectorPosition.right;
    }

    private void GhostifyModel(Transform parent, Material ghostMaterial = null)
    {
        if (ghostMaterial != null)
            foreach (MeshRenderer r in parent.GetComponentsInChildren<MeshRenderer>())
                r.material = ghostMaterial;
        else
            foreach (Collider c in parent.GetComponentsInChildren<Collider>())
                c.enabled = false;
    }

    private GameObject GetCurrentBuild()
    {
        switch (currentBuildType)
        {
            case SelectedBuildingType.floor:
                return currentBuildingIndex < floorObjects.Count ? floorObjects[currentBuildingIndex] : null;
            case SelectedBuildingType.wall:
                return currentBuildingIndex < wallObjects.Count ? wallObjects[currentBuildingIndex] : null;
            default: return null;
        }
    }

    private void PlaceBuild()
    {
        if (ghostBuildGameobject == null || !isGhostInValidPosition) return;

        GameObject newBuild = Instantiate(
            GetCurrentBuild(),
            ghostBuildGameobject.transform.position,
            ghostBuildGameobject.transform.rotation);

        Destroy(ghostBuildGameobject);
        ghostBuildGameobject = null;

        foreach (Connector c in newBuild.GetComponentsInChildren<Connector>())
            c.UpdateConnectors(true);

        InventoryUI.ConsumeEquippedItem();
    }

    private void OnValidate()
    {
        if (hotbarSlots != null)
        {
            foreach (var slot in hotbarSlots)
            {
                if (slot.buildType == SelectedBuildingType.floor && floorObjects.Count > 0)
                    slot.objectIndex = Mathf.Clamp(slot.objectIndex, 0, floorObjects.Count - 1);
                if (slot.buildType == SelectedBuildingType.wall && wallObjects.Count > 0)
                    slot.objectIndex = Mathf.Clamp(slot.objectIndex, 0, wallObjects.Count - 1);
            }
        }
    }
}