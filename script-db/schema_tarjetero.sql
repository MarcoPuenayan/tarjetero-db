-- Script de Creación de Base de Datos para Tarjetero de Vacunación
-- Normalización: 3NF (Tercera Forma Normal)
-- Objetivo: Eliminar redundancia de datos de pacientes y estandarizar el catálogo de vacunas.

-- 1. Tabla de Catálogo de Vacunas (Entidad Fuerte)
-- Almacena los tipos de vacunas disponibles para evitar repetir nombres.
CREATE TABLE Vacunas (
    id_vacuna INT PRIMARY KEY IDENTITY(1,1),
    nombre_biologico VARCHAR(100) NOT NULL, -- Ej: Pentavalente, BCG, Rotavirus
    siglas VARCHAR(20),                     -- Ej: DPT, SRP, OPV
    descripcion_enfermedad VARCHAR(255)     -- Enfermedad que previene (Ej: Difteria, Tosferina, Tétanos)
);

-- 2. Tabla de Personal de Salud (Entidad Fuerte)
-- Registra a quienes administran las vacunas.
CREATE TABLE Personal_Salud (
    id_personal INT PRIMARY KEY IDENTITY(1,1),
    cedula VARCHAR(20) UNIQUE,
    nombres_completos VARCHAR(150) NOT NULL,
    cargo VARCHAR(50) -- Ej: Enfermero/a, Médico, TENS
);

-- 3. Tabla de Representantes/Tutores (Entidad Fuerte)
-- Separado de pacientes porque un representante puede tener varios hijos/pacientes.
CREATE TABLE Representantes (
    id_representante INT PRIMARY KEY IDENTITY(1,1),
    cedula VARCHAR(20) UNIQUE,
    nombres VARCHAR(150) NOT NULL,
    relacion VARCHAR(50), -- Madre, Padre, Abuela/o, Tutor Legal
    telefono VARCHAR(20),
    direccion TEXT
);

-- 4. Tabla de Pacientes (Entidad Fuerte con dependencia)
-- Información demográfica del paciente.
CREATE TABLE Pacientes (
    id_paciente INT PRIMARY KEY IDENTITY(1,1),
    historia_clinica VARCHAR(50) UNIQUE NOT NULL, -- Identificador único sanitario (HC)
    nombres VARCHAR(100) NOT NULL,
    apellidos VARCHAR(100) NOT NULL,
    fecha_nacimiento DATE NOT NULL,
    sexo CHAR(1) CHECK (sexo IN ('M', 'F')),
    id_representante INT, -- FK opcional si el paciente es adulto, obligatorio si es niño
    FOREIGN KEY (id_representante) REFERENCES Representantes(id_representante)
);

-- 5. Tabla Principal: Registro de Vacunación (Entidad Transaccional)
-- Esta tabla registra el evento de vacunación. Conecta Paciente + Vacuna + Personal + Tiempo.
CREATE TABLE Registro_Vacunacion (
    id_registro INT PRIMARY KEY IDENTITY(1,1),
    id_paciente INT NOT NULL,
    id_vacuna INT NOT NULL,
    id_personal INT NOT NULL, -- Obligatorio para responsabilidad médica
    fecha_aplicacion DATETIME DEFAULT GETDATE(),
    
    -- Datos específicos de la aplicación
    numero_dosis VARCHAR(50) NOT NULL,  -- Ej: '1ra Dosis', '2da Dosis', 'Refuerzo', 'Unica'
    lote_biologico VARCHAR(50),         -- Crucial para trazabilidad sanitaria en caso de reacciones adversas
    edad_al_vacunar_meses INT,          -- Edad del paciente calculada al momento de la vacuna
    observaciones TEXT,                 -- Reacciones, notas adicionales
    
    -- Claves Foráneas (Relaciones)
    FOREIGN KEY (id_paciente) REFERENCES Pacientes(id_paciente),
    FOREIGN KEY (id_vacuna) REFERENCES Vacunas(id_vacuna),
    FOREIGN KEY (id_personal) REFERENCES Personal_Salud(id_personal)
);

-- Índices para optimizar consultas frecuentes
-- Búsqueda rápida por Historia Clínica
CREATE INDEX idx_paciente_historia ON Pacientes(historia_clinica);
-- Reportes de vacunación por fecha
CREATE INDEX idx_vacunacion_fecha ON Registro_Vacunacion(fecha_aplicacion);
-- Búsqueda de historial de vacunas de un paciente
CREATE INDEX idx_vacunacion_paciente ON Registro_Vacunacion(id_paciente);
