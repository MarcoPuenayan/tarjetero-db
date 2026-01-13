using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SQLite;
using TarjeteroApp.Data;

namespace TarjeteroApp
{
    public class GestionPersonalForm : Form
    {
        private DataGridView grid;
        private TextBox txtCedula, txtNombres, txtCargo;
        private Button btnGuardar, btnEliminar, btnLimpiar, btnModificar;
        private int? idSeleccionado = null;

        public GestionPersonalForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteQuery("SELECT * FROM Personal_Salud");
                grid.DataSource = dt;
                LimpiarCampos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando personal: " + ex.Message);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Administración de Personal de Salud";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            TableLayoutPanel panelInput = new TableLayoutPanel();
            panelInput.Dock = DockStyle.Top;
            panelInput.Height = 150;
            panelInput.ColumnCount = 2;
            panelInput.RowCount = 3;
            panelInput.Padding = new Padding(10);

            // Cédula
            panelInput.Controls.Add(new Label() { Text = "Cédula:", AutoSize = true }, 0, 0);
            txtCedula = new TextBox() { Width = 150 };
            panelInput.Controls.Add(txtCedula, 1, 0);

            // Nombres
            panelInput.Controls.Add(new Label() { Text = "Nombres Completos:", AutoSize = true }, 0, 1);
            txtNombres = new TextBox() { Width = 300 };
            panelInput.Controls.Add(txtNombres, 1, 1);

            // Cargo
            panelInput.Controls.Add(new Label() { Text = "Cargo:", AutoSize = true }, 0, 2);
            txtCargo = new TextBox() { Width = 200 }; // Podría ser ComboBox
            panelInput.Controls.Add(txtCargo, 1, 2);

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

            grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.SelectionChanged += Grid_SelectionChanged;

            this.Controls.Add(grid);
            this.Controls.Add(panelBotones);
            this.Controls.Add(panelInput);
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
                string query = "INSERT INTO Personal_Salud (cedula, nombres_completos, cargo) VALUES (@cedula, @nombres, @cargo)";
                var parameters = new SQLiteParameter[]
                {
                    new SQLiteParameter("@cedula", txtCedula.Text),
                    new SQLiteParameter("@nombres", txtNombres.Text),
                    new SQLiteParameter("@cargo", txtCargo.Text)
                };

                DatabaseHelper.ExecuteNonQuery(query, parameters);
                MessageBox.Show("Personal guardado exitosamente.");
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
            txtCargo.Clear();
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
                if (row.Cells["id_personal"].Value == DBNull.Value || row.Cells["id_personal"].Value == null) return;

                idSeleccionado = Convert.ToInt32(row.Cells["id_personal"].Value);
                txtCedula.Text = row.Cells["cedula"].Value?.ToString();
                txtNombres.Text = row.Cells["nombres_completos"].Value?.ToString();
                txtCargo.Text = row.Cells["cargo"].Value?.ToString();

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
                string sql = "UPDATE Personal_Salud SET cedula = @c, nombres_completos = @n, cargo = @cargo WHERE id_personal = @id";
                var parameters = new SQLiteParameter[]
                {
                    new SQLiteParameter("@c", txtCedula.Text),
                    new SQLiteParameter("@n", txtNombres.Text),
                    new SQLiteParameter("@cargo", txtCargo.Text),
                    new SQLiteParameter("@id", idSeleccionado)
                };

                DatabaseHelper.ExecuteNonQuery(sql, parameters);
                MessageBox.Show("Personal modificado correctamente.");
                LimpiarCampos();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al modificar personal: " + ex.Message);
            }
        }
    }
}
