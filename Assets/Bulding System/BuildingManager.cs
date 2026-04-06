using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum SelectedBuildingType
{
    floor,
    wall
}
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

    [Header("Internal State")]
    [SerializeField] private bool isBuilding = false;
    [SerializeField] private int currentBuildingIndex;
    private GameObject ghostBuildGameobject;
    private bool isGhostInValidPosition = false;
    private Transform modelParent = null;


    private void Update()
    {
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<Camera>();

        if (playerCamera == null) return;

        if (Input.GetKeyDown(KeyCode.B))
            isBuilding = !isBuilding;

        if (isBuilding)
        {
            GhostBuild();

            if (Input.GetMouseButtonDown(0))
                PlaceBuild();
        }
        else if (ghostBuildGameobject)
        {
            Destroy(ghostBuildGameobject);
            ghostBuildGameobject = null;
        }
    }

    private Camera playerCamera;

    private void Start()
    {
        playerCamera = FindFirstObjectByType<Camera>();
    }

    private void GhostBuild()
    {
        GameObject currentBuild = GetCurrentBuild();
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
        if (Physics.Raycast(ray, out hit))
        {
            ghostBuildGameobject.transform.position = hit.point;
        }
    }

    private void CheckBuildValidity()
    {
        Collider[] colliders = Physics.OverlapSphere(ghostBuildGameobject.transform.position, connectorOverlapRadius, connectorLayer);
        if (colliders.Length > 0)
            GhostConnectBuild(colliders);
        else
            GhostSeparateBuild();
    }

    private void GhostConnectBuild(Collider[] colliders)
    {
        Connector bestConnector = null;

        foreach (Collider collider in colliders)
        {
            Connector connector = collider.GetComponent<Connector>();

            if (connector.canConnectTo)
            {
                bestConnector = connector;
                break;
            }
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
        Transform ghostConnector = FindSnapConnector(connector.transform, ghostBuildGameobject.transform.GetChild(1));
        ghostBuildGameobject.transform.position = connector.transform.position - (ghostConnector.position - ghostBuildGameobject.transform.position);

        if (currentBuildType == SelectedBuildingType.wall)
        {
            Quaternion newRotation = ghostBuildGameobject.transform.rotation;
            Vector3 euler = newRotation.eulerAngles;
            euler.y = connector.transform.rotation.eulerAngles.y;
            ghostBuildGameobject.transform.rotation = Quaternion.Euler(euler);
        }

        GhostifyModel(modelParent, ghostMaterialValid);
        isGhostInValidPosition = true;
    }

    private void GhostSeparateBuild()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (currentBuildType == SelectedBuildingType.wall)
            {
                GhostifyModel(modelParent, ghostMaterialInvalid);
                isGhostInValidPosition = false;
                return;
            }

            if (hit.collider.transform.root.CompareTag("Buildables"))
            {
                GhostifyModel(modelParent, ghostMaterialInvalid);
                isGhostInValidPosition = false;
                return;
            }

            if (Vector3.Angle(hit.normal, Vector3.up) < maxGroundAngle)
            {
                GhostifyModel(modelParent, ghostMaterialValid);
                isGhostInValidPosition = true;
            }
            else
            {
                GhostifyModel(modelParent, ghostMaterialInvalid);
                isGhostInValidPosition = false;
            }
        }
    }

    private Transform FindSnapConnector(Transform snapConnector, Transform ghostConnectorParent)
    {
        ConnectorPosition oppositePosition = GetOppositePosition(snapConnector.GetComponent<Connector>());

        foreach (Connector connector in ghostConnectorParent.GetComponentsInChildren<Connector>())
        {
            if (connector.position == oppositePosition) // FIX: connectorPosition -> position
            {
                return connector.transform;
            }
        }
        return null;
    }


    private ConnectorPosition GetOppositePosition(Connector connector)
    {
        ConnectorPosition position = connector.position; // FIX: connectorPosition -> position

        if (currentBuildType == SelectedBuildingType.wall && connector.connectorParentType == SelectedBuildingType.floor)
            return ConnectorPosition.bottom;

        if (currentBuildType == SelectedBuildingType.floor && connector.connectorParentType == SelectedBuildingType.wall &&
            connector.position == ConnectorPosition.top) // FIX: connectorPosition -> position
        {
            if (connector.transform.root.rotation.eulerAngles.y == 0)
                return GetConnectorClosestToPlayer(true);
            else
                return GetConnectorClosestToPlayer(false);
        }

        switch (position)
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
        Transform cameraTransform = playerCamera.transform;

        if (topBottom)
        {
            return cameraTransform.position.z >= ghostBuildGameobject.transform.position.z
                ? ConnectorPosition.bottom
                : ConnectorPosition.top;
        }
        else
        {
            return cameraTransform.position.x >= ghostBuildGameobject.transform.position.x
                ? ConnectorPosition.left
                : ConnectorPosition.right;
        }
    }

    private void GhostifyModel(Transform modelParent, Material ghostMaterial = null)
    {
        if (ghostMaterial != null)
        {
            foreach (MeshRenderer meshRenderer in modelParent.GetComponentsInChildren<MeshRenderer>())
            {
                meshRenderer.material = ghostMaterial;
            }
        }
        else
        {
            foreach (Collider modelCollider in modelParent.GetComponentsInChildren<Collider>())
            {
                modelCollider.enabled = false;
            }
        }
    }

    private GameObject GetCurrentBuild()
    {
        switch (currentBuildType)
        {
            case SelectedBuildingType.floor: return floorObjects[currentBuildingIndex];
            case SelectedBuildingType.wall: return wallObjects[currentBuildingIndex];
            default: return null;
        }
    }

    private void PlaceBuild()
    {
        if (ghostBuildGameobject != null && isGhostInValidPosition)
        {
            GameObject newBuild = Instantiate(
                GetCurrentBuild(),
                ghostBuildGameobject.transform.position,
                ghostBuildGameobject.transform.rotation
            );

            Destroy(ghostBuildGameobject);
            ghostBuildGameobject = null;
            // isBuilding = false;

            foreach (Connector connector in newBuild.GetComponentsInChildren<Connector>())
            {
                connector.UpdateConnectors(true);
            }
        }
    }
}