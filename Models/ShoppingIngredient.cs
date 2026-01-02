public class ShoppingIngredient
{
    public int RecipeId { get; set; }
    public int IngredientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsChecked { get; set; } = false;
    public double Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}
