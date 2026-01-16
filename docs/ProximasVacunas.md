# 📅 Gestión de Próximas Vacunaciones

## Descripción
Pantalla para gestionar y visualizar pacientes próximos a vacunarse según su edad actual y el esquema nacional de vacunación.

## Características Principales

### 1. **Cálculo Automático de Edad**
- Calcula la edad del paciente en meses desde la fecha de nacimiento
- Muestra edad en formato legible: "X años Y meses" o "Z meses"
- Actualización en tiempo real

### 2. **Seguimiento de Vacunación**
- Muestra la última vacuna aplicada a cada paciente
- Indica la dosis aplicada (1ra, 2da, 3ra, Refuerzo)
- Fecha de la última vacuna
- Días transcurridos desde la última vacunación

### 3. **Próximas Vacunas Sugeridas**
El sistema sugiere automáticamente las vacunas según la edad del paciente:

| Edad | Vacunas Sugeridas |
|------|-------------------|
| Recién nacido | BCG, Hepatitis B |
| 2 meses | Pentavalente 1ra, Rotavirus 1ra, Neumococo 1ra, Polio 1ra |
| 4 meses | Pentavalente 2da, Rotavirus 2da, Neumococo 2da, Polio 2da |
| 6 meses | Pentavalente 3ra, Polio 3ra, Influenza 1ra |
| 7 meses | Influenza 2da |
| 12 meses | SRP 1ra, Neumococo Refuerzo, Varicela |
| 15 meses | Fiebre Amarilla |
| 18 meses | DPT Refuerzo, Polio Refuerzo |
| 4-5 años | SRP 2da |
| 6 años | DPT 2do Refuerzo |

### 4. **Indicadores de Estado**
- 🟢 **Al día**: Menos de 60 días desde la última vacuna
- 🟠 **Próximo**: Entre 60-90 días desde la última vacuna
- 🟡 **Atrasado**: Más de 90 días desde la última vacuna
- 🔴 **Sin vacunas**: Paciente sin registro de vacunación

### 5. **Filtros Avanzados**
- **Por Paciente**: Buscar por nombre, HC o cédula
- **Por Vacuna**: Filtrar por vacuna específica (última o próxima)
- **Por Rango de Edad**:
  - 0-2 meses (Recién nacido)
  - 2-4 meses
  - 4-6 meses
  - 6-12 meses
  - 12-18 meses (1-1.5 años)
  - 18-24 meses (1.5-2 años)
  - 2-5 años
  - 5-10 años
  - 10+ años

### 6. **Exportación de Datos**
- Exportar a CSV para análisis en Excel
- Formato compatible con hojas de cálculo
- Incluye todos los datos relevantes

## Columnas del Grid

| Columna | Descripción |
|---------|-------------|
| Historia Clínica | Identificador único del paciente |
| Paciente | Nombre completo del paciente |
| Sexo | M/F |
| Edad | Edad calculada (años y meses) |
| Representante | Nombre del representante/tutor |
| Teléfono | Teléfono de contacto |
| Última Vacuna | Última vacuna aplicada |
| Dosis | Número de dosis de la última vacuna |
| Fecha Última Vacuna | Fecha de aplicación |
| Días Transcurridos | Días desde la última vacunación |
| Próxima Vacuna Sugerida | Vacunas recomendadas según edad |
| Estado | Indicador visual del estado de vacunación |

## Uso

### Acceso
Menú principal → **Vacunación** → **📅 Próximas Vacunaciones**

### Filtrar Pacientes
1. Usar el campo "Buscar Paciente" para buscar por nombre, HC o cédula
2. Seleccionar una vacuna específica en el combo "Vacuna"
3. Seleccionar un rango de edad en "Rango de Edad"
4. Los filtros se aplican automáticamente

### Refrescar Datos
- Click en botón **🔄 Refrescar** para actualizar la lista con datos actuales

### Exportar Reportes
1. Click en **📊 Exportar Excel**
2. Seleccionar ubicación y nombre del archivo
3. El archivo CSV se puede abrir en Excel

## Cálculos Técnicos

### Edad en Meses
```sql
CAST((JULIANDAY('now') - JULIANDAY(fecha_nacimiento)) / 30.44 AS INTEGER)
```

### Días desde Última Vacuna
```sql
CAST((JULIANDAY('now') - JULIANDAY(fecha_ultima_vacuna)) AS INTEGER)
```

### Estado de Vacunación
- **Sin vacunas**: `fecha_ultima_vacuna IS NULL`
- **Atrasado**: `dias_desde_ultima > 90`
- **Próximo**: `dias_desde_ultima > 60`
- **Al día**: `dias_desde_ultima <= 60`

## Beneficios

1. **Prevención**: Identifica pacientes que necesitan vacunación próximamente
2. **Planificación**: Permite organizar jornadas de vacunación por grupos de edad
3. **Seguimiento**: Monitorea el cumplimiento del esquema de vacunación
4. **Contacto**: Acceso rápido a teléfonos de representantes para recordatorios
5. **Reportes**: Exportación para análisis estadísticos y reportes

## Notas Importantes

- Las edades y sugerencias se calculan en tiempo real
- El esquema de vacunación sigue las normativas nacionales de Ecuador
- Los colores facilitan la identificación visual de prioridades
- Los datos se actualizan automáticamente al registrar nuevas vacunas

## Futuras Mejoras

- [ ] Notificaciones automáticas por WhatsApp/SMS
- [ ] Generación de recordatorios por email
- [ ] Reportes estadísticos avanzados
- [ ] Gráficos de cobertura de vacunación
- [ ] Integración con calendario para agendar citas
