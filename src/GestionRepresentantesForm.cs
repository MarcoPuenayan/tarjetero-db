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
        private Button btnGuardar, btnEliminar, btnLimpiar;

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
            btnLimpiar = new Button() { Text = "Limpiar", Width = 90 };
            btnEliminar = new Button() { Text = "Eliminar", Width = 90, Enabled = false };

            btnGuardar.Click += BtnGuardar_Click;
            btnLimpiar.Click += (s, e) => { txtCedula.Clear(); txtNombres.Clear(); txtTelefono.Clear(); txtDireccion.Clear(); };

            panelBotones.Controls.AddRange(new Control[] { btnGuardar, btnLimpiar, btnEliminar });

            grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

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
                // Limpiar
                txtCedula.Clear(); txtNombres.Clear(); txtTelefono.Clear(); txtDireccion.Clear();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }
    }
}
