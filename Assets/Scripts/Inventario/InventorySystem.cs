using UnityEngine;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [System.Serializable]
    public class ItemStack
    {
        public string itemName;
        public Sprite icon;
        public int quantity;
    }

    public const int HotbarSlots = 5;
    public const int InventoryRows = 4;
    public const int InventoryCols = 6;
    public const int InventorySlots = InventoryRows * InventoryCols;

    public const float MaxWeight = 50f;
    public const float HeavyThreshold = 45f;

    private static readonly Dictionary<string, float> itemWeights = new Dictionary<string, float>
    {
        { "Madeira",  0.2f },
        { "Galho",    0.1f },
        { "Pedra",    0.5f },
        { "Machado",  3.5f },
        { "Picareta", 5.0f },
        { "Floor",    5.0f },
        { "Wall",     5.0f },
    };

    public static float GetItemWeight(string name) =>
        itemWeights.TryGetValue(name, out float w) ? w : 0f;

    public float GetTotalWeight()
    {
        float total = 0f;
        foreach (var s in hotbar)
            if (s != null) total += GetItemWeight(s.itemName) * s.quantity;
        foreach (var s in inventory)
            if (s != null) total += GetItemWeight(s.itemName) * s.quantity;
        return total;
    }

    [System.NonSerialized] public ItemStack[] hotbar;
    [System.NonSerialized] public ItemStack[] inventory;

    public event System.Action OnInventoryChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        hotbar = new ItemStack[HotbarSlots];
        inventory = new ItemStack[InventorySlots];
        Debug.Log($"[Inventory] Awake — Instance ID={GetInstanceID()}");
    }

    public void NotifyChanged() => OnInventoryChanged?.Invoke();

    public bool AddItem(string itemName, Sprite icon = null, int quantity = 1)
    {
        float addedWeight = GetItemWeight(itemName) * quantity;
        if (GetTotalWeight() + addedWeight > MaxWeight)
        {
            Debug.Log("[Inventory] Inventário demasiado pesado!");
            return false;
        }

        Debug.Log($"[Inventory] AddItem: {itemName} x{quantity} | InstanceID={GetInstanceID()}");

        if (TryStack(hotbar, itemName, quantity)) { NotifyChanged(); return true; }
        if (TryStack(inventory, itemName, quantity)) { NotifyChanged(); return true; }
        if (TryEmpty(hotbar, itemName, icon, quantity)) { NotifyChanged(); return true; }
        if (TryEmpty(inventory, itemName, icon, quantity)) { NotifyChanged(); return true; }

        Debug.Log("[Inventory] Inventário cheio!");
        return false;
    }

    private bool TryStack(ItemStack[] slots, string name, int qty)
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] != null && slots[i].itemName == name)
            {
                slots[i].quantity += qty;
                return true;
            }
        return false;
    }

    private bool TryEmpty(ItemStack[] slots, string name, Sprite icon, int qty)
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null)
            {
                slots[i] = new ItemStack { itemName = name, icon = icon, quantity = qty };
                return true;
            }
        return false;
    }
}