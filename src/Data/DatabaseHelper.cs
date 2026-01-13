using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace TarjeteroApp.Data
{
    public static class DatabaseHelper
    {
        private static string DbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tarjetero.db");
        private static string ConnectionString => $"Data Source={DbPath};Version=3;";

        public static void InitializeDatabase()
        {
            if (!File.Exists(DbPath))
            {
                SQLiteConnection.CreateFile(DbPath);
                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    conn.Open();
                    // Aquí cargaríamos el esquema inicial
                    // Por brevedad, omitimos la creación de tablas por código y asumimos
                    // que se ejecuta el script .sql generado previamente
                }
            }
        }

        public static DataTable ExecuteQuery(string query)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(query, conn))
                using (var adapter = new SQLiteDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }
        
        public static void ExecuteNonQuery(string query)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
