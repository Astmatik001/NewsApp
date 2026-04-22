using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NewsApp.Models;

namespace NewsApp.Services
{
    public class LocalDatabaseService
    {
        private readonly string _connectionString;

        public LocalDatabaseService()
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "newsapp.db");
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            // User categories
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS user_categories (
                    user_id TEXT NOT NULL,
                    category TEXT NOT NULL,
                    PRIMARY KEY (user_id, category)
                );
                CREATE TABLE IF NOT EXISTS onboarding_status (
                    user_id TEXT PRIMARY KEY,
                    completed INTEGER NOT NULL
                );
                CREATE TABLE IF NOT EXISTS cached_articles (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    url TEXT UNIQUE,
                    title TEXT,
                    content_html TEXT,
                    category TEXT,
                    source TEXT,
                    publish_date TEXT
                );
            ";
            cmd.ExecuteNonQuery();
        }

        public async Task SaveUserCategoriesAsync(string userId, List<string> categories)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            // Clear old
            var delete = conn.CreateCommand();
            delete.CommandText = "DELETE FROM user_categories WHERE user_id = $user";
            delete.Parameters.AddWithValue("$user", userId);
            await delete.ExecuteNonQueryAsync();
            // Insert new
            foreach (var cat in categories)
            {
                var insert = conn.CreateCommand();
                insert.CommandText = "INSERT INTO user_categories (user_id, category) VALUES ($user, $cat)";
                insert.Parameters.AddWithValue("$user", userId);
                insert.Parameters.AddWithValue("$cat", cat);
                await insert.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<string>> GetUserCategoriesAsync(string userId)
        {
            var list = new List<string>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT category FROM user_categories WHERE user_id = $user";
            cmd.Parameters.AddWithValue("$user", userId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(reader.GetString(0));
            return list;
        }

        public async Task SetOnboardingCompletedAsync(string userId, bool completed)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO onboarding_status (user_id, completed) VALUES ($user, $comp)";
            cmd.Parameters.AddWithValue("$user", userId);
            cmd.Parameters.AddWithValue("$comp", completed ? 1 : 0);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> GetOnboardingCompletedAsync(string userId)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT completed FROM onboarding_status WHERE user_id = $user";
            cmd.Parameters.AddWithValue("$user", userId);
            var result = await cmd.ExecuteScalarAsync();
            return result != null && Convert.ToInt32(result) == 1;
        }

        public async Task CacheArticleAsync(Article article)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO cached_articles (url, title, content_html, category, source, publish_date)
                VALUES ($url, $title, $html, $cat, $src, $date)
            ";
            cmd.Parameters.AddWithValue("$url", article.Url);
            cmd.Parameters.AddWithValue("$title", article.Title);
            cmd.Parameters.AddWithValue("$html", article.ContentHtml ?? "");
            cmd.Parameters.AddWithValue("$cat", article.Category);
            cmd.Parameters.AddWithValue("$src", article.Source);
            cmd.Parameters.AddWithValue("$date", article.PublishDate.ToString("o"));
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<Article> GetCachedArticleByUrlAsync(string url)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT title, content_html, category, source, publish_date FROM cached_articles WHERE url = $url";
            cmd.Parameters.AddWithValue("$url", url);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Article
                {
                    Title = reader.GetString(0),
                    ContentHtml = reader.GetString(1),
                    Category = reader.GetString(2),
                    Source = reader.GetString(3),
                    PublishDate = DateTime.Parse(reader.GetString(4)),
                    Url = url
                };
            }
            return null;
        }
    }
}