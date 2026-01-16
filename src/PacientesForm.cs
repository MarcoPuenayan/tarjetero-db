using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SQLite;
using TarjeteroApp.Data;

namespace TarjeteroApp
{
    public class PacientesForm : Form
    {
        private DataGridView gridPacientes;
        private TextBox txtHC, txtNombres, txtApellidos, txtCedula, txtNacionalidad;
        private DateTimePicker dtpNacimiento;
        private ComboBox cmbSexo;
        
        // Campos Representante
        private TextBox txtRepCedula, txtRepNombres, txtRepRelacion, txtRepTelefono, txtRepDireccion;

        private Button btnGuardar, btnEliminar, btnLimpiar, btnModificar;
        private int? idSeleccionado = null;
        private long? idRepresentanteActual = null;

        public PacientesForm()
        {
            InitializeComponent();
            LoadData();
            LoadAutoCompleteData();
        }

        private void InitializeComponent()
        {
            this.Text = "Gestión de Pacientes y Representantes";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // Main Container (Split: Top for Input, Bottom for Grid)
            SplitContainer splitMain = new SplitContainer();
            splitMain.Dock = DockStyle.Fill;
            splitMain.Orientation = Orientation.Horizontal;
            splitMain.SplitterDistance = 400; // Espacio para formularios de paciente y representante
            splitMain.BorderStyle = BorderStyle.Fixed3D;
            splitMain.SplitterWidth = 5;

            // Input Container (Left: Patient, Right: Representative)
            TableLayoutPanel panelInput = new TableLayoutPanel();
            panelInput.Dock = DockStyle.Fill;
            panelInput.RowCount = 1;
            panelInput.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panelInput.ColumnCount = 2;
            panelInput.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            panelInput.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            
            // --- GRUPO PACIENTE ---
            GroupBox grpPaciente = new GroupBox() { Text = "Datos del Paciente", Dock = DockStyle.Fill, Padding = new Padding(10) };
            TableLayoutPanel layoutPac = new TableLayoutPanel() { Dock = DockStyle.Fill, RowCount = 7, ColumnCount = 2, AutoScroll = true };
            
            // 0. Cedula
            layoutPac.Controls.Add(new Label() { Text = "Cédula de Identidad:", Anchor = AnchorStyles.Left }, 0, 0);
            txtCedula = new TextBox() 
            { 
                Width = 200,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.CustomSource
            };
            layoutPac.Controls.Add(txtCedula, 1, 0);

            // 1. HC
            layoutPac.Controls.Add(new Label() { Text = "Historia Clínica Única (HCU):", Anchor = AnchorStyles.Left }, 0, 1);
            txtHC = new TextBox() { Width = 200, PlaceholderText = "Dejar vacío para generar auto" };
            layoutPac.Controls.Add(txtHC, 1, 1);

            // 2. Nombres
            layoutPac.Controls.Add(new Label() { Text = "Nombres:", Anchor = AnchorStyles.Left }, 0, 2);
            txtNombres = new TextBox() 
            { 
                Width = 250,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.CustomSource
            };
            layoutPac.Controls.Add(txtNombres, 1, 2);

            // 3. Apellidos
            layoutPac.Controls.Add(new Label() { Text = "Apellidos:", Anchor = AnchorStyles.Left }, 0, 3);
            txtApellidos = new TextBox() { Width = 250 };
            layoutPac.Controls.Add(txtApellidos, 1, 3);

            // 4. Nacionalidad
            layoutPac.Controls.Add(new Label() { Text = "Nacionalidad:", Anchor = AnchorStyles.Left }, 0, 4);
            txtNacionalidad = new TextBox() { Width = 200 };
            layoutPac.Controls.Add(txtNacionalidad, 1, 4);

            // 5. Fecha Nacimiento
            layoutPac.Controls.Add(new Label() { Text = "Fecha Nacimiento:", Anchor = AnchorStyles.Left }, 0, 5);
            dtpNacimiento = new DateTimePicker() { Format = DateTimePickerFormat.Short, Width = 150 };
            layoutPac.Controls.Add(dtpNacimiento, 1, 5);

            // 6. Sexo
            layoutPac.Controls.Add(new Label() { Text = "Sexo:", Anchor = AnchorStyles.Left }, 0, 6);
            cmbSexo = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
            cmbSexo.Items.AddRange(new object[] { "M", "F" });
            cmbSexo.SelectedIndex = 0;
            layoutPac.Controls.Add(cmbSexo, 1, 6);

            grpPaciente.Controls.Add(layoutPac);
            
            // --- GRUPO REPRESENTANTE ---
            GroupBox grpRep = new GroupBox() { Text = "Datos del Representante", Dock = DockStyle.Fill, Padding = new Padding(10) };
            TableLayoutPanel layoutRep = new TableLayoutPanel() { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 2, AutoScroll = true };

            layoutRep.Controls.Add(new Label() { Text = "Cédula:", Anchor = AnchorStyles.Left }, 0, 0);
            txtRepCedula = new TextBox() 
            { 
                Width = 200,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.CustomSource
            };
            // Evento para buscar si existe el representante al salir de la cédula
            txtRepCedula.Leave += TxtRepCedula_Leave;
            layoutRep.Controls.Add(txtRepCedula, 1, 0);

            layoutRep.Controls.Add(new Label() { Text = "Nombres Completos:", Anchor = AnchorStyles.Left }, 0, 1);
            txtRepNombres = new TextBox() 
            { 
                Width = 250,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.CustomSource
            };
            layoutRep.Controls.Add(txtRepNombres, 1, 1);

            layoutRep.Controls.Add(new Label() { Text = "Parentesco/Relación:", Anchor = AnchorStyles.Left }, 0, 2);
            txtRepRelacion = new TextBox() { Width = 200 };
            layoutRep.Controls.Add(txtRepRelacion, 1, 2);

            layoutRep.Controls.Add(new Label() { Text = "Teléfono:", Anchor = AnchorStyles.Left }, 0, 3);
            txtRepTelefono = new TextBox() { Width = 200 };
            layoutRep.Controls.Add(txtRepTelefono, 1, 3);
            
            layoutRep.Controls.Add(new Label() { Text = "Dirección:", Anchor = AnchorStyles.Left }, 0, 4);
            txtRepDireccion = new TextBox() { Width = 250 };
            layoutRep.Controls.Add(txtRepDireccion, 1, 4);

            grpRep.Controls.Add(layoutRep);

            // Add Groups to Input Panel
            panelInput.Controls.Add(grpPaciente, 0, 0);
            panelInput.Controls.Add(grpRep, 1, 0);

            // Buttons Panel
            FlowLayoutPanel panelBotones = new FlowLayoutPanel();
            panelBotones.Dock = DockStyle.Bottom;
            panelBotones.Height = 50;
            panelBotones.Padding = new Padding(10);
            
            btnGuardar = new Button() { Text = "Guardar", Width = 90 };
            btnModificar = new Button() { Text = "Modificar", Width = 90, Enabled = false };
            btnLimpiar = new Button() { Text = "Limpiar", Width = 90 };
            btnEliminar = new Button() { Text = "Eliminar", Width = 90, Enabled = false };

            btnGuardar.Click += BtnGuardar_Click;
            btnModificar.Click += BtnModificar_Click;
            btnEliminar.Click += BtnEliminar_Click;
            btnLimpiar.Click += (s, e) => LimpiarCampos();

            // Search box
            Label lblBuscar = new Label() { Text = "Buscar:", AutoSize = true, Margin = new Padding(20, 8, 5, 0) };
            TextBox txtBuscar = new TextBox() { Width = 200, Margin = new Padding(0, 5, 0, 0) };
            txtBuscar.PlaceholderText = "Nombre, Apellido o HC...";
            txtBuscar.TextChanged += (s, e) => FiltrarGrid(txtBuscar.Text);

            panelBotones.Controls.AddRange(new Control[] { btnGuardar, btnModificar, btnLimpiar, btnEliminar, lblBuscar, txtBuscar });
            
            // Add panels to Split Top
            splitMain.Panel1.Controls.Add(panelInput);
            splitMain.Panel1.Controls.Add(panelBotones);

            // Grid
            gridPacientes = new DataGridView();
            gridPacientes.Dock = DockStyle.Fill;
            gridPacientes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridPacientes.ReadOnly = true;
            gridPacientes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridPacientes.MultiSelect = false;
            gridPacientes.SelectionChanged += Grid_SelectionChanged;

            splitMain.Panel2.Controls.Add(gridPacientes);

            this.Controls.Add(splitMain);
        }

