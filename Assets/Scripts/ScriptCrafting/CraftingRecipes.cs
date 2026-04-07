using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CraftingRecipes", menuName = "Game/Crafting Recipes")]
public class CraftingRecipes : ScriptableObject
{
    [System.Serializable]
    public class Ingredient
    {
        public string itemName;
        public int quantity;
    }

    [System.Serializable]
    public class Recipe
    {
        public string recipeName;
        public Sprite icon;
        public List<Ingredient> ingredients = new List<Ingredient>();
        public int outputQuantity = 1;
    }

    public List<Recipe> recipes = new List<Recipe>();
}