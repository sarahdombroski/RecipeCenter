public class Recipe
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ?Image { get; set; }
    public string ?Source { get; set; }
    public string ?Url { get; set; }
    public string ?Description { get; set; }
    public int ?PrepTime { get; set; }
    public int ?CookTime { get; set; }
    public int ?TotalTime { get; set; }
    public int ?Servings { get; set; }
    public List<string> Ingredients { get; set; }
    public string Instructions { get; set; }
    public List<string> SharedUsers { get; set; } = new();

    public Recipe()
    {
        Name = "New Recipe";
        Description = "Description";
        Ingredients = new List<string>();
        Instructions = "Instructions";
    }
}