        private void LoadData()
        {
            try
            {
                // Cargar Pacientes con datos de Representante
                string queryGrid = @"
                    SELECT 
                        p.id_paciente, p.cedula, p.historia_clinica, p.nombres, p.apellidos, 
                        p.nacionalidad, p.fecha_nacimiento, p.sexo, p.id_representante,
                        r.cedula AS rep_cedula,
                        r.nombres AS rep_nombres,
                        r.telefono AS rep_telefono,
                        r.direccion AS rep_direccion,
                        r.relacion AS rep_parentesco
                    FROM Pacientes p
                    LEFT JOIN Representantes r ON p.id_representante = r.id_representante";
                
                var dtPacientes = DatabaseHelper.ExecuteQuery(queryGrid);
                gridPacientes.DataSource = dtPacientes;
                
                // Ocultar columnas internas
                if (gridPacientes.Columns["id_representante"] != null) gridPacientes.Columns["id_representante"].Visible = false;
                if (gridPacientes.Columns["id_paciente"] != null) gridPacientes.Columns["id_paciente"].Visible = false;

                LimpiarCampos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando datos: " + ex.Message);
            }
        }

        private void LoadAutoCompleteData()
        {
            try
            {
                // AutoComplete para cédulas de Pacientes
                var cedulasPacientes = new AutoCompleteStringCollection();
                var dtCedulasPac = DatabaseHelper.ExecuteQuery(
                    "SELECT DISTINCT cedula FROM Pacientes WHERE cedula IS NOT NULL AND cedula != '' ORDER BY cedula");
                
                foreach (DataRow row in dtCedulasPac.Rows)
                {
                    cedulasPacientes.Add(row["cedula"].ToString() ?? string.Empty);
                }
                txtCedula.AutoCompleteCustomSource = cedulasPacientes;

                // AutoComplete para nombres completos de Pacientes
                var nombresPacientes = new AutoCompleteStringCollection();
                var dtNombresPac = DatabaseHelper.ExecuteQuery(
                    "SELECT DISTINCT nombres || ' ' || apellidos as fullname FROM Pacientes WHERE nombres IS NOT NULL ORDER BY fullname");
                
                foreach (DataRow row in dtNombresPac.Rows)
                {
                    nombresPacientes.Add(row["fullname"].ToString() ?? string.Empty);
                }
                txtNombres.AutoCompleteCustomSource = nombresPacientes;

                // AutoComplete para cédulas de Representantes
                var cedulasRep = new AutoCompleteStringCollection();
                var dtCedulasRep = DatabaseHelper.ExecuteQuery(
                    "SELECT DISTINCT cedula FROM Representantes WHERE cedula IS NOT NULL AND cedula != '' ORDER BY cedula");
                
                foreach (DataRow row in dtCedulasRep.Rows)
                {
                    cedulasRep.Add(row["cedula"].ToString() ?? string.Empty);
                }
                txtRepCedula.AutoCompleteCustomSource = cedulasRep;

                // AutoComplete para nombres de Representantes
                var nombresRep = new AutoCompleteStringCollection();
                var dtNombresRep = DatabaseHelper.ExecuteQuery(
                    "SELECT DISTINCT nombres FROM Representantes WHERE nombres IS NOT NULL ORDER BY nombres");
                
                foreach (DataRow row in dtNombresRep.Rows)
                {
                    nombresRep.Add(row["nombres"].ToString() ?? string.Empty);
                }
                txtRepNombres.AutoCompleteCustomSource = nombresRep;
            }
            catch (Exception ex)
            {
                // No mostrar error al usuario, el autocomplete es opcional
                System.Diagnostics.Debug.WriteLine("Error cargando autocomplete: " + ex.Message);
            }
        }

