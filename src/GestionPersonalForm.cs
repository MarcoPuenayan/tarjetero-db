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
        private Button btnGuardar, btnEliminar, btnLimpiar;

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
            btnLimpiar = new Button() { Text = "Limpiar", Width = 90 };
            btnEliminar = new Button() { Text = "Eliminar", Width = 90, Enabled = false };

            btnGuardar.Click += BtnGuardar_Click;
            btnLimpiar.Click += (s, e) => { txtCedula.Clear(); txtNombres.Clear(); txtCargo.Clear(); };

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
                string query = "INSERT INTO Personal_Salud (cedula, nombres_completos, cargo) VALUES (@cedula, @nombres, @cargo)";
                var parameters = new SQLiteParameter[]
                {
                    new SQLiteParameter("@cedula", txtCedula.Text),
                    new SQLiteParameter("@nombres", txtNombres.Text),
                    new SQLiteParameter("@cargo", txtCargo.Text)
                };

                DatabaseHelper.ExecuteNonQuery(query, parameters);
                MessageBox.Show("Personal guardado exitosamente.");
                txtCedula.Clear(); txtNombres.Clear(); txtCargo.Clear();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }
    }
}
