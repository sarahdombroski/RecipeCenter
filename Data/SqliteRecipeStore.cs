namespace TeamProjectYay.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

public static class SqliteRecipeStore
{
    private const string ConnectionString = "Data Source=Database/recipes.db";
    public static List<Recipe> LoadRecipes()
    {
        var recipes = new List<Recipe>();

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT recipe_id, recipe_name, recipe_image, recipe_source, recipe_url, description, prep_time, cook_time, total_time, servings, instructions
            FROM recipe
        ";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var recipe = new Recipe
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Image = reader.IsDBNull(2) ? null : reader.GetString(2),
                Source = reader.IsDBNull(3) ? null : reader.GetString(3),
                Url = reader.IsDBNull(4) ? null : reader.GetString(4),
                Description = reader.IsDBNull(5) ? null : reader.GetString(5),
                PrepTime = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                CookTime = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                TotalTime = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                Servings = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                Instructions = reader.IsDBNull(10) ? "" : reader.GetString(10),
                Ingredients = new List<string>() // Will fill next step
            };

            recipes.Add(recipe);
        }

        // Load individual ingredients for each recipe
        foreach (var recipe in recipes)
        {
            LoadIngredientsForRecipe(recipe, connection);
        }

        return recipes;
    }

    private static void LoadIngredientsForRecipe(Recipe recipe, SqliteConnection connection)
    /* Ingredients are stored in the ingredients table by name, and linked to the recipe on
       id's with the quantity stored with each link. Because of this, it's a little trickier
       to grab the ingredients for each recipe with the correct quantity, but is much easier
       to store, and makes more sense from the database side. */
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT i.ingredient_name, ri.quantity
            FROM recipe_ingredients ri
            JOIN ingredients i ON i.ingredient_id = ri.ingredient_id
            WHERE ri.recipe_id = $id
        ";

        cmd.Parameters.AddWithValue("$id", recipe.Id);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string ingredientName = reader.GetString(0);
            string quantity = reader.IsDBNull(1) ? "" : reader.GetString(1);

            recipe.Ingredients.Add($"{quantity}-{ingredientName}");
        }
    }

    public static void SaveRecipe(Recipe recipe)
    /* This method is more extensive than SaveUser because it uses a transaction, which is
       like a save point. Since we are saving to multiple tables, it is best practice to 
       'save' our progress before attempting to store the recipe in the database just in 
       case something goes wrong. */
    {
        using var connection = new SqliteConnection(ConnectionString);
    connection.Open();

    using var transaction = connection.BeginTransaction();

    // Insert recipe into recipe table
    var insertRecipeCmd = connection.CreateCommand();
    insertRecipeCmd.Transaction = transaction;
    insertRecipeCmd.CommandText = @"
        INSERT INTO recipe 
            (recipe_name, recipe_image, recipe_source, recipe_url, description, prep_time, cook_time, total_time, servings, instructions)
        VALUES 
            ($name, $image, $source, $url, $description, $prep, $cook, $total, $servings, $instructions);
    ";
    insertRecipeCmd.Parameters.AddWithValue("$name", recipe.Name);
    insertRecipeCmd.Parameters.AddWithValue("$image", (object?)recipe.Image ?? "images/GenericRecipe.png");
    insertRecipeCmd.Parameters.AddWithValue("$source", (object?)recipe.Source ?? DBNull.Value);
    insertRecipeCmd.Parameters.AddWithValue("$url", (object?)recipe.Url ?? DBNull.Value);
    insertRecipeCmd.Parameters.AddWithValue("$description", (object?)recipe.Description ?? DBNull.Value);
    insertRecipeCmd.Parameters.AddWithValue("$prep", (object?)recipe.PrepTime ?? DBNull.Value);
    insertRecipeCmd.Parameters.AddWithValue("$cook", (object?)recipe.CookTime ?? DBNull.Value);
    insertRecipeCmd.Parameters.AddWithValue("$total", (object?)recipe.TotalTime ?? DBNull.Value);
    insertRecipeCmd.Parameters.AddWithValue("$servings", (object?)recipe.Servings ?? DBNull.Value);
    insertRecipeCmd.Parameters.AddWithValue("$instructions", (object?)recipe.Instructions ?? DBNull.Value);

    insertRecipeCmd.ExecuteNonQuery();

    // Get the last inserted recipe id
    var getIdCmd = connection.CreateCommand();
    getIdCmd.Transaction = transaction;
    getIdCmd.CommandText = "SELECT last_insert_rowid();";
    recipe.Id = Convert.ToInt32(getIdCmd.ExecuteScalar());

    // Insert ingredients into ingredients table and linking table recipe_ingredients
    foreach (var ingredient in recipe.Ingredients)
    {
        int ingredientId;
        var ingredientParts = ingredient.Split('-');
        var ingredientQuantity = ingredientParts[0];
        var ingredientName = ingredientParts[1];

        // Check if ingredient exists
        var lookupCmd = connection.CreateCommand();
        lookupCmd.Transaction = transaction;
        lookupCmd.CommandText = "SELECT ingredient_id FROM ingredients WHERE ingredient_name = $name;";
        lookupCmd.Parameters.AddWithValue("$name", ingredientName);

        var result = lookupCmd.ExecuteScalar();
        if (result != null)
        {
            ingredientId = Convert.ToInt32(result);
        }
        else
        {
            // Insert ingredient if not already exists
            var insertIngredientCmd = connection.CreateCommand();
            insertIngredientCmd.Transaction = transaction;
            insertIngredientCmd.CommandText = @"
                INSERT INTO ingredients (ingredient_name) VALUES ($name);
                SELECT last_insert_rowid();
            ";
            insertIngredientCmd.Parameters.AddWithValue("$name", ingredientName);
            ingredientId = Convert.ToInt32(insertIngredientCmd.ExecuteScalar());
        }

        // Insert into recipe_ingredients (quantity currently null, will update later)
        var linkCmd = connection.CreateCommand();
        linkCmd.Transaction = transaction;
        linkCmd.CommandText = @"
            INSERT INTO recipe_ingredients (recipe_id, ingredient_id, quantity)
            VALUES ($recipeId, $ingredientId, $quantity);
        ";
        linkCmd.Parameters.AddWithValue("$recipeId", recipe.Id);
        linkCmd.Parameters.AddWithValue("$ingredientId", ingredientId);
        linkCmd.Parameters.AddWithValue("$quantity", ingredientQuantity);

        linkCmd.ExecuteNonQuery();
    }

    transaction.Commit();
    }

    public static void DeleteRecipe(int id)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        // Delete foreign keys from user recipe
        var deleteUserLink = connection.CreateCommand();
        deleteUserLink.CommandText = "DELETE FROM user_recipe WHERE recipe_id = $id";
        deleteUserLink.Parameters.AddWithValue("$id", id);
        deleteUserLink.ExecuteNonQuery();

        // Delete foreign keys from recipe ingredients
        var deleteLinks = connection.CreateCommand();
        deleteLinks.CommandText = "DELETE FROM recipe_ingredients WHERE recipe_id = $id";
        deleteLinks.Parameters.AddWithValue("$id", id);
        deleteLinks.ExecuteNonQuery();

        // Delete recipe
        var deleteRecipe = connection.CreateCommand();
        deleteRecipe.CommandText = "DELETE FROM recipe WHERE recipe_id = $id";
        deleteRecipe.Parameters.AddWithValue("$id", id);
        deleteRecipe.ExecuteNonQuery();
    }
    public static void UpdateRecipe(Recipe recipe)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        // Update recipe table
        var updateRecipeCmd = connection.CreateCommand();
        updateRecipeCmd.Transaction = transaction;
        updateRecipeCmd.CommandText = @"
            UPDATE recipe SET
                recipe_name = $name,
            recipe_image = $image,
            recipe_source = $source,
            recipe_url = $url,
            description = $description,
            prep_time = $prep,
            cook_time = $cook,
            total_time = $total,
            servings = $servings,
            instructions = $instructions
        WHERE recipe_id = $id;
        ";

        // add values to update
        updateRecipeCmd.Parameters.AddWithValue("$id", recipe.Id);
        updateRecipeCmd.Parameters.AddWithValue("$name", recipe.Name);
        updateRecipeCmd.Parameters.AddWithValue("$image", (object?)recipe.Image ?? "images/GenericRecipe.png");
        updateRecipeCmd.Parameters.AddWithValue("$source", (object?)recipe.Source ?? DBNull.Value);
        updateRecipeCmd.Parameters.AddWithValue("$url", (object?)recipe.Url ?? DBNull.Value);
        updateRecipeCmd.Parameters.AddWithValue("$description", (object?)recipe.Description ?? DBNull.Value);
        updateRecipeCmd.Parameters.AddWithValue("$prep", (object?)recipe.PrepTime ?? DBNull.Value);
        updateRecipeCmd.Parameters.AddWithValue("$cook", (object?)recipe.CookTime ?? DBNull.Value);
        updateRecipeCmd.Parameters.AddWithValue("$total", (object?)recipe.TotalTime ?? DBNull.Value);
        updateRecipeCmd.Parameters.AddWithValue("$servings", (object?)recipe.Servings ?? DBNull.Value);
        updateRecipeCmd.Parameters.AddWithValue("$instructions", (object?)recipe.Instructions ?? DBNull.Value);

        updateRecipeCmd.ExecuteNonQuery();

        // Delete ingredient links
        var deleteLinksCmd = connection.CreateCommand();
        deleteLinksCmd.Transaction = transaction;
        deleteLinksCmd.CommandText = "DELETE FROM recipe_ingredients WHERE recipe_id = $id;";
        deleteLinksCmd.Parameters.AddWithValue("$id", recipe.Id);
        deleteLinksCmd.ExecuteNonQuery();

        // Reinsert ingredients
        foreach (var ingredientName in recipe.Ingredients)
        {
            int ingredientId;

            var lookupCmd = connection.CreateCommand();
            lookupCmd.Transaction = transaction;
            lookupCmd.CommandText = "SELECT ingredient_id FROM ingredients WHERE ingredient_name = $name;";
            lookupCmd.Parameters.AddWithValue("$name", ingredientName);

            var result = lookupCmd.ExecuteScalar();
            if (result != null)
            {
                ingredientId = Convert.ToInt32(result);
            }
            else
            {
                var insertIngredientCmd = connection.CreateCommand();
                insertIngredientCmd.Transaction = transaction;
                insertIngredientCmd.CommandText = @"
                    INSERT INTO ingredients (ingredient_name) VALUES ($name);
                    SELECT last_insert_rowid();
                ";
                insertIngredientCmd.Parameters.AddWithValue("$name", ingredientName);
                ingredientId = Convert.ToInt32(insertIngredientCmd.ExecuteScalar());
            }

            var linkCmd = connection.CreateCommand();
            linkCmd.Transaction = transaction;
            linkCmd.CommandText = @"
                INSERT INTO recipe_ingredients (recipe_id, ingredient_id, quantity)
                VALUES ($recipeId, $ingredientId, $quantity);
            ";
            linkCmd.Parameters.AddWithValue("$recipeId", recipe.Id);
            linkCmd.Parameters.AddWithValue("$ingredientId", ingredientId);
            linkCmd.Parameters.AddWithValue("$quantity", DBNull.Value);

            linkCmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }
    public static void LinkRecipeToUser(int userId, int recipeId)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        // insert into the database so the user can see their recipes
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO user_recipe (user_id, recipe_id)
            VALUES ($user, $recipe)
        ";
        cmd.Parameters.AddWithValue("$user", userId); // adds user id
        cmd.Parameters.AddWithValue("$recipe", recipeId); // adds recipe id
        cmd.ExecuteNonQuery();
    }
    public static List<Recipe> LoadRecipesForUser(int userId)
    {
        var recipes = new List<Recipe>();

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT r.recipe_id, r.recipe_name, r.recipe_image, r.recipe_source, r.recipe_url, r.description, r.prep_time, r.cook_time, r.total_time, r.servings, r.instructions
            FROM recipe r
            JOIN user_recipe ur ON ur.recipe_id = r.recipe_id
            WHERE ur.user_id = $userId
        "; // select only the recipes the user added
        cmd.Parameters.AddWithValue("$userId", userId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var recipe = new Recipe
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Image = reader.IsDBNull(2) ? null : reader.GetString(2),
                Source = reader.IsDBNull(3) ? null : reader.GetString(3),
                Url = reader.IsDBNull(4) ? null : reader.GetString(4),
                Description = reader.IsDBNull(5) ? null : reader.GetString(5),
                PrepTime = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                CookTime = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                TotalTime = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                Servings = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                Instructions = reader.IsDBNull(10) ? "" : reader.GetString(10),
                Ingredients = new List<string>() // Will fill next step
            };

            recipes.Add(recipe); // add recipe to the list
        }

        // Load individual ingredients for each recipe
        foreach (var recipe in recipes)
        {
            LoadIngredientsForRecipe(recipe, connection);
        }

        return recipes;
    }

    public static List<int> LoadShoppingListForUser(int userId)
    {
        var shoppingList = new List<int>();

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT user_id, recipe_id
            FROM shopping
            WHERE user_id = $userId
        ";
        cmd.Parameters.AddWithValue("$userId", userId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            int user = reader.GetInt32(0);
            int recipe = reader.GetInt32(1);
            shoppingList.Add(recipe);
        }

        return shoppingList;
    }

    public static void AddToShoppingList(int userId, int recipeId)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO shopping (user_id, recipe_id)
            VALUES ($user, $recipe)
        ";
        cmd.Parameters.AddWithValue("$user", userId);
        cmd.Parameters.AddWithValue("$recipe", recipeId);
        cmd.ExecuteNonQuery();
    }

    public static void RemoveFromShoppingList(int userId, int recipeId)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM shopping 
            WHERE user_id = $user AND recipe_id = $recipe
        ";
        cmd.Parameters.AddWithValue("$user", userId);
        cmd.Parameters.AddWithValue("$recipe", recipeId);
        cmd.ExecuteNonQuery();
    }

    public static void ClearUserIngredients(int userId)
    {
        using var conn = new SqliteConnection("Data Source=Database/recipes.db");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM user_ingredients
            WHERE user_id = $userId;
        ";
        cmd.Parameters.AddWithValue("$userId", userId);

        cmd.ExecuteNonQuery();
    }

    public static void ClearShoppingListForUser(int userId)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM shopping 
            WHERE user_id = $user
        ";
        cmd.Parameters.AddWithValue("$user", userId);
        cmd.ExecuteNonQuery();
    }

    public static void UpdateIngredientChecked(int userId, int recipeId, int ingredientId, bool isChecked)
    {
        using var conn = new SqliteConnection("Data Source=Database/recipes.db");
        conn.Open();

        // Upsert logic: insert if missing, update if exists
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO user_ingredients (user_id, recipe_id, ingredient_id, is_checked)
            VALUES ($userId, $recipeId, $ingredientId, $isChecked)
            ON CONFLICT(user_id, recipe_id, ingredient_id)
            DO UPDATE SET is_checked = $isChecked;
        ";

        cmd.Parameters.AddWithValue("$userId", userId);
        cmd.Parameters.AddWithValue("$recipeId", recipeId);
        cmd.Parameters.AddWithValue("$ingredientId", ingredientId);
        cmd.Parameters.AddWithValue("$isChecked", isChecked ? 1 : 0);
        Console.WriteLine($"Updating ingredient checked status: UserId={userId}, RecipeId={recipeId}, IngredientId={ingredientId}, IsChecked={isChecked}");
        cmd.ExecuteNonQuery();
    }

    public static bool IsIngredientChecked(int userId, int recipeId, int ingredientId)
    {
        using var conn = new SqliteConnection("Data Source=Database/recipes.db");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT is_checked
            FROM user_ingredients
            WHERE user_id = $userId AND recipe_id = $recipeId AND ingredient_id = $ingredientId;
        ";

        cmd.Parameters.AddWithValue("$userId", userId);
        cmd.Parameters.AddWithValue("$recipeId", recipeId);
        cmd.Parameters.AddWithValue("$ingredientId", ingredientId);

        var result = cmd.ExecuteScalar();
        return result != null && Convert.ToInt32(result) == 1;
    }

    public static int LookupIngredientId(string ingredientName)
    {
        using var conn = new SqliteConnection("Data Source=Database/recipes.db");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT ingredient_id
            FROM ingredients
            WHERE ingredient_name = $name
            LIMIT 1;
        ";
        cmd.Parameters.AddWithValue("$name", ingredientName);

        var result = cmd.ExecuteScalar();
        if (result != null && int.TryParse(result.ToString(), out int id))
        {
            return id;
        }

        // If not found, return 0
        return 0;
    }
}