        private void TxtRepCedula_Leave(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRepCedula.Text)) return;

            try
            {
                string query = "SELECT * FROM Representantes WHERE cedula = @ced";
                var dt = DatabaseHelper.ExecuteQuery(query, new SQLiteParameter[] { new SQLiteParameter("@ced", txtRepCedula.Text.Trim()) });
                
                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    txtRepNombres.Text = row["nombres"].ToString();
                    txtRepTelefono.Text = row["telefono"].ToString();
                    txtRepDireccion.Text = row["direccion"].ToString();
                    txtRepRelacion.Text = row["relacion"].ToString();
                    idRepresentanteActual = Convert.ToInt64(row["id_representante"]);
                }
                else
                {
                    idRepresentanteActual = null;
                }
            }
            catch { /* Ignore */ }
        }

        private long GestionarRepresentante()
        {
            if (string.IsNullOrWhiteSpace(txtRepCedula.Text))
                throw new Exception("La cédula del representante es obligatoria.");

            // Verificar si existe por Cédula
            string queryCheck = "SELECT id_representante FROM Representantes WHERE cedula = @ced";
            var dt = DatabaseHelper.ExecuteQuery(queryCheck, new SQLiteParameter[] { new SQLiteParameter("@ced", txtRepCedula.Text.Trim()) });

            long idRep;

            if (dt.Rows.Count > 0)
            {
                // Existe -> Actualizar datos
                idRep = Convert.ToInt64(dt.Rows[0]["id_representante"]);
                string update = @"UPDATE Representantes SET 
                                    nombres = @nom, 
                                    telefono = @tel, 
                                    direccion = @dir, 
                                    relacion = @par 
                                  WHERE id_representante = @id";
                
                DatabaseHelper.ExecuteNonQuery(update, new SQLiteParameter[] {
                    new SQLiteParameter("@nom", txtRepNombres.Text),
                    new SQLiteParameter("@tel", txtRepTelefono.Text),
                    new SQLiteParameter("@dir", txtRepDireccion.Text),
                    new SQLiteParameter("@par", txtRepRelacion.Text),
                    new SQLiteParameter("@id", idRep)
                });
            }
            else
            {
                // Nuevo -> Insertar
                string insert = @"INSERT INTO Representantes (cedula, nombres, telefono, direccion, relacion) 
                                  VALUES (@ced, @nom, @tel, @dir, @par)";
                
                DatabaseHelper.ExecuteNonQuery(insert, new SQLiteParameter[] {
                    new SQLiteParameter("@ced", txtRepCedula.Text),
                    new SQLiteParameter("@nom", txtRepNombres.Text),
                    new SQLiteParameter("@tel", txtRepTelefono.Text),
                    new SQLiteParameter("@dir", txtRepDireccion.Text),
                    new SQLiteParameter("@par", txtRepRelacion.Text)
                });

                // Obtener ID generado
                var dtId = DatabaseHelper.ExecuteQuery("SELECT last_insert_rowid()");
                idRep = Convert.ToInt64(dtId.Rows[0][0]);
            }
            return idRep;
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            try {
                if (string.IsNullOrWhiteSpace(txtNombres.Text))
                {
                    MessageBox.Show("El nombre del paciente es obligatorio.");
                    return;
                }

                // Generar HCU automático si está vacío
                if (string.IsNullOrWhiteSpace(txtHC.Text))
                {
                    txtHC.Text = "A-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                }

                // 1. Gestionar Representante
                long idRep = GestionarRepresentante();

                // 2. Guardar Paciente
                string sql = @"
                    INSERT INTO Pacientes (cedula, historia_clinica, nombres, apellidos, nacionalidad, fecha_nacimiento, sexo, id_representante) 
                    VALUES (@ced, @hc, @nom, @ape, @nac, @fec, @sex, @idrep)";

                var parameters = new SQLiteParameter[] {
                    new SQLiteParameter("@ced", txtCedula.Text),
                    new SQLiteParameter("@hc", txtHC.Text),
                    new SQLiteParameter("@nom", txtNombres.Text),
                    new SQLiteParameter("@ape", txtApellidos.Text),
                    new SQLiteParameter("@nac", txtNacionalidad.Text),
                    new SQLiteParameter("@fec", dtpNacimiento.Value.ToString("yyyy-MM-dd")),
                    new SQLiteParameter("@sex", cmbSexo.SelectedItem?.ToString() ?? "M"),
                    new SQLiteParameter("@idrep", idRep)
                };

                DatabaseHelper.ExecuteNonQuery(sql, parameters);
                MessageBox.Show("Paciente y Representante registrados correctamente");
                LimpiarCampos();
                // No necesitamos recargar a full si solo añadimos uno, pero por consistencia:
                LoadData();
                LoadAutoCompleteData();
            }
            catch(Exception ex) {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }

        private void LimpiarCampos()
        {
            txtCedula.Clear();
            txtHC.Clear();
            txtNombres.Clear();
            txtApellidos.Clear();
            txtNacionalidad.Clear();
            dtpNacimiento.Value = DateTime.Now;
            cmbSexo.SelectedIndex = 0;
            
            // Rep fields
            txtRepCedula.Clear();
            txtRepNombres.Clear();
            txtRepTelefono.Clear();
            txtRepDireccion.Clear();
            txtRepRelacion.Clear();
            
            idSeleccionado = null;
            idRepresentanteActual = null;
            
            btnModificar.Enabled = false;
            btnGuardar.Enabled = true;
            btnEliminar.Enabled = false;
            gridPacientes.ClearSelection();
        }

        private void Grid_SelectionChanged(object? sender, EventArgs e)
        {
            if (gridPacientes.SelectedRows.Count > 0)
            {
                var row = gridPacientes.SelectedRows[0];
                if (row.Cells["id_paciente"].Value == DBNull.Value || row.Cells["id_paciente"].Value == null) return;

                idSeleccionado = Convert.ToInt32(row.Cells["id_paciente"].Value);
                txtCedula.Text = row.Cells["cedula"].Value?.ToString();
                txtHC.Text = row.Cells["historia_clinica"].Value?.ToString();
                txtNombres.Text = row.Cells["nombres"].Value?.ToString();
                txtApellidos.Text = row.Cells["apellidos"].Value?.ToString();
                txtNacionalidad.Text = row.Cells["nacionalidad"].Value?.ToString();
                
                if (DateTime.TryParse(row.Cells["fecha_nacimiento"].Value?.ToString(), out DateTime fec))
                    dtpNacimiento.Value = fec;

                cmbSexo.SelectedItem = row.Cells["sexo"].Value?.ToString();
                
                // Cargar datos representante del grid (JOIN)
                if (row.Cells["id_representante"].Value != DBNull.Value)
                {
                    txtRepCedula.Text = row.Cells["rep_cedula"].Value?.ToString();
                    txtRepNombres.Text = row.Cells["rep_nombres"].Value?.ToString();
                    txtRepTelefono.Text = row.Cells["rep_telefono"].Value?.ToString();
                    txtRepDireccion.Text = row.Cells["rep_direccion"].Value?.ToString();
                    txtRepRelacion.Text = row.Cells["rep_parentesco"].Value?.ToString();
                    idRepresentanteActual = Convert.ToInt64(row.Cells["id_representante"].Value);
                }
                else
                {
                     // Limpiar campos rep si no tiene
                    txtRepCedula.Clear();
                    txtRepNombres.Clear();
                    txtRepTelefono.Clear();
                    txtRepDireccion.Clear();
                    txtRepRelacion.Clear();
                    idRepresentanteActual = null;
                }

                btnModificar.Enabled = true;
                btnGuardar.Enabled = false;
                btnEliminar.Enabled = true;
            }
        }

        private void BtnModificar_Click(object? sender, EventArgs e)
        {
            if (idSeleccionado == null)
            {
                MessageBox.Show("Seleccione un registro para modificar.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNombres.Text))
            {
                MessageBox.Show("El nombre del paciente es obligatorio.");
                return;
            }

            // Generar HCU automático si está vacío
            if (string.IsNullOrWhiteSpace(txtHC.Text))
            {
                txtHC.Text = "A-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            }

            try 
            {
                // 1. Gestionar o Actualizar Representante
                long idRep = GestionarRepresentante();

                // 2. Actualizar Paciente
                string sql = @"UPDATE Pacientes SET 
                    cedula = @ced,
                    historia_clinica = @hc, 
                    nombres = @nom, 
                    apellidos = @ape, 
                    nacionalidad = @nac,
                    fecha_nacimiento = @fec, 
                    sexo = @sex, 
                    id_representante = @idrep 
                    WHERE id_paciente = @id";

                var parameters = new SQLiteParameter[] {
                    new SQLiteParameter("@ced", txtCedula.Text),
                    new SQLiteParameter("@hc", txtHC.Text),
                    new SQLiteParameter("@nom", txtNombres.Text),
                    new SQLiteParameter("@ape", txtApellidos.Text),
                    new SQLiteParameter("@nac", txtNacionalidad.Text),
                    new SQLiteParameter("@fec", dtpNacimiento.Value.ToString("yyyy-MM-dd")),
                    new SQLiteParameter("@sex", cmbSexo.SelectedItem?.ToString() ?? "M"),
                    new SQLiteParameter("@idrep", idRep),
                    new SQLiteParameter("@id", idSeleccionado)
                };

                DatabaseHelper.ExecuteNonQuery(sql, parameters);
                MessageBox.Show("Datos modificados correctamente.");
                LimpiarCampos();
                LoadData();
                LoadAutoCompleteData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al modificar: " + ex.Message);
            }
        }

        private void BtnEliminar_Click(object? sender, EventArgs e)
        {
            if (idSeleccionado == null)
            {
                MessageBox.Show("Seleccione un paciente para eliminar.");
                return;
            }

            var confirmResult = MessageBox.Show("¿Está seguro de eliminar este paciente?",
                                                "Confirmar Eliminación",
                                                MessageBoxButtons.YesNo,
                                                MessageBoxIcon.Warning);

            if (confirmResult == DialogResult.Yes)
            {
                try
                {
                    string sql = "DELETE FROM Pacientes WHERE id_paciente = @id";
                    DatabaseHelper.ExecuteNonQuery(sql, new SQLiteParameter[] { new SQLiteParameter("@id", idSeleccionado) });
                    MessageBox.Show("Paciente eliminado correctamente.");
                    LimpiarCampos();
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al eliminar: " + ex.Message);
                }
            }
        }

        private void FiltrarGrid(string texto)
        {
            try
            {
                if (gridPacientes.DataSource is DataTable dt)
                {
                    if (string.IsNullOrWhiteSpace(texto))
                    {
                        dt.DefaultView.RowFilter = "";
                    }
                    else
                    {
                        // Escape single quotes for safety
                        string safeText = texto.Replace("'", "''"); 
                        // Search in Patient Name, HC, Rep Name, Rep Cedula
                        string filter = string.Format(
                            "cedula LIKE '%{0}%' OR " +
                            "nombres LIKE '%{0}%' OR " +
                            "apellidos LIKE '%{0}%' OR " +
                            "historia_clinica LIKE '%{0}%' OR " +
                            "rep_nombres LIKE '%{0}%' OR " +
                            "rep_cedula LIKE '%{0}%'", 
                            safeText);
                            
                        dt.DefaultView.RowFilter = filter;
                    }
                }
            }
            catch (Exception ex)
            {
                // Silent fail or log
                Console.WriteLine("Error filter: " + ex.Message);
            }
        }
    }
}
