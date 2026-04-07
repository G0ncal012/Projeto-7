using UnityEngine;
using System.Collections.Generic;

public class CraftingSystem : MonoBehaviour
{
    public static CraftingSystem Instance { get; private set; }

    [Tooltip("Arrasta aqui o ScriptableObject CraftingRecipes")]
    [SerializeField] public CraftingRecipes recipes;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool CanCraft(CraftingRecipes.Recipe recipe)
    {
        if (InventorySystem.Instance == null) { Debug.Log("[Craft] InventorySystem NULL"); return false; }
        Debug.Log($"[Craft] CanCraft — InventorySystem InstanceID={InventorySystem.Instance.GetInstanceID()}");
        foreach (var ing in recipe.ingredients)
        {
            int count = CountItem(ing.itemName);
            Debug.Log($"[Craft] {ing.itemName}: tenho={count} preciso={ing.quantity}");
            if (count < ing.quantity) return false;
        }
        return true;
    }

    public bool TryCraft(CraftingRecipes.Recipe recipe)
    {
        if (!CanCraft(recipe)) return false;

        foreach (var ing in recipe.ingredients)
            RemoveItem(ing.itemName, ing.quantity);

        InventorySystem.Instance.AddItem(recipe.recipeName, recipe.icon, recipe.outputQuantity);
        return true;
    }

    private int CountItem(string itemName)
    {
        var inv = InventorySystem.Instance;
        Debug.Log($"[Craft] CountItem({itemName}) — InventorySystem InstanceID={inv.GetInstanceID()}");

        int count = 0;
        foreach (var s in inv.hotbar)
            if (s != null && s.itemName == itemName) count += s.quantity;
        foreach (var s in inv.inventory)
            if (s != null && s.itemName == itemName) count += s.quantity;

        Debug.Log($"[Craft] CountItem({itemName}) = {count}");
        return count;
    }

    private void RemoveItem(string itemName, int quantity)
    {
        var inv = InventorySystem.Instance;
        int remaining = quantity;

        for (int i = 0; i < inv.hotbar.Length && remaining > 0; i++)
        {
            if (inv.hotbar[i] != null && inv.hotbar[i].itemName == itemName)
            {
                int take = Mathf.Min(remaining, inv.hotbar[i].quantity);
                inv.hotbar[i].quantity -= take;
                remaining -= take;
                if (inv.hotbar[i].quantity <= 0) inv.hotbar[i] = null;
            }
        }

        for (int i = 0; i < inv.inventory.Length && remaining > 0; i++)
        {
            if (inv.inventory[i] != null && inv.inventory[i].itemName == itemName)
            {
                int take = Mathf.Min(remaining, inv.inventory[i].quantity);
                inv.inventory[i].quantity -= take;
                remaining -= take;
                if (inv.inventory[i].quantity <= 0) inv.inventory[i] = null;
            }
        }

        inv.NotifyChanged();
    }
}