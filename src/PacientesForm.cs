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
        private TextBox txtHC, txtNombres, txtApellidos;
        private DateTimePicker dtpNacimiento;
        private ComboBox cmbSexo;
        private ComboBox cmbRepresentante;
        private Button btnGuardar, btnEliminar, btnLimpiar, btnModificar;
        private int? idSeleccionado = null;

        public PacientesForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Gestión de Pacientes";
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // Layout Panels
            TableLayoutPanel panelInput = new TableLayoutPanel();
            panelInput.RowCount = 6;
            panelInput.ColumnCount = 2;
            panelInput.Dock = DockStyle.Top;
            panelInput.Height = 250;
            panelInput.Padding = new Padding(10);
            
            // Controls
            // HC
            panelInput.Controls.Add(new Label() { Text = "Historia Clínica:", Anchor = AnchorStyles.Left }, 0, 0);
            txtHC = new TextBox() { Width = 200 };
            panelInput.Controls.Add(txtHC, 1, 0);

            // Nombres
            panelInput.Controls.Add(new Label() { Text = "Nombres:", Anchor = AnchorStyles.Left }, 0, 1);
            txtNombres = new TextBox() { Width = 300 };
            panelInput.Controls.Add(txtNombres, 1, 1);

            // Apellidos
            panelInput.Controls.Add(new Label() { Text = "Apellidos:", Anchor = AnchorStyles.Left }, 0, 2);
            txtApellidos = new TextBox() { Width = 300 };
            panelInput.Controls.Add(txtApellidos, 1, 2);

            // Fecha Nac
            panelInput.Controls.Add(new Label() { Text = "Fecha Nacimiento:", Anchor = AnchorStyles.Left }, 0, 3);
            dtpNacimiento = new DateTimePicker() { Format = DateTimePickerFormat.Short, Width = 150 };
            panelInput.Controls.Add(dtpNacimiento, 1, 3);

            // Sexo
            panelInput.Controls.Add(new Label() { Text = "Sexo:", Anchor = AnchorStyles.Left }, 0, 4);
            cmbSexo = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
            cmbSexo.Items.AddRange(new object[] { "M", "F" });
            cmbSexo.SelectedIndex = 0;
            panelInput.Controls.Add(cmbSexo, 1, 4);

            // Representante
            panelInput.Controls.Add(new Label() { Text = "Representante:", Anchor = AnchorStyles.Left }, 0, 5);
            cmbRepresentante = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
            panelInput.Controls.Add(cmbRepresentante, 1, 5);

            // Buttons
            FlowLayoutPanel panelBotones = new FlowLayoutPanel();
            panelBotones.Dock = DockStyle.Top;
            panelBotones.Height = 50;
            panelBotones.Padding = new Padding(10);
            
            btnGuardar = new Button() { Text = "Guardar", Width = 90 };
            btnModificar = new Button() { Text = "Modificar", Width = 90, Enabled = false };
            btnLimpiar = new Button() { Text = "Limpiar", Width = 90 };
            btnEliminar = new Button() { Text = "Eliminar", Width = 90, Enabled = false };

            btnGuardar.Click += BtnGuardar_Click;
            btnModificar.Click += BtnModificar_Click;
            btnLimpiar.Click += (s, e) => LimpiarCampos();

            panelBotones.Controls.AddRange(new Control[] { btnGuardar, btnModificar, btnLimpiar, btnEliminar });

            // Grid
            gridPacientes = new DataGridView();
            gridPacientes.Dock = DockStyle.Fill;
            gridPacientes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridPacientes.ReadOnly = true;
            gridPacientes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridPacientes.MultiSelect = false;
            gridPacientes.SelectionChanged += Grid_SelectionChanged;

            this.Controls.Add(gridPacientes);
            this.Controls.Add(panelBotones);
            this.Controls.Add(panelInput);
        }

        private void LoadData()
        {
            try
            {
                // Cargar Pacientes en Grid (Join para ver nombre representante)
                string queryGrid = @"
                    SELECT 
                        p.id_paciente, p.historia_clinica, p.nombres, p.apellidos, 
                        p.fecha_nacimiento, p.sexo, p.id_representante,
                        r.nombres AS representante
                    FROM Pacientes p
                    LEFT JOIN Representantes r ON p.id_representante = r.id_representante";
                
                var dtPacientes = DatabaseHelper.ExecuteQuery(queryGrid);
                gridPacientes.DataSource = dtPacientes;
                if (gridPacientes.Columns["id_representante"] != null)
                    gridPacientes.Columns["id_representante"].Visible = false;

                // Cargar Combo Representantes
                string queryRep = "SELECT id_representante, nombres || ' - ' || cedula AS display FROM Representantes";
                var dtRep = DatabaseHelper.ExecuteQuery(queryRep);
                
                cmbRepresentante.DataSource = dtRep;
                cmbRepresentante.DisplayMember = "display";
                cmbRepresentante.ValueMember = "id_representante";
                cmbRepresentante.SelectedIndex = -1; // Default empty
                
                LimpiarCampos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando datos: " + ex.Message);
            }
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            try {
                if (string.IsNullOrWhiteSpace(txtHC.Text) || string.IsNullOrWhiteSpace(txtNombres.Text))
                {
                    MessageBox.Show("Campos obligatorios vacíos (HC, Nombres)");
                    return;
                }

                string sql = @"
                    INSERT INTO Pacientes (historia_clinica, nombres, apellidos, fecha_nacimiento, sexo, id_representante) 
                    VALUES (@hc, @nom, @ape, @fec, @sex, @idrep)";

                var parameters = new SQLiteParameter[] {
                    new SQLiteParameter("@hc", txtHC.Text),
                    new SQLiteParameter("@nom", txtNombres.Text),
                    new SQLiteParameter("@ape", txtApellidos.Text),
                    new SQLiteParameter("@fec", dtpNacimiento.Value.ToString("yyyy-MM-dd")),
                    new SQLiteParameter("@sex", cmbSexo.SelectedItem?.ToString() ?? "M"),
                    new SQLiteParameter("@idrep", cmbRepresentante.SelectedValue ?? DBNull.Value)
                };

                DatabaseHelper.ExecuteNonQuery(sql, parameters);
                MessageBox.Show("Paciente registrado correctamente");
                LimpiarCampos();
                LoadData();
            }
            catch(Exception ex) {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }

        private void LimpiarCampos()
        {
            txtHC.Clear();
            txtNombres.Clear();
            txtApellidos.Clear();
            dtpNacimiento.Value = DateTime.Now;
            cmbSexo.SelectedIndex = 0;
            cmbRepresentante.SelectedIndex = -1;
            idSeleccionado = null;
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
                txtHC.Text = row.Cells["historia_clinica"].Value?.ToString();
                txtNombres.Text = row.Cells["nombres"].Value?.ToString();
                txtApellidos.Text = row.Cells["apellidos"].Value?.ToString();
                
                if (DateTime.TryParse(row.Cells["fecha_nacimiento"].Value?.ToString(), out DateTime fec))
                    dtpNacimiento.Value = fec;

                cmbSexo.SelectedItem = row.Cells["sexo"].Value?.ToString();
                
                // Set combo value
                if (row.Cells["id_representante"].Value != DBNull.Value)
                    cmbRepresentante.SelectedValue = Convert.ToInt64(row.Cells["id_representante"].Value);
                else
                    cmbRepresentante.SelectedIndex = -1;

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

            if (string.IsNullOrWhiteSpace(txtHC.Text) || string.IsNullOrWhiteSpace(txtNombres.Text))
            {
                MessageBox.Show("Campos obligatorios vacíos (HC, Nombres)");
                return;
            }

            try 
            {
                string sql = @"UPDATE Pacientes SET 
                    historia_clinica = @hc, 
                    nombres = @nom, 
                    apellidos = @ape, 
                    fecha_nacimiento = @fec, 
                    sexo = @sex, 
                    id_representante = @idrep 
                    WHERE id_paciente = @id";

                var parameters = new SQLiteParameter[] {
                    new SQLiteParameter("@hc", txtHC.Text),
                    new SQLiteParameter("@nom", txtNombres.Text),
                    new SQLiteParameter("@ape", txtApellidos.Text),
                    new SQLiteParameter("@fec", dtpNacimiento.Value.ToString("yyyy-MM-dd")),
                    new SQLiteParameter("@sex", cmbSexo.SelectedItem?.ToString() ?? "M"),
                    new SQLiteParameter("@idrep", cmbRepresentante.SelectedValue ?? DBNull.Value),
                    new SQLiteParameter("@id", idSeleccionado)
                };

                DatabaseHelper.ExecuteNonQuery(sql, parameters);
                MessageBox.Show("Paciente modificado correctamente.");
                LimpiarCampos();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al modificar paciente: " + ex.Message);
            }
        }
    }
}
