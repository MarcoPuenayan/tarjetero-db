-- Script de Creación de Base de Datos para Tarjetero de Vacunación (Versión SQLite)
-- Normalización: 3NF (Tercera Forma Normal)
-- Motor: SQLite

-- Habilita el soporte para claves foráneas (por defecto está apagado en algunas versiones de SQLite)
PRAGMA foreign_keys = ON;

-- 1. Tabla de Catálogo de Vacunas
CREATE TABLE Vacunas (
    id_vacuna INTEGER PRIMARY KEY AUTOINCREMENT,
    nombre_biologico TEXT NOT NULL,         -- Ej: Pentavalente, BCG
    siglas TEXT,                            -- Ej: DPT, SRP
    descripcion_enfermedad TEXT             -- Enfermedad que previene
);

-- 2. Tabla de Personal de Salud
CREATE TABLE Personal_Salud (
    id_personal INTEGER PRIMARY KEY AUTOINCREMENT,
    cedula TEXT UNIQUE,
    nombres_completos TEXT NOT NULL,
    cargo TEXT                              -- Ej: Enfermero/a, Médico
);

-- 3. Tabla de Representantes/Tutores
CREATE TABLE Representantes (
    id_representante INTEGER PRIMARY KEY AUTOINCREMENT,
    cedula TEXT UNIQUE,
    nombres TEXT NOT NULL,
    relacion TEXT,                          -- Madre, Padre, Tutor
    telefono TEXT,
    direccion TEXT
);

-- 4. Tabla de Pacientes
CREATE TABLE Pacientes (
    id_paciente INTEGER PRIMARY KEY AUTOINCREMENT,
    historia_clinica TEXT UNIQUE NOT NULL,  -- Identificador único (HC)
    nombres TEXT NOT NULL,
    apellidos TEXT NOT NULL,
    fecha_nacimiento TEXT NOT NULL,         -- SQLite no tiene tipo DATE, usa TEXT (ISO8601: YYYY-MM-DD)
    sexo TEXT CHECK (sexo IN ('M', 'F')),
    id_representante INTEGER,
    FOREIGN KEY (id_representante) REFERENCES Representantes(id_representante)
        ON DELETE SET NULL                  -- Si se borra el repre, el campo queda NULL
);

-- 5. Tabla Principal: Registro de Vacunación
CREATE TABLE Registro_Vacunacion (
    id_registro INTEGER PRIMARY KEY AUTOINCREMENT,
    id_paciente INTEGER NOT NULL,
    id_vacuna INTEGER NOT NULL,
    id_personal INTEGER NOT NULL,
    fecha_aplicacion TEXT DEFAULT CURRENT_TIMESTAMP, -- Formato YYYY-MM-DD HH:MM:SS
    
    -- Datos específicos
    numero_dosis TEXT NOT NULL,             -- Ej: '1ra Dosis', 'Refuerzo'
    lote_biologico TEXT,
    edad_al_vacunar_meses INTEGER,
    observaciones TEXT,
    
    -- Claves Foráneas
    FOREIGN KEY (id_paciente) REFERENCES Pacientes(id_paciente)
        ON DELETE CASCADE,                  -- Si se borra el paciente, se borran sus vacunas
    FOREIGN KEY (id_vacuna) REFERENCES Vacunas(id_vacuna),
    FOREIGN KEY (id_personal) REFERENCES Personal_Salud(id_personal)
);

-- Índices para optimizar consultas
CREATE INDEX idx_paciente_historia ON Pacientes(historia_clinica);
CREATE INDEX idx_vacunacion_fecha ON Registro_Vacunacion(fecha_aplicacion);
CREATE INDEX idx_vacunacion_paciente ON Registro_Vacunacion(id_paciente);
