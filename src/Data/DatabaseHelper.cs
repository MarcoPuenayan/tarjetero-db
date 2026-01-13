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
                    // Crear tablas usando el esquema definido
                    string createTablesQuery = @"
                        -- Habilita FK
                        PRAGMA foreign_keys = ON;

                        -- 1. Vacunas
                        CREATE TABLE IF NOT EXISTS Vacunas (
                            id_vacuna INTEGER PRIMARY KEY AUTOINCREMENT,
                            nombre_biologico TEXT NOT NULL,
                            siglas TEXT,
                            descripcion_enfermedad TEXT
                        );

                        -- 2. Personal
                        CREATE TABLE IF NOT EXISTS Personal_Salud (
                            id_personal INTEGER PRIMARY KEY AUTOINCREMENT,
                            cedula TEXT UNIQUE,
                            nombres_completos TEXT NOT NULL,
                            cargo TEXT
                        );

                        -- 3. Representantes
                        CREATE TABLE IF NOT EXISTS Representantes (
                            id_representante INTEGER PRIMARY KEY AUTOINCREMENT,
                            cedula TEXT UNIQUE,
                            nombres TEXT NOT NULL,
                            relacion TEXT,
                            telefono TEXT,
                            direccion TEXT
                        );

                        -- 4. Pacientes
                        CREATE TABLE IF NOT EXISTS Pacientes (
                            id_paciente INTEGER PRIMARY KEY AUTOINCREMENT,
                            historia_clinica TEXT UNIQUE NOT NULL,
                            nombres TEXT NOT NULL,
                            apellidos TEXT NOT NULL,
                            fecha_nacimiento TEXT NOT NULL,
                            sexo TEXT CHECK (sexo IN ('M', 'F')),
                            id_representante INTEGER,
                            FOREIGN KEY (id_representante) REFERENCES Representantes(id_representante)
                                ON DELETE SET NULL
                        );

                        -- 5. Registro Vacunacion
                        CREATE TABLE IF NOT EXISTS Registro_Vacunacion (
                            id_registro INTEGER PRIMARY KEY AUTOINCREMENT,
                            id_paciente INTEGER NOT NULL,
                            id_vacuna INTEGER NOT NULL,
                            id_personal INTEGER NOT NULL,
                            fecha_aplicacion TEXT DEFAULT CURRENT_TIMESTAMP,
                            numero_dosis TEXT NOT NULL,
                            lote_biologico TEXT,
                            edad_al_vacunar_meses INTEGER,
                            observaciones TEXT,
                            FOREIGN KEY (id_paciente) REFERENCES Pacientes(id_paciente) ON DELETE CASCADE,
                            FOREIGN KEY (id_vacuna) REFERENCES Vacunas(id_vacuna),
                            FOREIGN KEY (id_personal) REFERENCES Personal_Salud(id_personal)
                        );
                    ";

                    using (var cmd = new SQLiteCommand(createTablesQuery, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public static DataTable ExecuteQuery(string query, SQLiteParameter[]? parameters = null)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        return dt;
                    }
                }
            }
        }
        
        public static void ExecuteNonQuery(string query, SQLiteParameter[]? parameters = null)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
