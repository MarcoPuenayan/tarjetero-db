using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SQLite;
using TarjeteroApp.Data;

namespace TarjeteroApp
{
    /// <summary>
    /// Formulario para gestionar pacientes próximos a vacunarse
    /// Calcula la edad del paciente, encuentra la última vacuna y determina la próxima dosis
    /// </summary>
    public class ProximasVacunasForm : Form
    {
        private DataGridView gridProximas = null!;
        private ComboBox cmbFiltroVacuna = null!, cmbFiltroRangoEdad = null!;
        private Button btnRefrescar = null!, btnExportar = null!;
        private Label lblTotalPacientes = null!;
        private TextBox txtBuscar = null!;

        public ProximasVacunasForm()
        {
            InitializeComponent();
            CargarCombosFiltro();
            CargarDatos();
        }

        private void InitializeComponent()
        {
            this.Text = "Gestión de Próximas Vacunaciones";
            this.Size = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Main Container
            SplitContainer splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 120,
                BorderStyle = BorderStyle.Fixed3D,
                SplitterWidth = 5
            };

            // Panel Superior: Filtros
            Panel panelFiltros = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            GroupBox grpFiltros = new GroupBox 
            { 
                Text = "Filtros de Búsqueda", 
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            TableLayoutPanel layoutFiltros = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 4,
                AutoSize = true
            };

            // Fila 0: Labels
            layoutFiltros.Controls.Add(new Label { Text = "Buscar Paciente:", AutoSize = true }, 0, 0);
            layoutFiltros.Controls.Add(new Label { Text = "Vacuna:", AutoSize = true }, 1, 0);
            layoutFiltros.Controls.Add(new Label { Text = "Rango de Edad:", AutoSize = true }, 2, 0);

            // Fila 1: Controles
            txtBuscar = new TextBox { Width = 250, PlaceholderText = "Nombre, HC o Cédula..." };
            txtBuscar.TextChanged += (s, e) => FiltrarGrid();
            layoutFiltros.Controls.Add(txtBuscar, 0, 1);

            cmbFiltroVacuna = new ComboBox 
            { 
                Width = 250, 
                DropDownStyle = ComboBoxStyle.DropDownList 
            };
            cmbFiltroVacuna.SelectedIndexChanged += (s, e) => FiltrarGrid();
            layoutFiltros.Controls.Add(cmbFiltroVacuna, 1, 1);

            cmbFiltroRangoEdad = new ComboBox 
            { 
                Width = 200, 
                DropDownStyle = ComboBoxStyle.DropDownList 
            };
            cmbFiltroRangoEdad.Items.AddRange(new object[] 
            { 
                "Todas las edades",
                "0-2 meses (Recién nacido)",
                "2-4 meses",
                "4-6 meses",
                "6-12 meses",
                "12-18 meses (1-1.5 años)",
                "18-24 meses (1.5-2 años)",
                "2-5 años",
                "5-10 años",
                "10+ años"
            });
            cmbFiltroRangoEdad.SelectedIndex = 0;
            cmbFiltroRangoEdad.SelectedIndexChanged += (s, e) => FiltrarGrid();
            layoutFiltros.Controls.Add(cmbFiltroRangoEdad, 2, 1);

            // Botones
            FlowLayoutPanel panelBotones = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight
            };

            btnRefrescar = new Button { Text = "🔄 Refrescar", Width = 120, Height = 30 };
            btnRefrescar.Click += (s, e) => CargarDatos();

            btnExportar = new Button { Text = "📊 Exportar Excel", Width = 130, Height = 30 };
            btnExportar.Click += BtnExportar_Click;

            panelBotones.Controls.AddRange(new Control[] { btnRefrescar, btnExportar });
            layoutFiltros.Controls.Add(panelBotones, 3, 1);

            // Fila 2: Total de pacientes
            lblTotalPacientes = new Label 
            { 
                Text = "Total de pacientes: 0", 
                AutoSize = true, 
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            layoutFiltros.Controls.Add(lblTotalPacientes, 0, 2);

            grpFiltros.Controls.Add(layoutFiltros);
            panelFiltros.Controls.Add(grpFiltros);
            splitMain.Panel1.Controls.Add(panelFiltros);

            // Panel Inferior: Grid
            gridProximas = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle 
                { 
                    BackColor = Color.LightGray 
                }
            };

