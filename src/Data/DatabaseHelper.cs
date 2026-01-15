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

        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(ConnectionString);
        }

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
                            descripcion_enfermedad TEXT,
                            edad_recomendada TEXT,
                            dosis_esquema TEXT
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
                            cedula TEXT UNIQUE,
                            historia_clinica TEXT UNIQUE NOT NULL,
                            nombres TEXT NOT NULL,
                            apellidos TEXT NOT NULL,
                            fecha_nacimiento TEXT NOT NULL,
                            nacionalidad TEXT,
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
                
                SeedVacunas();
            }

            // Migrations for existing databases
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                try {
                    using (var cmd = new SQLiteCommand("ALTER TABLE Vacunas ADD COLUMN edad_recomendada TEXT", conn)) cmd.ExecuteNonQuery();
                } catch { } // Column exists or error
                
                try {
                    using (var cmd = new SQLiteCommand("ALTER TABLE Vacunas ADD COLUMN dosis_esquema TEXT", conn)) cmd.ExecuteNonQuery();
                } catch { } // Column exists or error

                // Migration for Pacientes (Cedula, Nacionalidad)
                try {
                    using (var cmd = new SQLiteCommand("ALTER TABLE Pacientes ADD COLUMN cedula TEXT", conn)) cmd.ExecuteNonQuery();
                } catch { }
                try {
                    using (var cmd = new SQLiteCommand("ALTER TABLE Pacientes ADD COLUMN nacionalidad TEXT", conn)) cmd.ExecuteNonQuery();
                } catch { }
            }
        }

        public static void SeedVacunas()
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                // Check if empty
                using (var cmdCount = new SQLiteCommand("SELECT COUNT(*) FROM Vacunas", conn))
                {
                    long count = (long)cmdCount.ExecuteScalar();
                    if (count > 0) return;
                }

                // Insert defaults based on the user provided image
                string[] vaccines = new[] {
                    "BCG|BCG|Tuberculosis",
                    "HB|HB|Hepatitis B",
                    "Rotavirus|ROTAVIRUS|Rotavirus",
                    "fIPV|FIPV|Polio Inactivada",
                    "bOPV|BOPV|Polio Oral Bivalente",
                    "Neumococo|NEUMOCOCO|Neumococo",
                    "Pentavalente|PENTAVALENTE|Difteria, Tétanos, Tosferina, Hepatitis B, Haemophilus influenzae tipo b",
                    "SRP|SRP|Sarampión, Rubéola, Paperas",
                    "FA|FA|Fiebre Amarilla",
                    "Varicela|VARICELA|Varicela",
                    "DPT|DPT|Difteria, Tétanos, Tosferina",
                    "HPV|HPV|Virus del Papiloma Humano",
                    "dT adulto|DT|Difteria y Tétanos (Adulto)"
                };

                using (var trans = conn.BeginTransaction())
                {
                    foreach (var v in vaccines)
                    {
                        var parts = v.Split('|');
                        string sql = "INSERT INTO Vacunas (nombre_biologico, siglas, descripcion_enfermedad) VALUES (@nom, @sig, @desc)";
                        using (var cmd = new SQLiteCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@nom", parts[0]);
                            cmd.Parameters.AddWithValue("@sig", parts[1]);
                            cmd.Parameters.AddWithValue("@desc", parts[2]);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
            }
        }

        public static void SeedPersonal()
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmdCount = new SQLiteCommand("SELECT COUNT(*) FROM Personal_Salud", conn))
                {
                    long count = (long)cmdCount.ExecuteScalar();
                    if (count > 0) return;
                }

                // Insert default user for imports
                string sql = "INSERT INTO Personal_Salud (id_personal, cedula, nombres_completos, cargo) VALUES (1, '9999999999', 'SISTEMA IMPORTACION', 'SISTEMA')";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void SeedDefaults()
        {
            try 
            {
                SeedVacunas();
                SeedPersonal();
            }
            catch (Exception ex)
            {
                // Create a silent log or rethrow dependent on needs
                Console.WriteLine("Error seeding defaults: " + ex.Message);
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

        public static object? ExecuteScalar(string query, SQLiteParameter[]? parameters = null)
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
                    return cmd.ExecuteScalar();
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
