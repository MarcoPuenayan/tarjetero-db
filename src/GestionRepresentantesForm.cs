using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SQLite;
using TarjeteroApp.Data;

namespace TarjeteroApp
{
    public class GestionRepresentantesForm : Form
    {
        private DataGridView grid;
        private TextBox txtCedula, txtNombres, txtTelefono, txtDireccion;
        private ComboBox cmbRelacion;
        private Button btnGuardar, btnEliminar, btnLimpiar, btnModificar;
        private int? idSeleccionado = null;

        public GestionRepresentantesForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteQuery("SELECT * FROM Representantes");
                grid.DataSource = dt;
                LimpiarCampos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando representantes: " + ex.Message);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Administración de Representantes / Tutores";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // SplitContainer principal
            SplitContainer splitMain = new SplitContainer();
            splitMain.Dock = DockStyle.Fill;
            splitMain.Orientation = Orientation.Horizontal;
            splitMain.SplitterDistance = 270;
            splitMain.FixedPanel = FixedPanel.Panel1;
            splitMain.BorderStyle = BorderStyle.FixedSingle;

            // Panel superior: Formulario
            Panel panelSuperior = new Panel();
            panelSuperior.Dock = DockStyle.Fill;

            TableLayoutPanel panelInput = new TableLayoutPanel();
            panelInput.Dock = DockStyle.Top;
            panelInput.Height = 220; // Más alto por más campos
            panelInput.ColumnCount = 2;
            panelInput.RowCount = 5;
            panelInput.Padding = new Padding(10);

            // Cédula
            panelInput.Controls.Add(new Label() { Text = "Cédula:", AutoSize = true }, 0, 0);
            txtCedula = new TextBox() { Width = 150 };
            panelInput.Controls.Add(txtCedula, 1, 0);

            // Nombres
            panelInput.Controls.Add(new Label() { Text = "Nombres y Apellidos:", AutoSize = true }, 0, 1);
            txtNombres = new TextBox() { Width = 300 };
            panelInput.Controls.Add(txtNombres, 1, 1);

            // Relación (Madre, Padre...)
            panelInput.Controls.Add(new Label() { Text = "Relación con Paciente:", AutoSize = true }, 0, 2);
            cmbRelacion = new ComboBox() { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRelacion.Items.AddRange(new object[] { "Madre", "Padre", "Abuelo/a", "Tío/a", "Tutor Legal", "Otro" });
            panelInput.Controls.Add(cmbRelacion, 1, 2);

            // Teléfono
            panelInput.Controls.Add(new Label() { Text = "Teléfono:", AutoSize = true }, 0, 3);
            txtTelefono = new TextBox() { Width = 150 };
            panelInput.Controls.Add(txtTelefono, 1, 3);

            // Dirección
            panelInput.Controls.Add(new Label() { Text = "Dirección Domiciliaria:", AutoSize = true }, 0, 4);
            txtDireccion = new TextBox() { Width = 400 };
            panelInput.Controls.Add(txtDireccion, 1, 4);

            FlowLayoutPanel panelBotones = new FlowLayoutPanel();
            panelBotones.Dock = DockStyle.Top;
            panelBotones.Height = 40;
            panelBotones.Padding = new Padding(10, 0, 10, 0);

            btnGuardar = new Button() { Text = "Guardar", Width = 90 };
            btnModificar = new Button() { Text = "Modificar", Width = 90, Enabled = false };
            btnLimpiar = new Button() { Text = "Limpiar", Width = 90 };
            btnEliminar = new Button() { Text = "Eliminar", Width = 90, Enabled = false };

            btnGuardar.Click += BtnGuardar_Click;
            btnModificar.Click += BtnModificar_Click;
            btnLimpiar.Click += (s, e) => LimpiarCampos();

            panelBotones.Controls.AddRange(new Control[] { btnGuardar, btnModificar, btnLimpiar, btnEliminar });

            panelSuperior.Controls.Add(panelInput);
            panelSuperior.Controls.Add(panelBotones);
            splitMain.Panel1.Controls.Add(panelSuperior);

            // Panel inferior: Grilla
            grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.SelectionChanged += Grid_SelectionChanged;
            splitMain.Panel2.Controls.Add(grid);

            this.Controls.Add(splitMain);
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombres.Text))
            {
                MessageBox.Show("El nombre es obligatorio.");
                return;
            }

            try
            {
                string query = "INSERT INTO Representantes (cedula, nombres, relacion, telefono, direccion) VALUES (@cedula, @nombres, @relacion, @telefono, @direccion)";
                var parameters = new SQLiteParameter[]
                {
                    new SQLiteParameter("@cedula", txtCedula.Text),
                    new SQLiteParameter("@nombres", txtNombres.Text),
                    new SQLiteParameter("@relacion", cmbRelacion.SelectedItem?.ToString() ?? ""),
                    new SQLiteParameter("@telefono", txtTelefono.Text),
                    new SQLiteParameter("@direccion", txtDireccion.Text)
                };

                DatabaseHelper.ExecuteNonQuery(query, parameters);
                MessageBox.Show("Representante guardado exitosamente.");
                LimpiarCampos();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }

        private void LimpiarCampos()
        {
            txtCedula.Clear();
            txtNombres.Clear();
            cmbRelacion.SelectedIndex = -1;
            txtTelefono.Clear();
            txtDireccion.Clear();
            idSeleccionado = null;
            btnModificar.Enabled = false;
            btnGuardar.Enabled = true;
            btnEliminar.Enabled = false;
            grid.ClearSelection();
        }

        private void Grid_SelectionChanged(object? sender, EventArgs e)
        {
            if (grid.SelectedRows.Count > 0)
            {
                var row = grid.SelectedRows[0];
                if (row.Cells["id_representante"].Value == DBNull.Value || row.Cells["id_representante"].Value == null) return;

                idSeleccionado = Convert.ToInt32(row.Cells["id_representante"].Value);
                txtCedula.Text = row.Cells["cedula"].Value?.ToString();
                txtNombres.Text = row.Cells["nombres"].Value?.ToString();
                cmbRelacion.SelectedItem = row.Cells["relacion"].Value?.ToString();
                txtTelefono.Text = row.Cells["telefono"].Value?.ToString();
                txtDireccion.Text = row.Cells["direccion"].Value?.ToString();

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
                MessageBox.Show("El nombre es obligatorio.");
                return;
            }

            try 
            {
                string sql = "UPDATE Representantes SET cedula = @c, nombres = @n, relacion = @r, telefono = @t, direccion = @d WHERE id_representante = @id";
                var parameters = new SQLiteParameter[]
                {
                    new SQLiteParameter("@c", txtCedula.Text),
                    new SQLiteParameter("@n", txtNombres.Text),
                    new SQLiteParameter("@r", cmbRelacion.SelectedItem?.ToString() ?? ""),
                    new SQLiteParameter("@t", txtTelefono.Text),
                    new SQLiteParameter("@d", txtDireccion.Text),
                    new SQLiteParameter("@id", idSeleccionado)
                };

                DatabaseHelper.ExecuteNonQuery(sql, parameters);
                MessageBox.Show("Representante modificado correctamente.");
                LimpiarCampos();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al modificar representante: " + ex.Message);
            }
        }
    }
}