            splitMain.Panel2.Controls.Add(gridProximas);
            this.Controls.Add(splitMain);
        }

        private void CargarCombosFiltro()
        {
            try
            {
                // Cargar vacunas disponibles
                cmbFiltroVacuna.Items.Clear();
                cmbFiltroVacuna.Items.Add("Todas las vacunas");

                var dtVacunas = DatabaseHelper.ExecuteQuery(
                    "SELECT DISTINCT nombre_biologico FROM Vacunas ORDER BY nombre_biologico");

                foreach (DataRow row in dtVacunas.Rows)
                {
                    cmbFiltroVacuna.Items.Add(row["nombre_biologico"].ToString());
                }

                cmbFiltroVacuna.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando filtros: " + ex.Message, "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarDatos()
        {
            try
            {
                // Consulta que calcula:
                // 1. Edad actual del paciente en meses
                // 2. Última vacuna aplicada (con subconsulta compatible con SQLite)
                // 3. Fecha de última vacuna
                // 4. Próxima vacuna sugerida según edad

                string query = @"
                    SELECT 
                        p.id_paciente,
                        p.historia_clinica AS HC,
                        p.nombres || ' ' || p.apellidos AS nombre_completo,
                        p.cedula,
                        p.fecha_nacimiento,
                        p.sexo,
                        -- Calcular edad en meses
                        CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) AS edad_meses,
                        -- Calcular años y meses
                        CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 365.25 AS INTEGER) AS edad_anos,
                        CAST(((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44) % 12 AS INTEGER) AS edad_meses_resto,
                        -- Edad en formato texto
                        CASE 
                            WHEN CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 365.25 AS INTEGER) = 0 
                            THEN CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) || ' meses'
                            WHEN CAST(((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44) % 12 AS INTEGER) = 0 
                            THEN CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 365.25 AS INTEGER) || ' años'
                            ELSE CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 365.25 AS INTEGER) || ' años ' || 
                                 CAST(((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44) % 12 AS INTEGER) || ' meses'
                        END AS edad_texto,
                        r.nombres AS representante_nombre,
                        r.telefono AS representante_telefono,
                        -- Última vacuna (subconsulta)
                        COALESCE(
                            (SELECT v.nombre_biologico 
                             FROM Registro_Vacunacion rv 
                             INNER JOIN Vacunas v ON rv.id_vacuna = v.id_vacuna 
                             WHERE rv.id_paciente = p.id_paciente 
                             ORDER BY rv.fecha_aplicacion DESC 
                             LIMIT 1),
                            'Sin vacunas registradas'
                        ) AS ultima_vacuna,
                        -- Última dosis
                        COALESCE(
                            (SELECT rv.numero_dosis 
                             FROM Registro_Vacunacion rv 
                             WHERE rv.id_paciente = p.id_paciente 
                             ORDER BY rv.fecha_aplicacion DESC 
                             LIMIT 1),
                            '-'
                        ) AS ultima_dosis,
                        -- Fecha última vacuna
                        COALESCE(
                            (SELECT rv.fecha_aplicacion 
                             FROM Registro_Vacunacion rv 
                             WHERE rv.id_paciente = p.id_paciente 
                             ORDER BY rv.fecha_aplicacion DESC 
                             LIMIT 1),
                            '-'
                        ) AS fecha_ultima_vacuna,
                        -- Días desde última vacuna
                        CASE 
                            WHEN (SELECT rv.fecha_aplicacion 
                                  FROM Registro_Vacunacion rv 
                                  WHERE rv.id_paciente = p.id_paciente 
                                  ORDER BY rv.fecha_aplicacion DESC 
                                  LIMIT 1) IS NOT NULL 
                            THEN CAST((JULIANDAY('now') - JULIANDAY(
                                (SELECT rv.fecha_aplicacion 
                                 FROM Registro_Vacunacion rv 
                                 WHERE rv.id_paciente = p.id_paciente 
                                 ORDER BY rv.fecha_aplicacion DESC 
                                 LIMIT 1))) AS INTEGER)
                            ELSE NULL
                        END AS dias_desde_ultima_vacuna,
                        -- Próxima vacuna sugerida según edad
                        CASE 
                            WHEN CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) = 0 
                            THEN 'BCG, Hepatitis B (Recién nacido)'
                            WHEN CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) >= 2 
                                 AND CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) < 4 
                            THEN 'Pentavalente 1ra, Rotavirus 1ra, Neumococo 1ra, Polio 1ra'
                            WHEN CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) >= 4 
                                 AND CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) < 6 
                            THEN 'Pentavalente 2da, Rotavirus 2da, Neumococo 2da, Polio 2da'
                            WHEN CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) >= 6 
                                 AND CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) < 7 
                            THEN 'Pentavalente 3ra, Polio 3ra, Influenza 1ra'
                            WHEN CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) >= 7 
                                 AND CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) < 12 
                            THEN 'Influenza 2da'
                            WHEN CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) >= 12 
                                 AND CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) < 15 
                            THEN 'SRP 1ra, Neumococo Refuerzo, Varicela'
                            WHEN CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) >= 15 
                                 AND CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) < 18 
                            THEN 'Fiebre Amarilla'
                            WHEN CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) >= 18 
                                 AND CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) < 24 
                            THEN 'DPT Refuerzo, Polio Refuerzo'
                            WHEN CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) >= 24 
                                 AND CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) < 60 
                            THEN 'SRP 2da (4-5 años)'
                            WHEN CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) >= 60 
                                 AND CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) < 120 
                            THEN 'DPT 2do Refuerzo (6 años)'
                            WHEN CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) >= 120 
                            THEN 'Esquema completo - Revisar refuerzos'
                            ELSE 'Consultar calendario de vacunación'
                        END AS proxima_vacuna_sugerida,
                        -- Estado de vacunación
                        CASE
                            WHEN (SELECT rv.fecha_aplicacion 
                                  FROM Registro_Vacunacion rv 
                                  WHERE rv.id_paciente = p.id_paciente 
                                  ORDER BY rv.fecha_aplicacion DESC 
                                  LIMIT 1) IS NULL 
                            THEN '🔴 Sin vacunas'
                            WHEN CAST((JULIANDAY('now') - JULIANDAY(
                                (SELECT rv.fecha_aplicacion 
                                 FROM Registro_Vacunacion rv 
                                 WHERE rv.id_paciente = p.id_paciente 
                                 ORDER BY rv.fecha_aplicacion DESC 
                                 LIMIT 1))) AS INTEGER) > 90 
                            THEN '🟡 Atrasado (>90 días)'
                            WHEN CAST((JULIANDAY('now') - JULIANDAY(
                                (SELECT rv.fecha_aplicacion 
                                 FROM Registro_Vacunacion rv 
                                 WHERE rv.id_paciente = p.id_paciente 
                                 ORDER BY rv.fecha_aplicacion DESC 
                                 LIMIT 1))) AS INTEGER) > 60 
                            THEN '🟠 Próximo (>60 días)'
                            ELSE '🟢 Al día'
                        END AS estado_vacunacion
                    FROM Pacientes p
                    LEFT JOIN Representantes r ON p.id_representante = r.id_representante
                    ORDER BY CAST((JULIANDAY('now') - JULIANDAY(p.fecha_nacimiento)) / 30.44 AS INTEGER) ASC, 
                             p.nombres || ' ' || p.apellidos ASC";

                var dt = DatabaseHelper.ExecuteQuery(query);
                gridProximas.DataSource = dt;

                // Configurar columnas
                ConfigurarColumnasGrid();

                // Actualizar contador
                lblTotalPacientes.Text = $"Total de pacientes: {dt.Rows.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando datos:\n{ex.Message}\n\nDetalle: {ex.StackTrace}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigurarColumnasGrid()
        {
            try
            {
                if (gridProximas.Columns.Count == 0) return;

                // Suspender layout para mejorar rendimiento
                gridProximas.SuspendLayout();

                // Ocultar columnas no necesarias
                if (gridProximas.Columns.Contains("id_paciente"))
                    gridProximas.Columns["id_paciente"].Visible = false;
                
                if (gridProximas.Columns.Contains("fecha_nacimiento"))
                    gridProximas.Columns["fecha_nacimiento"].Visible = false;

                if (gridProximas.Columns.Contains("edad_meses"))
                    gridProximas.Columns["edad_meses"].Visible = false;

                if (gridProximas.Columns.Contains("cedula"))
                    gridProximas.Columns["cedula"].Visible = false;

                // Configurar headers
                var headers = new Dictionary<string, string>
                {
                    { "HC", "Historia Clínica" },
                    { "nombre_completo", "Paciente" },
                    { "sexo", "Sexo" },
                    { "edad_texto", "Edad" },
                    { "representante_nombre", "Representante" },
                    { "representante_telefono", "Teléfono" },
                    { "ultima_vacuna", "Última Vacuna" },
                    { "ultima_dosis", "Dosis" },
                    { "fecha_ultima_vacuna", "Fecha Última Vacuna" },
                    { "dias_desde_ultima_vacuna", "Días Transcurridos" },
                    { "proxima_vacuna_sugerida", "Próxima Vacuna Sugerida" },
                    { "estado_vacunacion", "Estado" }
                };

                foreach (var kvp in headers)
                {
                    if (gridProximas.Columns.Contains(kvp.Key))
                    {
                        gridProximas.Columns[kvp.Key].HeaderText = kvp.Value;
                    }
                }

                // Configurar auto-size para columnas
                gridProximas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

                // Ajustar anchos de forma segura
                SetColumnWidth("HC", 100);
                SetColumnWidth("nombre_completo", 200);
                SetColumnWidth("edad_texto", 120);
                SetColumnWidth("sexo", 50);
                SetColumnWidth("representante_nombre", 180);
                SetColumnWidth("representante_telefono", 100);
                SetColumnWidth("ultima_vacuna", 150);
                SetColumnWidth("ultima_dosis", 80);
                SetColumnWidth("fecha_ultima_vacuna", 130);
                SetColumnWidth("dias_desde_ultima_vacuna", 100);
                SetColumnWidth("estado_vacunacion", 130);

                // Configurar columna de próxima vacuna con wrap
                if (gridProximas.Columns.Contains("proxima_vacuna_sugerida"))
                {
                    gridProximas.Columns["proxima_vacuna_sugerida"].Width = 350;
                    gridProximas.Columns["proxima_vacuna_sugerida"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                }

                // Aplicar colores según estado (solo una vez)
                gridProximas.CellFormatting -= Grid_CellFormatting; // Remover si ya existe
                gridProximas.CellFormatting += Grid_CellFormatting;
            }
            finally
            {
                // Reanudar layout
                gridProximas.ResumeLayout();
            }
        }

        private void SetColumnWidth(string columnName, int width)
        {
            try
            {
                if (gridProximas.Columns.Contains(columnName))
                {
                    var column = gridProximas.Columns[columnName];
                    if (column != null && column.Visible)
                    {
                        column.Width = width;
                        column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    }
                }
            }
            catch
            {
                // Ignorar errores al establecer ancho
            }
        }

        private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                // Validar índices
                if (e.ColumnIndex < 0 || e.ColumnIndex >= gridProximas.Columns.Count)
                    return;

                if (e.RowIndex < 0 || e.RowIndex >= gridProximas.Rows.Count)
                    return;

                var columnName = gridProximas.Columns[e.ColumnIndex].Name;

                // Formatear columna de estado de vacunación
                if (columnName == "estado_vacunacion" && e.Value != null)
                {
                    string estado = e.Value.ToString() ?? "";
                    
                    if (estado.Contains("Sin vacunas"))
                    {
                        e.CellStyle.BackColor = Color.LightCoral;
                        e.CellStyle.ForeColor = Color.DarkRed;
                        if (gridProximas.Font != null)
                            e.CellStyle.Font = new Font(gridProximas.Font, FontStyle.Bold);
                    }
                    else if (estado.Contains("Atrasado"))
                    {
                        e.CellStyle.BackColor = Color.LightYellow;
                        e.CellStyle.ForeColor = Color.DarkOrange;
                        if (gridProximas.Font != null)
                            e.CellStyle.Font = new Font(gridProximas.Font, FontStyle.Bold);
                    }
                    else if (estado.Contains("Próximo"))
                    {
                        e.CellStyle.BackColor = Color.LightGoldenrodYellow;
                        e.CellStyle.ForeColor = Color.DarkGoldenrod;
                    }
                    else if (estado.Contains("Al día"))
                    {
                        e.CellStyle.BackColor = Color.LightGreen;
                        e.CellStyle.ForeColor = Color.DarkGreen;
                    }
                }

                // Formatear columna de próximas vacunas
                if (columnName == "proxima_vacuna_sugerida" && e.Value != null)
                {
                    if (gridProximas.Font != null)
                        e.CellStyle.Font = new Font(gridProximas.Font, FontStyle.Regular);
                    e.CellStyle.BackColor = Color.AliceBlue;
                }
            }
            catch
            {
                // Ignorar errores de formateo
            }
        }

        private void FiltrarGrid()
        {
            try
            {
                if (gridProximas.DataSource is not DataTable dt) return;

                string filtro = "";
                var filtros = new List<string>();

                // Filtro por búsqueda de texto
                if (!string.IsNullOrWhiteSpace(txtBuscar.Text))
                {
                    string texto = txtBuscar.Text.Replace("'", "''");
                    filtros.Add($"(nombre_completo LIKE '%{texto}%' OR HC LIKE '%{texto}%' OR " +
                               $"cedula LIKE '%{texto}%' OR representante_nombre LIKE '%{texto}%')");
                }

                // Filtro por vacuna
                if (cmbFiltroVacuna.SelectedIndex > 0)
                {
                    string vacuna = cmbFiltroVacuna.SelectedItem?.ToString()?.Replace("'", "''") ?? "";
                    filtros.Add($"(ultima_vacuna LIKE '%{vacuna}%' OR proxima_vacuna_sugerida LIKE '%{vacuna}%')");
                }

                // Filtro por rango de edad
                if (cmbFiltroRangoEdad.SelectedIndex > 0)
                {
                    string rangoSeleccionado = cmbFiltroRangoEdad.SelectedItem?.ToString() ?? "";
                    
                    if (rangoSeleccionado.Contains("0-2 meses"))
                        filtros.Add("edad_meses >= 0 AND edad_meses < 2");
                    else if (rangoSeleccionado.Contains("2-4 meses"))
                        filtros.Add("edad_meses >= 2 AND edad_meses < 4");
                    else if (rangoSeleccionado.Contains("4-6 meses"))
                        filtros.Add("edad_meses >= 4 AND edad_meses < 6");
                    else if (rangoSeleccionado.Contains("6-12 meses"))
                        filtros.Add("edad_meses >= 6 AND edad_meses < 12");
                    else if (rangoSeleccionado.Contains("12-18 meses"))
                        filtros.Add("edad_meses >= 12 AND edad_meses < 18");
                    else if (rangoSeleccionado.Contains("18-24 meses"))
                        filtros.Add("edad_meses >= 18 AND edad_meses < 24");
                    else if (rangoSeleccionado.Contains("2-5 años"))
                        filtros.Add("edad_meses >= 24 AND edad_meses < 60");
                    else if (rangoSeleccionado.Contains("5-10 años"))
                        filtros.Add("edad_meses >= 60 AND edad_meses < 120");
                    else if (rangoSeleccionado.Contains("10+ años"))
                        filtros.Add("edad_meses >= 120");
                }

                // Combinar filtros
                if (filtros.Count > 0)
                    filtro = string.Join(" AND ", filtros);

                dt.DefaultView.RowFilter = filtro;
                lblTotalPacientes.Text = $"Total de pacientes: {dt.DefaultView.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error aplicando filtros: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnExportar_Click(object? sender, EventArgs e)
        {
            try
            {
                if (gridProximas.DataSource is not DataTable dt || dt.Rows.Count == 0)
                {
                    MessageBox.Show("No hay datos para exportar.", "Información", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Archivo CSV|*.csv|Archivo de Texto|*.txt",
                    Title = "Exportar Próximas Vacunaciones",
                    FileName = $"Proximas_Vacunas_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    ExportarACSV(dt, saveDialog.FileName);
                    MessageBox.Show($"Datos exportados correctamente a:\n{saveDialog.FileName}", 
                        "Exportación Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportarACSV(DataTable dt, string rutaArchivo)
        {
            using (var writer = new System.IO.StreamWriter(rutaArchivo, false, System.Text.Encoding.UTF8))
            {
                // Escribir encabezados
                var headers = new List<string>();
                foreach (DataColumn column in dt.Columns)
                {
                    if (column.ColumnName != "id_paciente" && 
                        column.ColumnName != "edad_meses" && 
                        column.ColumnName != "fecha_nacimiento" &&
                        column.ColumnName != "cedula")
                    {
                        headers.Add($"\"{column.ColumnName}\"");
                    }
                }
                writer.WriteLine(string.Join(",", headers));

                // Escribir datos
                foreach (DataRow row in dt.Rows)
                {
                    var valores = new List<string>();
                    foreach (DataColumn column in dt.Columns)
                    {
                        if (column.ColumnName != "id_paciente" && 
                            column.ColumnName != "edad_meses" && 
                            column.ColumnName != "fecha_nacimiento" &&
                            column.ColumnName != "cedula")
                        {
                            string valor = row[column].ToString()?.Replace("\"", "\"\"") ?? "";
                            valores.Add($"\"{valor}\"");
                        }
                    }
                    writer.WriteLine(string.Join(",", valores));
                }
            }
        }
    }
}
