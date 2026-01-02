namespace TeamProjectYay.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;


// public static class FileUserStore
// {
//     private static readonly string filePath = "Data/users.json";
//     private static readonly PasswordHasher<AppUser> hasher = new();

//     public static List<AppUser> LoadUsers()
//     {
//         if (!File.Exists(filePath)) return new List<AppUser>();
//         var json = File.ReadAllText(filePath);
//         return JsonSerializer.Deserialize<List<AppUser>>(json) ?? new List<AppUser>();
//     }

//     public static void SaveUsers(List<AppUser> users)
//     {
//         var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
//         File.WriteAllText(filePath, json);
//     }

//     public static bool VerifyPassword(AppUser user, string password)
//     {
//         return hasher.VerifyHashedPassword(user, user.HashedPassword, password) == PasswordVerificationResult.Success;
//     }

//     public static string HashPassword(AppUser user, string password)
//     {
//         return hasher.HashPassword(user, password);
//     }
// }


public static class SqliteUserStore
{
    private const string ConnectionString = "Data Source=Database/recipes.db";
    private static readonly PasswordHasher<AppUser> hasher = new();

    public static List<AppUser> LoadUsers()
    {
        var users = new List<AppUser>();

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT user_id, username, first_name, last_name, hashed_password, profile_picture_path 
            FROM users
        ";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            users.Add(new AppUser
            {
                UserId = reader.GetInt32(0),
                Username = reader.GetString(1),
                FirstName = reader.IsDBNull(2) ? null : reader.GetString(2),
                LastName = reader.IsDBNull(3) ? null : reader.GetString(3),
                HashedPassword = reader.GetString(4),
                ProfilePicturePath = reader.IsDBNull(5) ? null : reader.GetString(5),
            });
        }

        return users;
    }

    public static void SaveUser(AppUser user)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO users (username, first_name, last_name, hashed_password, profile_picture_path)
            VALUES ($username, $first, $last, $hash, $pic)
        ";
        cmd.Parameters.AddWithValue("$username", user.Username);
        cmd.Parameters.AddWithValue("$first", user.FirstName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$last", user.LastName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$hash", user.HashedPassword);
        cmd.Parameters.AddWithValue("$pic", user.ProfilePicturePath ?? (object)DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    public static AppUser? GetUserByUsername(string username)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT user_id, username, first_name, last_name, hashed_password, profile_picture_path 
            FROM users
            WHERE username = $u
        ";
        cmd.Parameters.AddWithValue("$u", username);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new AppUser
        {
            UserId = reader.GetInt32(0),
            Username = reader.GetString(1),
            FirstName = reader.IsDBNull(2) ? null : reader.GetString(2),
            LastName = reader.IsDBNull(3) ? null : reader.GetString(3),
            HashedPassword = reader.GetString(4),
            ProfilePicturePath = reader.IsDBNull(5) ? null : reader.GetString(5)
        };
    }

    public static bool VerifyPassword(AppUser user, string password)
    {
        return hasher.VerifyHashedPassword(user, user.HashedPassword, password)
            == PasswordVerificationResult.Success;
    }

    public static string HashPassword(AppUser user, string password)
    {
        return hasher.HashPassword(user, password);
    }
    public static void UpdateUser(AppUser user)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE users SET
                first_name = $first,
                last_name = $last,
                profile_picture_path = $pic,
                hashed_password = $hash
            WHERE user_id = $id
            "; 
        
        // update user based on new values
        cmd.Parameters.AddWithValue("$first", user.FirstName);
        cmd.Parameters.AddWithValue("last", user.LastName);
        cmd.Parameters.AddWithValue("$pic", user.ProfilePicturePath);
        cmd.Parameters.AddWithValue("$hash", user.HashedPassword);
        cmd.Parameters.AddWithValue("$username", user.Username);
        cmd.Parameters.AddWithValue("$id", user.UserId);

        cmd.ExecuteNonQuery();
    }
}
