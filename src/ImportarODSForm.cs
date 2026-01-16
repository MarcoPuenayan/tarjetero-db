using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Data.SQLite;
using TarjeteroApp.Data;
using TarjeteroApp.Utils;

namespace TarjeteroApp
{
    public class ImportarODSForm : Form
    {
        private TextBox txtFile = null!;
        private Button btnBrowse = null!;
        private Button btnImportar = null!;
        private DataGridView gridPreview = null!;
        private Label lblStatus = null!;
        private DataTable? dtSource;
        
        // Configuración de mapeo de columnas (índice basado en 0)
        private ColumnaMappingConfig config = new ColumnaMappingConfig();

        public ImportarODSForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Importar Tarjetero desde ODS";
            this.Size = new Size(1200, 750);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Panel superior
            Panel panelTop = new Panel() { Dock = DockStyle.Top, Height = 60, Padding = new Padding(10) };
            
            Label lblInstr = new Label() { 
                Text = "1. Seleccione el archivo ODS  →  2. Configure las columnas  →  3. Importar", 
                AutoSize = true, 
                Location = new Point(10, 10),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            
            txtFile = new TextBox() { Location = new Point(10, 35), Width = 700, ReadOnly = true };
            btnBrowse = new Button() { Text = "📁 Examinar...", Location = new Point(720, 34), Width = 120 };
            btnImportar = new Button() { 
                Text = "✓ Importar Datos", 
                Location = new Point(850, 34), 
                Width = 150, 
                Enabled = false, 
                BackColor = Color.LightGreen,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            btnBrowse.Click += BtnBrowse_Click!;
            btnImportar.Click += BtnImportar_Click!;

            panelTop.Controls.AddRange(new Control[] { lblInstr, txtFile, btnBrowse, btnImportar });

            // Status bar
            lblStatus = new Label() { 
                Dock = DockStyle.Bottom, 
                Height = 30, 
                TextAlign = ContentAlignment.MiddleLeft, 
                Text = "Listo para comenzar.",
                Padding = new Padding(10, 0, 0, 0)
            };

            // Grid de vista previa
            gridPreview = new DataGridView();
            gridPreview.Dock = DockStyle.Fill;
            gridPreview.ReadOnly = true;
            gridPreview.AllowUserToAddRows = false;
            gridPreview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            gridPreview.ColumnHeadersHeight = 40;
            
            this.Controls.Add(gridPreview);
            this.Controls.Add(panelTop);
            this.Controls.Add(lblStatus);
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "OpenDocument Spreadsheet (*.ods)|*.ods|All Files (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtFile.Text = ofd.FileName;
                CargarArchivo(ofd.FileName);
            }
        }

        private void CargarArchivo(string path)
        {
            try
            {
                lblStatus.Text = "Leyendo archivo ODS...";
                Application.DoEvents();
                
                dtSource = OdsReader.ReadOds(path);
                gridPreview.DataSource = dtSource;
                
                lblStatus.Text = $"✓ Archivo cargado: {dtSource.Rows.Count} filas, {dtSource.Columns.Count} columnas. Configure el mapeo de columnas.";
                
                // Mostrar diálogo de configuración de columnas
                MostrarDialogoConfiguracion();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error leyendo archivo:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Error al leer archivo.";
            }
        }

