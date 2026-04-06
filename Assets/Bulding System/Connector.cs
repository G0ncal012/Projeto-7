using UnityEngine;

[System.Serializable]
public enum ConnectorPosition
{
    left, right, top, bottom
}


public class Connector : MonoBehaviour
{
    public ConnectorPosition position;
    public SelectedBuildingType connectorParentType;

    [HideInInspector] public bool isConnectedToFloor = false;
    [HideInInspector] public bool isConnectedToWall = false;
    [HideInInspector] public bool canConnectTo = true;

    [SerializeField] private bool canConnectToFloor = true;
    [SerializeField] private bool canConnectToWall = true;

    private void OnDrawGizmos()
    {
        // Set Gizmo color based on connection states
        if (isConnectedToFloor && isConnectedToWall)
            Gizmos.color = Color.red;
        else if (isConnectedToFloor)
            Gizmos.color = Color.blue;
        else if (isConnectedToWall)
            Gizmos.color = Color.yellow;
        else
            Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(transform.position, transform.lossyScale.x / 2f);
    }

    public void UpdateConnectors(bool rootCall = false)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, transform.lossyScale.x / 2f);

        isConnectedToFloor = !canConnectToFloor;
        isConnectedToWall = !canConnectToWall;

        foreach (Collider collider in colliders)
        {
            if (collider.GetInstanceID() == GetComponent<Collider>().GetInstanceID())
            { continue; }
                

            // Only check objects on the same layer
            if (collider.gameObject.layer == gameObject.layer)
            {
                Connector foundConnector = collider.GetComponent<Connector>();
                if (foundConnector == null) continue;

                if (foundConnector.connectorParentType == SelectedBuildingType.floor)
                    isConnectedToFloor = true;

                if (foundConnector.connectorParentType == SelectedBuildingType.wall)
                    isConnectedToWall = true;

                if (rootCall)
                    foundConnector.UpdateConnectors(false);
            }
        }
        canConnectTo = true;
        if (isConnectedToFloor && isConnectedToWall)
            canConnectTo = false;
    }
}