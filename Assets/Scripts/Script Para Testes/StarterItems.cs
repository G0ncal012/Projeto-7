using UnityEngine;

// Coloca este script no GameManager
// Dá items ao jogador no início do jogo
public class StarterItems : MonoBehaviour
{
    [System.Serializable]
    public class StartItem
    {
        public string itemName;
        public int quantity = 1;
    }

    [SerializeField] private StartItem[] items;

    void Start()
    {
        // Espera um frame para o InventorySystem estar pronto
        Invoke(nameof(GiveItems), 0.1f);
    }

    private void GiveItems()
    {
        if (InventorySystem.Instance == null)
        {
            Debug.LogWarning("[StarterItems] InventorySystem não encontrado!");
            return;
        }

        foreach (var item in items)
            InventorySystem.Instance.AddItem(item.itemName, null, item.quantity);
    }
}