        private void MostrarDialogoConfiguracion()
        {
            if (dtSource == null || dtSource.Columns.Count == 0)
            {
                MessageBox.Show("No hay datos para configurar.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Form configForm = new Form();
            configForm.Text = "⚙ Configurar Mapeo de Columnas";
            configForm.Size = new Size(900, 650);
            configForm.StartPosition = FormStartPosition.CenterParent;
            configForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            configForm.MaximizeBox = false;

            Label lblInstrucciones = new Label();
            lblInstrucciones.Text = "Seleccione el NÚMERO DE COLUMNA (0 = primera columna) para cada campo.\nDeje en blanco los campos opcionales que no desee importar.";
            lblInstrucciones.Location = new Point(15, 15);
            lblInstrucciones.Size = new Size(850, 40);
            lblInstrucciones.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            // Panel con scroll
            Panel panelCampos = new Panel();
            panelCampos.Location = new Point(15, 60);
            panelCampos.Size = new Size(850, 450);
            panelCampos.BorderStyle = BorderStyle.FixedSingle;
            panelCampos.AutoScroll = true;

            int yPos = 10;
            var controles = new Dictionary<string, NumericUpDown>();

            // Campos REQUERIDOS
            var camposRequeridos = new[] {
                new { Key = "NOMBRES_COMPLETOS", Nombre = "Apellidos y Nombres (en una columna)", Tooltip = "Columna con apellidos y nombres juntos, se separarán automáticamente" }
            };

            Label lblRequeridos = new Label();
            lblRequeridos.Text = "━━━ CAMPOS REQUERIDOS ━━━";
            lblRequeridos.Location = new Point(10, yPos);
            lblRequeridos.Size = new Size(800, 25);
            lblRequeridos.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblRequeridos.ForeColor = Color.DarkRed;
            panelCampos.Controls.Add(lblRequeridos);
            yPos += 30;

            foreach (var campo in camposRequeridos)
            {
                var lbl = CrearLabel(campo.Nombre + " *", yPos);
                var num = CrearNumericUpDown(yPos, dtSource.Columns.Count);
                var preview = CrearComboBoxPreview(yPos + 25, num);
                
                var tooltip = new ToolTip();
                tooltip.SetToolTip(lbl, campo.Tooltip);
                
                controles[campo.Key] = num;
                panelCampos.Controls.AddRange(new Control[] { lbl, num, preview });
                yPos += 65;
            }

            // Campos OPCIONALES - Datos del Paciente
            yPos += 10;
            Label lblOpcionales = new Label();
            lblOpcionales.Text = "━━━ DATOS DEL PACIENTE (Opcionales) ━━━";
            lblOpcionales.Location = new Point(10, yPos);
            lblOpcionales.Size = new Size(800, 25);
            lblOpcionales.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblOpcionales.ForeColor = Color.DarkBlue;
            panelCampos.Controls.Add(lblOpcionales);
            yPos += 30;

            var camposOpcionales = new[] {
                new { Key = "HC", Nombre = "Historia Clínica (HC) - Auto-generada si vacía", Tooltip = "Número de HC. Si no existe, se generará automáticamente: HC-AÑO-SECUENCIA" },
                new { Key = "FECHA_NAC", Nombre = "Fecha de Nacimiento", Tooltip = "Formato: dd/mm/yyyy o yyyy-mm-dd" },
                new { Key = "SEXO", Nombre = "Sexo", Tooltip = "M/F, H/M, Masculino/Femenino, 1/2" },
                new { Key = "CEDULA", Nombre = "Cédula de Identidad", Tooltip = "Número de cédula o documento" },
                new { Key = "NACIONALIDAD", Nombre = "Nacionalidad", Tooltip = "País de origen" },
                new { Key = "REPRESENTANTE", Nombre = "Nombre del Representante/Padre/Madre", Tooltip = "Se separarán automáticamente apellidos y nombres" }
            };

            foreach (var campo in camposOpcionales)
            {
                var lbl = CrearLabel(campo.Nombre, yPos);
                var num = CrearNumericUpDown(yPos, dtSource.Columns.Count);
                var preview = CrearComboBoxPreview(yPos + 25, num);
                num.Value = -1; // Opcional por defecto
                
                var tooltip = new ToolTip();
                tooltip.SetToolTip(lbl, campo.Tooltip);
                
                controles[campo.Key] = num;
                panelCampos.Controls.AddRange(new Control[] { lbl, num, preview });
                yPos += 65;
            }

            // Columnas de VACUNAS
            yPos += 10;
            Label lblVacunas = new Label();
            lblVacunas.Text = "━━━ COLUMNAS DE VACUNAS (Opcional - seleccione rango) ━━━";
            lblVacunas.Location = new Point(10, yPos);
            lblVacunas.Size = new Size(800, 25);
            lblVacunas.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblVacunas.ForeColor = Color.DarkGreen;
            panelCampos.Controls.Add(lblVacunas);
            yPos += 30;

            Label lblVacInfo = new Label();
            lblVacInfo.Text = "Indique el rango de columnas que contienen las vacunas (fechas de aplicación).\nSe intentará identificar automáticamente el nombre de la vacuna por el encabezado.";
            lblVacInfo.Location = new Point(10, yPos);
            lblVacInfo.Size = new Size(800, 35);
            panelCampos.Controls.Add(lblVacInfo);
            yPos += 40;

            var lblDesde = CrearLabel("Columna inicial de vacunas", yPos);
            var numDesde = CrearNumericUpDown(yPos, dtSource.Columns.Count);
            numDesde.Value = -1;
            controles["VAC_DESDE"] = numDesde;
            panelCampos.Controls.AddRange(new Control[] { lblDesde, numDesde });
            yPos += 35;

            var lblHasta = CrearLabel("Columna final de vacunas", yPos);
            var numHasta = CrearNumericUpDown(yPos, dtSource.Columns.Count);
            numHasta.Value = -1;
            controles["VAC_HASTA"] = numHasta;
            panelCampos.Controls.AddRange(new Control[] { lblHasta, numHasta });
            yPos += 35;

            // Fila inicial (para saltar encabezados)
            yPos += 10;
            Label lblFilaInicial = new Label();
            lblFilaInicial.Text = "━━━ CONFIGURACIÓN DE FILAS ━━━";
            lblFilaInicial.Location = new Point(10, yPos);
            lblFilaInicial.Size = new Size(800, 25);
            lblFilaInicial.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            panelCampos.Controls.Add(lblFilaInicial);
            yPos += 30;

            var lblFilaIni = CrearLabel("Fila inicial (0 = primera fila, 1 = segunda, etc.)", yPos);
            var numFilaIni = CrearNumericUpDown(yPos, dtSource.Rows.Count);
            numFilaIni.Value = 1; // Típicamente la fila 0 es encabezado
            controles["FILA_INICIAL"] = numFilaIni;
            panelCampos.Controls.AddRange(new Control[] { lblFilaIni, numFilaIni });

            configForm.Controls.Add(panelCampos);

            // Botones
            Button btnAutoDetectar = new Button();
            btnAutoDetectar.Text = "🔍 Auto-Detectar Columnas";
            btnAutoDetectar.Location = new Point(280, 530);
            btnAutoDetectar.Size = new Size(200, 35);
            btnAutoDetectar.BackColor = Color.LightBlue;
            btnAutoDetectar.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnAutoDetectar.Click += (s, ev) => {
                AutoDetectarColumnas(controles);
                MessageBox.Show("Auto-detección completada.\n\nRevise los valores detectados y ajústelos si es necesario.", 
                    "Auto-Detección", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            Button btnGuardar = new Button();
            btnGuardar.Text = "✓ Guardar Configuración";
            btnGuardar.Location = new Point(500, 530);
            btnGuardar.Size = new Size(180, 35);
            btnGuardar.BackColor = Color.LightGreen;
            btnGuardar.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnGuardar.Click += (s, ev) => {
                // Validar campos requeridos
                if (controles["NOMBRES_COMPLETOS"].Value < 0)
                {
                    MessageBox.Show("Debe especificar al menos:\n• Apellidos y Nombres\n\n(La Historia Clínica se generará automáticamente si no se especifica)", 
                        "Campos Requeridos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Guardar configuración
                config.ColHC = controles["HC"].Value >= 0 ? (int?)controles["HC"].Value : null;
                config.ColNombresCompletos = (int)controles["NOMBRES_COMPLETOS"].Value;
                config.ColFechaNac = controles["FECHA_NAC"].Value >= 0 ? (int?)controles["FECHA_NAC"].Value : null;
                config.ColSexo = controles["SEXO"].Value >= 0 ? (int?)controles["SEXO"].Value : null;
                config.ColCedula = controles["CEDULA"].Value >= 0 ? (int?)controles["CEDULA"].Value : null;
                config.ColNacionalidad = controles["NACIONALIDAD"].Value >= 0 ? (int?)controles["NACIONALIDAD"].Value : null;
                config.ColRepresentante = controles["REPRESENTANTE"].Value >= 0 ? (int?)controles["REPRESENTANTE"].Value : null;
                config.ColVacunaDesde = controles["VAC_DESDE"].Value >= 0 ? (int?)controles["VAC_DESDE"].Value : null;
                config.ColVacunaHasta = controles["VAC_HASTA"].Value >= 0 ? (int?)controles["VAC_HASTA"].Value : null;
                config.FilaInicial = (int)controles["FILA_INICIAL"].Value;

                configForm.DialogResult = DialogResult.OK;
                configForm.Close();
            };

            Button btnCancelar = new Button();
            btnCancelar.Text = "✗ Cancelar";
            btnCancelar.Location = new Point(700, 530);
            btnCancelar.Size = new Size(150, 35);
            btnCancelar.Click += (s, ev) => configForm.Close();

            configForm.Controls.AddRange(new Control[] { lblInstrucciones, btnAutoDetectar, btnGuardar, btnCancelar });

            if (configForm.ShowDialog() == DialogResult.OK)
            {
                btnImportar.Enabled = true;
                lblStatus.Text = $"✓ Configuración guardada. Presione 'Importar Datos' para comenzar la importación.";
                MessageBox.Show("Configuración guardada correctamente.\n\nAhora puede presionar 'Importar Datos' para iniciar la importación.", 
                    "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private Label CrearLabel(string texto, int yPos)
        {
            return new Label() {
                Text = texto,
                Location = new Point(20, yPos + 3),
                Size = new Size(350, 20),
                Font = new Font("Segoe UI", 9)
            };
        }

        private NumericUpDown CrearNumericUpDown(int yPos, int maxCol)
        {
            return new NumericUpDown() {
                Location = new Point(380, yPos),
                Width = 80,
                Minimum = -1,
                Maximum = maxCol - 1,
                Value = -1
            };
        }

        private ComboBox CrearComboBoxPreview(int yPos, NumericUpDown numRelacionado)
        {
            var combo = new ComboBox() {
                Location = new Point(470, yPos),
                Width = 350,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };

            if (dtSource != null)
            {
                for (int i = 0; i < dtSource.Columns.Count; i++)
                {
                    string preview = $"Col {i}: {dtSource.Columns[i].ColumnName}";
                    if (dtSource.Rows.Count > 0)
                    {
                        string valor = dtSource.Rows[0][i]?.ToString() ?? "";
                        if (valor.Length > 30) valor = valor.Substring(0, 30) + "...";
                        preview += $" | Ej: '{valor}'";
                    }
                    combo.Items.Add(preview);
                }

                numRelacionado.ValueChanged += (s, ev) => {
                    if (numRelacionado.Value >= 0 && numRelacionado.Value < combo.Items.Count)
                    {
                        combo.SelectedIndex = (int)numRelacionado.Value;
                        combo.Enabled = true;
                    }
                    else
                    {
                        combo.SelectedIndex = -1;
                        combo.Enabled = false;
                    }
                };
            }

            return combo;
        }

        private void BtnImportar_Click(object sender, EventArgs e)
        {
            if (dtSource == null || dtSource.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos para importar.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"¿Está seguro de iniciar la importación?\n\n" +
                $"Se procesarán {dtSource.Rows.Count - config.FilaInicial} filas aproximadamente.\n" +
                $"Esto incluye Pacientes, Representantes y Vacunas.",
                "Confirmar Importación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result != DialogResult.Yes) return;

            RealizarImportacion();
        }

        private void RealizarImportacion()
        {
            if (dtSource == null) return;

            Cursor = Cursors.WaitCursor;
            lblStatus.Text = "Verificando base de datos...";
            Application.DoEvents();

            // Verificar y crear tablas si no existen
            if (!VerificarYCrearBaseDatos())
            {
                MessageBox.Show("No se pudo inicializar la base de datos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Cursor = Cursors.Default;
                return;
            }

            lblStatus.Text = "Importando datos...";
            Application.DoEvents();

            int pacientesNuevos = 0;
            int pacientesActualizados = 0;
            int representantesNuevos = 0;
            int dosisRegistradas = 0;
            int errores = 0;
            int filasVacias = 0;
            
            // Lista para acumular errores detallados
            List<string> listaErrores = new List<string>();

            try
            {
                // Cargar vacunas disponibles
                var vacunas = CargarVacunasDisponibles();
                System.Diagnostics.Debug.WriteLine($"[INFO] Vacunas cargadas: {vacunas.Count}");
                
                if (vacunas.Count == 0)
                {
                    listaErrores.Add("ADVERTENCIA: No hay vacunas registradas en la base de datos. No se podrán registrar dosis.");
                }

                // Procesar cada fila
                for (int i = config.FilaInicial; i < dtSource.Rows.Count; i++)
                {
                    try
                    {
                        DataRow row = dtSource.Rows[i];

                        // Obtener Historia Clínica (puede ser vacía)
                        string hc = config.ColHC.HasValue ? ObtenerValorCelda(row, config.ColHC.Value) : "";
                        string nombresCompletos = ObtenerValorCelda(row, config.ColNombresCompletos);

                        // Si no hay nombre del paciente, ignorar toda la fila silenciosamente
                        if (string.IsNullOrWhiteSpace(nombresCompletos))
                        {
                            filasVacias++;
                            continue; // Saltar fila sin nombre
                        }

                        // Si no hay HC, generar automáticamente
                        if (string.IsNullOrWhiteSpace(hc))
                        {
                            hc = GenerarHCAutomatica();
                        }

                        // Separar apellidos y nombres
                        var (apellidos, nombres) = SepararApellidosYNombres(nombresCompletos);

                        // Datos opcionales del paciente
                        string fechaNacStr = config.ColFechaNac.HasValue ? ObtenerValorCelda(row, config.ColFechaNac.Value) : "";
                        string sexoRaw = config.ColSexo.HasValue ? ObtenerValorCelda(row, config.ColSexo.Value) : "";
                        string sexo = NormalizarSexo(sexoRaw); // Normalizar H→M, Mujer→F, etc.
                        string cedula = config.ColCedula.HasValue ? ObtenerValorCelda(row, config.ColCedula.Value) : "";
                        string nacionalidad = config.ColNacionalidad.HasValue ? ObtenerValorCelda(row, config.ColNacionalidad.Value) : "";
                        string representanteNombre = config.ColRepresentante.HasValue ? ObtenerValorCelda(row, config.ColRepresentante.Value) : "";

                        // Procesar representante si existe
                        long? idRepresentante = null;
                        if (!string.IsNullOrWhiteSpace(representanteNombre))
                        {
                            var (repApellidos, repNombres) = SepararApellidosYNombres(representanteNombre);
                            var resultRep = InsertarOActualizarRepresentante(repNombres, repApellidos);
                            idRepresentante = resultRep.Id;
                            if (resultRep.EsNuevo) representantesNuevos++;
                        }

                        // Insertar o actualizar paciente
                        var resultPac = InsertarOActualizarPaciente(
                            hc, nombres, apellidos, fechaNacStr, sexo, cedula, nacionalidad, idRepresentante
                        );

                        if (resultPac.EsNuevo) 
                        {
                            pacientesNuevos++;
                            System.Diagnostics.Debug.WriteLine($"[INSERT] Paciente nuevo - HC: {hc}, Nombre: {nombres} {apellidos}, ID: {resultPac.Id}");
                        }
                        else 
                        {
                            pacientesActualizados++;
                            System.Diagnostics.Debug.WriteLine($"[UPDATE] Paciente existente - HC: {hc}, ID: {resultPac.Id}");
                        }

                        // Procesar vacunas si se configuraron
                        if (config.ColVacunaDesde.HasValue && config.ColVacunaHasta.HasValue)
                        {
                            for (int colIdx = config.ColVacunaDesde.Value; colIdx <= config.ColVacunaHasta.Value; colIdx++)
                            {
                                if (colIdx >= dtSource.Columns.Count) break;

                                string valorCelda = ObtenerValorCelda(row, colIdx);
                                if (string.IsNullOrWhiteSpace(valorCelda)) continue;

                                // Intentar parsear como fecha
                                if (DateTime.TryParse(valorCelda, out DateTime fechaVacuna))
                                {
                                    // Identificar vacuna por nombre de columna
                                    string nombreColumna = dtSource.Columns[colIdx].ColumnName;
                                    var vacunaEncontrada = IdentificarVacuna(nombreColumna, vacunas);

                                    if (vacunaEncontrada != null)
                                    {
                                        bool registrada = RegistrarDosisVacuna(
                                            resultPac.Id,
                                            vacunaEncontrada.IdVacuna,
                                            fechaVacuna,
                                            vacunaEncontrada.NumeroDosis
                                        );

                                        if (registrada) dosisRegistradas++;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errores++;
                        string error = $"Fila {i + 1}: {ex.Message}";
                        listaErrores.Add(error);
                    }
                }

                // Mostrar resumen
                string mensaje = "✅ IMPORTACIÓN COMPLETADA\n\n" +
                                $"📊 Resumen:\n" +
                                $"  • Pacientes nuevos: {pacientesNuevos}\n" +
                                $"  • Pacientes actualizados: {pacientesActualizados}\n" +
                                $"  • Representantes nuevos: {representantesNuevos}\n" +
                                $"  • Dosis de vacunas registradas: {dosisRegistradas}\n" +
                                $"  • Filas vacías (ignoradas): {filasVacias}\n";

                if (errores > 0)
                {
                    mensaje += $"\n⚠ Errores encontrados: {errores}";
                }

                MessageBox.Show(mensaje, "Importación Completada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Mostrar errores detallados si existen
                if (listaErrores.Count > 0)
                {
                    string detalleErrores = "ERRORES DETALLADOS:\n\n";
                    
                    // Mostrar primeros 50 errores para no saturar
                    int maxErrores = Math.Min(50, listaErrores.Count);
                    for (int i = 0; i < maxErrores; i++)
                    {
                        detalleErrores += $"{i + 1}. {listaErrores[i]}\n";
                    }
                    
                    if (listaErrores.Count > 50)
                    {
                        detalleErrores += $"\n... y {listaErrores.Count - 50} errores más.";
                    }
                    
                    // Crear formulario con TextBox scrollable para mostrar errores
                    Form formErrores = new Form();
                    formErrores.Text = $"Detalle de Errores ({listaErrores.Count} total)";
                    formErrores.Size = new Size(800, 600);
                    formErrores.StartPosition = FormStartPosition.CenterParent;
                    
                    TextBox txtErrores = new TextBox();
                    txtErrores.Multiline = true;
                    txtErrores.ScrollBars = ScrollBars.Both;
                    txtErrores.Dock = DockStyle.Fill;
                    txtErrores.Font = new Font("Consolas", 9);
                    txtErrores.Text = string.Join("\r\n", listaErrores);
                    txtErrores.ReadOnly = true;
                    txtErrores.WordWrap = false;
                    
                    Button btnCopiar = new Button();
                    btnCopiar.Text = "📋 Copiar al Portapapeles";
                    btnCopiar.Dock = DockStyle.Bottom;
                    btnCopiar.Height = 40;
                    btnCopiar.Click += (s, ev) => {
                        Clipboard.SetText(txtErrores.Text);
                        MessageBox.Show("Errores copiados al portapapeles", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    };
                    
                    Button btnCerrar = new Button();
                    btnCerrar.Text = "Cerrar";
                    btnCerrar.Dock = DockStyle.Bottom;
                    btnCerrar.Height = 40;
                    btnCerrar.Click += (s, ev) => formErrores.Close();
                    
                    formErrores.Controls.Add(txtErrores);
                    formErrores.Controls.Add(btnCopiar);
                    formErrores.Controls.Add(btnCerrar);
                    
                    formErrores.ShowDialog();
                }
                lblStatus.Text = $"✓ Importación completada: {pacientesNuevos + pacientesActualizados} pacientes, {dosisRegistradas} dosis.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante la importación:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Error en la importación.";
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private bool VerificarYCrearBaseDatos()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    // Habilitar foreign keys
                    using (var cmd = new SQLiteCommand("PRAGMA foreign_keys = ON;", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Verificar si existe la tabla Vacunas
                    string checkTable = "SELECT name FROM sqlite_master WHERE type='table' AND name='Vacunas';";
                    using (var cmd = new SQLiteCommand(checkTable, conn))
                    {
                        var result = cmd.ExecuteScalar();
                        if (result == null)
                        {
                            // Las tablas no existen, crearlas
                            lblStatus.Text = "Creando estructura de base de datos...";
                            Application.DoEvents();

                            CrearTablas(conn);
                            
                            // Verificar si hay vacunas, si no, crear catálogo básico
                            using (var cmdCheck = new SQLiteCommand("SELECT COUNT(*) FROM Vacunas", conn))
                            {
                                long countVacunas = (long)cmdCheck.ExecuteScalar();
                                if (countVacunas == 0)
                                {
                                    CrearCatalogoVacunas(conn);
                                }
                            }
                            
                            MessageBox.Show("Base de datos creada correctamente.\nSe han inicializado las tablas necesarias.", "Base de Datos Creada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error verificando base de datos:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void CrearTablas(SQLiteConnection conn)
        {
            var comandos = new string[]
            {
                @"CREATE TABLE Vacunas (
                    id_vacuna INTEGER PRIMARY KEY AUTOINCREMENT,
                    nombre_biologico TEXT NOT NULL,
                    siglas TEXT,
                    descripcion_enfermedad TEXT
                );",

                @"CREATE TABLE Personal_Salud (
                    id_personal INTEGER PRIMARY KEY AUTOINCREMENT,
                    cedula TEXT UNIQUE,
                    nombres_completos TEXT NOT NULL,
                    cargo TEXT
                );",

                @"CREATE TABLE Representantes (
                    id_representante INTEGER PRIMARY KEY AUTOINCREMENT,
                    cedula TEXT UNIQUE,
                    nombres TEXT NOT NULL,
                    relacion TEXT,
                    telefono TEXT,
                    direccion TEXT
                );",

                @"CREATE TABLE Pacientes (
                    id_paciente INTEGER PRIMARY KEY AUTOINCREMENT,
                    historia_clinica TEXT UNIQUE NOT NULL,
                    nombres TEXT NOT NULL,
                    apellidos TEXT,
                    fecha_nacimiento TEXT NOT NULL,
                    sexo TEXT CHECK (sexo IN ('M', 'F')),
                    cedula TEXT,
                    nacionalidad TEXT,
                    id_representante INTEGER,
                    FOREIGN KEY (id_representante) REFERENCES Representantes(id_representante)
                        ON DELETE SET NULL
                );",

                @"CREATE TABLE Registro_Vacunacion (
                    id_registro INTEGER PRIMARY KEY AUTOINCREMENT,
                    id_paciente INTEGER NOT NULL,
                    id_vacuna INTEGER NOT NULL,
                    id_personal INTEGER NOT NULL,
                    fecha_aplicacion TEXT DEFAULT CURRENT_TIMESTAMP,
                    numero_dosis TEXT NOT NULL,
                    lote_biologico TEXT,
                    edad_al_vacunar_meses INTEGER,
                    observaciones TEXT,
                    FOREIGN KEY (id_paciente) REFERENCES Pacientes(id_paciente)
                        ON DELETE CASCADE,
                    FOREIGN KEY (id_vacuna) REFERENCES Vacunas(id_vacuna),
                    FOREIGN KEY (id_personal) REFERENCES Personal_Salud(id_personal)
                );",

                "CREATE INDEX idx_paciente_historia ON Pacientes(historia_clinica);",
                "CREATE INDEX idx_vacunacion_fecha ON Registro_Vacunacion(fecha_aplicacion);",
                "CREATE INDEX idx_vacunacion_paciente ON Registro_Vacunacion(id_paciente);",

                // Insertar personal por defecto
                "INSERT INTO Personal_Salud (cedula, nombres_completos, cargo) VALUES ('0000000000', 'Sistema - Importación ODS', 'Administrador');"
            };

            foreach (var sql in comandos)
            {
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void CrearCatalogoVacunas(SQLiteConnection conn)
        {
            // Catálogo básico de vacunas del esquema de vacunación
            var vacunas = new[] 
            {
                ("BCG", "Bacillus Calmette-Guérin", "Tuberculosis"),
                ("HB", "Hepatitis B", "Hepatitis B"),
                ("OPV", "Oral Polio Vaccine", "Poliomielitis"),
                ("IPV", "Inactivated Polio Vaccine", "Poliomielitis"),
                ("PENTA", "Pentavalente", "Difteria, Tétanos, Tos ferina, Hepatitis B, Haemophilus influenzae tipo b"),
                ("ROTA", "Rotavirus", "Rotavirus"),
                ("NEUMO", "Neumococo", "Neumococo"),
                ("SRP", "Sarampión, Rubéola, Paperas", "Sarampión, Rubéola, Paperas"),
                ("DPT", "Difteria, Pertussis, Tétanos", "Difteria, Tétanos, Tos ferina"),
                ("DT", "Difteria, Tétanos", "Difteria, Tétanos"),
                ("FA", "Fiebre Amarilla", "Fiebre Amarilla"),
                ("VARICELA", "Varicela", "Varicela"),
                ("INFLUENZA", "Influenza", "Influenza"),
                ("HPV", "Virus del Papiloma Humano", "Virus del Papiloma Humano"),
                ("HEPA", "Hepatitis A", "Hepatitis A")
            };

            foreach (var (siglas, nombre, enfermedad) in vacunas)
            {
                string sql = "INSERT INTO Vacunas (nombre_biologico, siglas, descripcion_enfermedad) VALUES (@nom, @sig, @enf);";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@nom", nombre);
                    cmd.Parameters.AddWithValue("@sig", siglas);
                    cmd.Parameters.AddWithValue("@enf", enfermedad);
                    cmd.ExecuteNonQuery();
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[INFO] Se crearon {vacunas.Length} vacunas en el catálogo");
        }

        private string ObtenerValorCelda(DataRow row, int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= row.Table.Columns.Count)
                return "";

            return row[columnIndex]?.ToString()?.Trim() ?? "";
        }

        private string GenerarHCAutomatica()
        {
            // Generar HC con formato: HC-2026-0001, HC-2026-0002, etc.
            string año = DateTime.Now.Year.ToString();
            
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                
                // Buscar el último número de secuencia para este año
                string query = "SELECT MAX(CAST(SUBSTR(historia_clinica, -4) AS INTEGER)) FROM Pacientes WHERE historia_clinica LIKE @patron";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@patron", $"HC-{año}-%");
                    var result = cmd.ExecuteScalar();
                    
                    int siguienteNumero = 1;
                    if (result != null && result != DBNull.Value)
                    {
                        siguienteNumero = Convert.ToInt32(result) + 1;
                    }
                    
                    return $"HC-{año}-{siguienteNumero:D4}";
                }
            }
        }

        private void AutoDetectarColumnas(Dictionary<string, NumericUpDown> controles)
        {
            if (dtSource == null || dtSource.Rows.Count < 2) return;

            try
            {
                // Buscar en las primeras 10 filas
                int maxFilas = Math.Min(10, dtSource.Rows.Count);

                for (int col = 0; col < dtSource.Columns.Count; col++)
                {
                    // Revisar valores en esta columna
                    for (int fila = 0; fila < maxFilas; fila++)
                    {
                        string valor = dtSource.Rows[fila][col]?.ToString()?.Trim().ToUpperInvariant() ?? "";
                        if (string.IsNullOrWhiteSpace(valor)) continue;

                        // Detectar NACIONALIDAD (ECUATORIANA, VENEZOLANA, etc.)
                        if ((valor.Contains("ECUATORIANA") || valor.Contains("VENEZOLANA") || 
                            valor.Contains("COLOMBIANA") || valor.Contains("PERUANA")) && 
                            controles["NACIONALIDAD"].Value < 0)
                        {
                            controles["NACIONALIDAD"].Value = col;
                        }

                        // Detectar SEXO (M, F, H, MASCULINO, FEMENINO, HOMBRE, MUJER)
                        if ((valor == "M" || valor == "F" || valor == "H" || 
                            valor == "MASCULINO" || valor == "FEMENINO" || 
                            valor == "HOMBRE" || valor == "MUJER" || valor == "1" || valor == "2") &&
                            controles["SEXO"].Value < 0)
                        {
                            // Verificar que no sea cédula (números largos)
                            if (valor.Length <= 2)
                            {
                                controles["SEXO"].Value = col;
                            }
                        }

                        // Detectar CÉDULA (10 dígitos)
                        if (valor.Length == 10 && valor.All(char.IsDigit) && 
                            controles["CEDULA"].Value < 0)
                        {
                            controles["CEDULA"].Value = col;
                        }

                        // Detectar FECHA DE NACIMIENTO (dd/mm/yyyy o yyyy-mm-dd)
                        if (DateTime.TryParse(valor, out DateTime fecha) && 
                            controles["FECHA_NAC"].Value < 0)
                        {
                            // Si la fecha es antes del año 2025, probablemente sea fecha de nacimiento
                            if (fecha.Year < 2025)
                            {
                                controles["FECHA_NAC"].Value = col;
                            }
                        }

                        // Detectar HC (números de 4-8 dígitos sin guiones, O formato HC-XXXX-XXXX)
                        if (controles["HC"].Value < 0)
                        {
                            bool esHC = (valor.Length >= 4 && valor.Length <= 8 && valor.All(char.IsDigit)) ||
                                       valor.StartsWith("HC-");
                            if (esHC)
                            {
                                controles["HC"].Value = col;
                            }
                        }

                        // Detectar nombres (palabras con letras, posiblemente con espacios)
                        if (valor.Any(char.IsLetter) && !valor.Contains("ECUATORIANA") && 
                            !valor.Contains("MASCULINO") && !valor.Contains("FEMENINO"))
                        {
                            // Si contiene apellidos en mayúsculas seguidos de nombres
                            if (valor.Contains(" ") && valor.Split(' ').Length >= 2)
                            {
                                // Verificar si no es representante (col - 1 debería ser cédula de representante)
                                bool esRepresentante = false;
                                if (col > 0)
                                {
                                    string valorAnterior = dtSource.Rows[fila][col - 1]?.ToString()?.Trim() ?? "";
                                    if (valorAnterior.Length == 10 && valorAnterior.All(char.IsDigit))
                                    {
                                        // Probablemente es representante si la columna anterior es cédula
                                        controles["REPRESENTANTE"].Value = col;
                                        esRepresentante = true;
                                    }
                                }

                                if (!esRepresentante && controles["NOMBRES_COMPLETOS"].Value < 0)
                                {
                                    controles["NOMBRES_COMPLETOS"].Value = col;
                                }
                            }
                        }
                    }
                }

                // Auto-detectar rango de vacunas (buscar columnas con fechas recientes)
                int primeraVacuna = -1;
                int ultimaVacuna = -1;

                for (int col = 0; col < dtSource.Columns.Count; col++)
                {
                    bool tieneFecharVacuna = false;
                    for (int fila = 0; fila < maxFilas; fila++)
                    {
                        string valor = dtSource.Rows[fila][col]?.ToString()?.Trim() ?? "";
                        if (DateTime.TryParse(valor, out DateTime fecha))
                        {
                            // Fechas de vacunación suelen ser recientes (2020-2026)
                            if (fecha.Year >= 2020 && fecha.Year <= 2026)
                            {
                                tieneFecharVacuna = true;
                                break;
                            }
                        }
                    }

                    if (tieneFecharVacuna)
                    {
                        if (primeraVacuna < 0) primeraVacuna = col;
                        ultimaVacuna = col;
                    }
                }

                if (primeraVacuna >= 0)
                {
                    controles["VAC_DESDE"].Value = primeraVacuna;
                    controles["VAC_HASTA"].Value = ultimaVacuna;
                }

                // Detectar fila inicial (primera fila con datos reales después de encabezados)
                for (int fila = 0; fila < Math.Min(5, dtSource.Rows.Count); fila++)
                {
                    string primerValor = dtSource.Rows[fila][0]?.ToString()?.Trim() ?? "";
                    // Si encuentra una fila con número de HC válido, esa es la fila inicial
                    if (primerValor.Length >= 4 && primerValor.Length <= 8 && primerValor.All(char.IsDigit))
                    {
                        controles["FILA_INICIAL"].Value = fila;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en auto-detección:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private string NormalizarSexo(string valorOriginal)
        {
            if (string.IsNullOrWhiteSpace(valorOriginal))
                return "";

            string valor = valorOriginal.Trim().ToUpperInvariant();

            // Masculino: H, M, Hombre, Masculino, 1
            if (valor == "H" || valor == "M" || 
                valor == "HOMBRE" || valor == "MASCULINO" || 
                valor == "1" || valor.StartsWith("MASC") || valor.StartsWith("HOM"))
            {
                return "M";
            }

            // Femenino: F, Mujer, Femenino, 2
            if (valor == "F" || 
                valor == "MUJER" || valor == "FEMENINO" || 
                valor == "2" || valor.StartsWith("FEM") || valor.StartsWith("MUJ"))
            {
                return "F";
            }

            // Si no coincide con ningún patrón conocido, retornar vacío (NULL en BD)
            // Agregar a log de errores para debug
            System.Diagnostics.Debug.WriteLine($"[ADVERTENCIA] Valor de sexo no reconocido: '{valorOriginal}' - se insertará como NULL");
            return "";
        }

        private (string apellidos, string nombres) SepararApellidosYNombres(string nombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto))
                return ("", "");

            // Dividir por espacios
            var partes = nombreCompleto.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (partes.Length == 0)
                return ("", "");

            if (partes.Length == 1)
                return (partes[0], ""); // Solo una palabra -> apellido

            if (partes.Length == 2)
                return (partes[0], partes[1]); // 2 palabras -> apellido, nombre

            // 3 o más palabras: Detectar donde cambia de MAYÚSCULAS a Título
            int indiceCambio = -1;
            for (int i = 0; i < partes.Length; i++)
            {
                if (partes[i].Length > 0 && partes[i] != partes[i].ToUpper())
                {
                    indiceCambio = i;
                    break;
                }
            }

            if (indiceCambio > 0)
            {
                // Separar por el cambio detectado
                string apellidos = string.Join(" ", partes.Take(indiceCambio));
                string nombres = string.Join(" ", partes.Skip(indiceCambio));
                return (apellidos, nombres);
            }

            // Fallback: primeras 2 palabras = apellidos, resto = nombres
            if (partes.Length >= 3)
            {
                string apellidos = $"{partes[0]} {partes[1]}";
                string nombres = string.Join(" ", partes.Skip(2));
                return (apellidos, nombres);
            }

            return (partes[0], partes.Length > 1 ? partes[1] : "");
        }

        private Dictionary<string, VacunaInfo> CargarVacunasDisponibles()
        {
            var vacunas = new Dictionary<string, VacunaInfo>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var dt = DatabaseHelper.ExecuteQuery("SELECT id_vacuna, nombre_biologico, siglas FROM Vacunas");

                foreach (DataRow row in dt.Rows)
                {
                    long id = Convert.ToInt64(row["id_vacuna"]);
                    string nombre = row["nombre_biologico"]?.ToString() ?? "";
                    string siglas = row["siglas"]?.ToString() ?? "";

                    if (!string.IsNullOrEmpty(nombre))
                    {
                        vacunas[nombre] = new VacunaInfo { IdVacuna = id, NombreBiologico = nombre };
                    }

                    if (!string.IsNullOrEmpty(siglas))
                    {
                        vacunas[siglas] = new VacunaInfo { IdVacuna = id, NombreBiologico = nombre };
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando catálogo de vacunas:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return vacunas;
        }

        private VacunaInfo? IdentificarVacuna(string nombreColumna, Dictionary<string, VacunaInfo> vacunas)
        {
            if (string.IsNullOrWhiteSpace(nombreColumna))
                return null;

            string nombreLimpio = nombreColumna.Trim().ToUpper();

            // Extraer número de dosis si existe (ej: "BCG1" -> "BCG", dosis "1")
            string nombreSinDosis = nombreLimpio;
            string numeroDosis = "1";

            // Patrones: BCG1, BCG 1, BCG(1), BCG-1, BCG_1
            var match = System.Text.RegularExpressions.Regex.Match(nombreLimpio, @"^(.+?)[\s\-_\(]*(\d+)[\)]*$");
            if (match.Success)
            {
                nombreSinDosis = match.Groups[1].Value.Trim();
                numeroDosis = match.Groups[2].Value;
            }

            // Buscar coincidencia exacta
            if (vacunas.ContainsKey(nombreLimpio))
            {
                var vacuna = vacunas[nombreLimpio];
                return new VacunaInfo
                {
                    IdVacuna = vacuna.IdVacuna,
                    NombreBiologico = vacuna.NombreBiologico,
                    NumeroDosis = numeroDosis
                };
            }

            // Buscar sin número de dosis
            if (vacunas.ContainsKey(nombreSinDosis))
            {
                var vacuna = vacunas[nombreSinDosis];
                return new VacunaInfo
                {
                    IdVacuna = vacuna.IdVacuna,
                    NombreBiologico = vacuna.NombreBiologico,
                    NumeroDosis = numeroDosis
                };
            }

            // Buscar coincidencia parcial
            foreach (var kvp in vacunas)
            {
                if (nombreSinDosis.Contains(kvp.Key) || kvp.Key.Contains(nombreSinDosis))
                {
                    return new VacunaInfo
                    {
                        IdVacuna = kvp.Value.IdVacuna,
                        NombreBiologico = kvp.Value.NombreBiologico,
                        NumeroDosis = numeroDosis
                    };
                }
            }

            return null;
        }

        private (long Id, bool EsNuevo) InsertarOActualizarPaciente(
            string hc, string nombres, string apellidos, string fechaNacStr,
            string sexoStr, string cedula, string nacionalidad, long? idRepresentante)
        {
            // Verificar si existe
            var checkQuery = "SELECT id_paciente FROM Pacientes WHERE historia_clinica = @hc and cedula = @cedula";
            var checkParams = new SQLiteParameter[] { new SQLiteParameter("@hc", hc), new SQLiteParameter("@cedula", cedula) };
            var existing = DatabaseHelper.ExecuteQuery(checkQuery, checkParams);

            if (existing.Rows.Count > 0)
            {
                // Ya existe, retornar ID
                long idExistente = Convert.ToInt64(existing.Rows[0]["id_paciente"]);
                return (idExistente, false);
            }

            // Preparar datos
            DateTime fechaNac = DateTime.Now.AddYears(-1); // Default
            if (!string.IsNullOrWhiteSpace(fechaNacStr) && DateTime.TryParse(fechaNacStr, out DateTime parsedDate))
            {
                fechaNac = parsedDate;
            }

            // Sexo ya viene normalizado desde RealizarImportacion() (M, F o vacío)
            // Si viene vacío, insertar NULL en lugar de un valor por defecto

            // Insertar nuevo paciente
            string sql = @"INSERT INTO Pacientes 
                          (historia_clinica, nombres, apellidos, fecha_nacimiento, sexo, cedula, nacionalidad, id_representante)
                          VALUES (@hc, @nom, @ape, @fecha, @sexo, @ced, @nac, @rep);
                          SELECT last_insert_rowid();";

            var parameters = new SQLiteParameter[] {
                new SQLiteParameter("@hc", hc),
                new SQLiteParameter("@nom", nombres),
                new SQLiteParameter("@ape", string.IsNullOrWhiteSpace(apellidos) ? (object)DBNull.Value : apellidos),
                new SQLiteParameter("@fecha", fechaNac.ToString("yyyy-MM-dd")),
                new SQLiteParameter("@sexo", string.IsNullOrWhiteSpace(sexoStr) ? (object)DBNull.Value : sexoStr),
                new SQLiteParameter("@ced", string.IsNullOrWhiteSpace(cedula) ? (object)DBNull.Value : cedula),
                new SQLiteParameter("@nac", string.IsNullOrWhiteSpace(nacionalidad) ? (object)DBNull.Value : nacionalidad),
                new SQLiteParameter("@rep", idRepresentante.HasValue ? (object)idRepresentante.Value : DBNull.Value)
            };

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddRange(parameters);
                    long newId = (long)cmd.ExecuteScalar();
                    return (newId, true);
                }
            }
        }

        private (long Id, bool EsNuevo) InsertarOActualizarRepresentante(string nombres, string apellidos)
        {
            // Concatenar apellidos y nombres en un solo campo (la tabla Representantes solo tiene campo "nombres")
            string nombreCompleto = $"{apellidos} {nombres}".Trim();

            var checkQuery = "SELECT id_representante FROM Representantes WHERE nombres = @nom LIMIT 1";
            var checkParams = new SQLiteParameter[] { new SQLiteParameter("@nom", nombreCompleto) };
            var existing = DatabaseHelper.ExecuteQuery(checkQuery, checkParams);

            if (existing.Rows.Count > 0)
            {
                long idExistente = Convert.ToInt64(existing.Rows[0]["id_representante"]);
                return (idExistente, false);
            }

            // Insertar nuevo - La tabla solo tiene el campo "nombres" (que guardará el nombre completo)
            string sql = "INSERT INTO Representantes (nombres) VALUES (@nom); SELECT last_insert_rowid();";

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@nom", nombreCompleto);
                    long newId = (long)cmd.ExecuteScalar();
                    return (newId, true);
                }
            }
        }

        private bool RegistrarDosisVacuna(long idPaciente, long idVacuna, DateTime fechaAplicacion, string numeroDosis)
        {
            // Verificar duplicados
            string checkSql = @"SELECT COUNT(*) FROM Registro_Vacunacion 
                               WHERE id_paciente = @pac AND id_vacuna = @vac 
                               AND date(fecha_aplicacion) = date(@fecha)";

            var checkParams = new SQLiteParameter[] {
                new SQLiteParameter("@pac", idPaciente),
                new SQLiteParameter("@vac", idVacuna),
                new SQLiteParameter("@fecha", fechaAplicacion.ToString("yyyy-MM-dd"))
            };

            var result = DatabaseHelper.ExecuteQuery(checkSql, checkParams);
            if (result.Rows.Count > 0 && Convert.ToInt32(result.Rows[0][0]) > 0)
            {
                // Ya existe
                return false;
            }

            // Insertar
            string sql = @"INSERT INTO Registro_Vacunacion 
                          (id_paciente, id_vacuna, id_personal, fecha_aplicacion, numero_dosis)
                          VALUES (@pac, @vac, 1, @fecha, @dosis)";

            var insertParams = new SQLiteParameter[] {
                new SQLiteParameter("@pac", idPaciente),
                new SQLiteParameter("@vac", idVacuna),
                new SQLiteParameter("@fecha", fechaAplicacion.ToString("yyyy-MM-dd")),
                new SQLiteParameter("@dosis", numeroDosis)
            };

            try
            {
                DatabaseHelper.ExecuteNonQuery(sql, insertParams);
                return true;
            }
            catch
            {
                // Error será capturado en el try-catch principal
                throw;
            }
        }
    }

    // Clase de configuración de columnas
    internal class ColumnaMappingConfig
    {
        public int? ColHC { get; set; }
        public int ColNombresCompletos { get; set; } = -1;
        public int? ColFechaNac { get; set; }
        public int? ColSexo { get; set; }
        public int? ColCedula { get; set; }
        public int? ColNacionalidad { get; set; }
        public int? ColRepresentante { get; set; }
        public int? ColVacunaDesde { get; set; }
        public int? ColVacunaHasta { get; set; }
        public int FilaInicial { get; set; } = 1;
    }

    // Clase auxiliar para información de vacunas
    internal class VacunaInfo
    {
        public long IdVacuna { get; set; }
        public string NombreBiologico { get; set; } = "";
        public string NumeroDosis { get; set; } = "1";
    }
}
