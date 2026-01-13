using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SQLite;
using TarjeteroApp.Data;

namespace TarjeteroApp
{
    public class GestionVacunasForm : Form
    {
        private DataGridView grid;
        private TextBox txtNombre, txtSiglas, txtDescripcion;
        private Button btnGuardar, btnEliminar, btnLimpiar;

        public GestionVacunasForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Administración de Vacunas (Biológicos)";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Panel de Entrada de Datos
            TableLayoutPanel panelInput = new TableLayoutPanel();
            panelInput.Dock = DockStyle.Top;
            panelInput.Height = 150;
            panelInput.ColumnCount = 2;
            panelInput.RowCount = 3;
            panelInput.Padding = new Padding(10);
            
            // Nombre
            panelInput.Controls.Add(new Label() { Text = "Nombre Biológico:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            txtNombre = new TextBox() { Width = 300 };
            panelInput.Controls.Add(txtNombre, 1, 0);

            // Siglas
            panelInput.Controls.Add(new Label() { Text = "Siglas:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            txtSiglas = new TextBox() { Width = 100 };
            panelInput.Controls.Add(txtSiglas, 1, 1);

            // Descripción
            panelInput.Controls.Add(new Label() { Text = "Descripción / Enfermedad:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            txtDescripcion = new TextBox() { Width = 400 };
            panelInput.Controls.Add(txtDescripcion, 1, 2);

            // Panel de Botones
            FlowLayoutPanel panelBotones = new FlowLayoutPanel();
            panelBotones.Dock = DockStyle.Top;
            panelBotones.Height = 40;
            panelBotones.Padding = new Padding(10, 0, 10, 0);

            btnGuardar = new Button() { Text = "Guardar", Width = 90 };
            btnLimpiar = new Button() { Text = "Limpiar", Width = 90 };
            btnEliminar = new Button() { Text = "Eliminar", Width = 90, Enabled = false }; // Habilitar al seleccionar

            btnGuardar.Click += BtnGuardar_Click;
            btnLimpiar.Click += (s, e) => LimpiarCampos();

            panelBotones.Controls.AddRange(new Control[] { btnGuardar, btnLimpiar, btnEliminar });

            // Grilla
            grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            
            this.Controls.Add(grid);
            this.Controls.Add(panelBotones);
            this.Controls.Add(panelInput);
        }

        private void LoadData()
        {
            try 
            {
                var dt = DatabaseHelper.ExecuteQuery("SELECT * FROM Vacunas");
                grid.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar datos: " + ex.Message);
            }
        }

        private void LimpiarCampos()
        {
            txtNombre.Clear();
            txtSiglas.Clear();
            txtDescripcion.Clear();
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("El nombre del biológico es obligatorio.");
                return;
            }

            try 
            {
                string query = "INSERT INTO Vacunas (nombre_biologico, siglas, descripcion_enfermedad) VALUES (@nombre, @siglas, @descripcion)";
                var parameters = new SQLiteParameter[]
                {
                    new SQLiteParameter("@nombre", txtNombre.Text),
                    new SQLiteParameter("@siglas", txtSiglas.Text),
                    new SQLiteParameter("@descripcion", txtDescripcion.Text)
                };

                DatabaseHelper.ExecuteNonQuery(query, parameters);
                MessageBox.Show("Vacuna guardada exitosamente.");
                LimpiarCampos();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }
    }
}
