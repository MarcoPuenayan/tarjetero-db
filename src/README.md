# Tarjetero de Vacunación - Aplicación Windows Forms

Esta aplicación ha sido generada con código C# para gestionar el tarjetero de vacunación.

## Requisitos
- .NET SDK (6.0 o superior)
- Visual Studio 2022 o VS Code con extensión C#

## Estructura del Proyecto
- **MainForm.cs**: Pantalla principal con menú.
- **PacientesForm.cs**: Pantalla para gestionar (buscar y crear) pacientes.
- **RegistroVacunasForm.cs**: Pantalla para ingresar nuevas vacunas.
- **Data/DatabaseHelper.cs**: Utilidad para conectar con SQLite.

## Ejecución
Desde la terminal en la carpeta `src`:

```bash
dotnet run
```

Esto compilará el proyecto y abrirá la ventana de Windows Forms.
La base de datos `tarjetero.db` se creará automáticamente la primera vez.

## Notas
Las interfaces gráficas han sido construidas por código (Code-only) para facilitar su portabilidad sin depender de archivos `.resx` complejos del diseñador visual en una primera fase.
