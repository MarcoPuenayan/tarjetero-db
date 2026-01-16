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
        private TextBox txtNombre, txtSiglas, txtDescripcion, txtEdad, txtDosis;
        private Button btnGuardar, btnEliminar, btnLimpiar, btnModificar;
        private int? idSeleccionado = null;

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

            // Panel de Entrada de Datos
            TableLayoutPanel panelInput = new TableLayoutPanel();
            panelInput.Dock = DockStyle.Top;
            panelInput.Height = 220;
            panelInput.ColumnCount = 2;
            panelInput.RowCount = 5;
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

            // Edad Recomendada
            panelInput.Controls.Add(new Label() { Text = "Edad / Grupo Etario:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
            txtEdad = new TextBox() { Width = 300, PlaceholderText = "Ej: 0 a 11 meses" };
            panelInput.Controls.Add(txtEdad, 1, 3);

            // Dosis Esquema
            panelInput.Controls.Add(new Label() { Text = "Dosis / Esquema:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 4);
            txtDosis = new TextBox() { Width = 300, PlaceholderText = "Ej: 1ra, 2da, Refuerzo..." };
            panelInput.Controls.Add(txtDosis, 1, 4);

            // Panel de Botones
            FlowLayoutPanel panelBotones = new FlowLayoutPanel();
            panelBotones.Dock = DockStyle.Top;
            panelBotones.Height = 40;
            panelBotones.Padding = new Padding(10, 0, 10, 0);

            btnGuardar = new Button() { Text = "Guardar", Width = 90 };
            btnModificar = new Button() { Text = "Modificar", Width = 90, Enabled = false };
            btnLimpiar = new Button() { Text = "Limpiar", Width = 90 };
            btnEliminar = new Button() { Text = "Eliminar", Width = 90, Enabled = false }; // Habilitar al seleccionar

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

        private void LoadData()
        {
            try 
            {
                var dt = DatabaseHelper.ExecuteQuery("SELECT * FROM Vacunas");
                grid.DataSource = dt;
                LimpiarCampos();
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
            txtEdad.Clear();
            txtDosis.Clear();
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
                if (row.Cells["id_vacuna"].Value == DBNull.Value || row.Cells["id_vacuna"].Value == null) return;
                
                idSeleccionado = Convert.ToInt32(row.Cells["id_vacuna"].Value);
                txtNombre.Text = row.Cells["nombre_biologico"].Value?.ToString();
                txtSiglas.Text = row.Cells["siglas"].Value?.ToString();
                txtDescripcion.Text = row.Cells["descripcion_enfermedad"].Value?.ToString();
                txtEdad.Text = row.Cells["edad_recomendada"].Value?.ToString();
                txtDosis.Text = row.Cells["dosis_esquema"].Value?.ToString();

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

            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("El nombre del biológico es obligatorio.");
                return;
            }

            try 
            {
                string sql = "UPDATE Vacunas SET nombre_biologico = @n, siglas = @s, descripcion_enfermedad = @d, edad_recomendada = @e, dosis_esquema = @do WHERE id_vacuna = @id";
                var parameters = new SQLiteParameter[]
                {
                    new SQLiteParameter("@n", txtNombre.Text),
                    new SQLiteParameter("@s", txtSiglas.Text),
                    new SQLiteParameter("@d", txtDescripcion.Text),
                    new SQLiteParameter("@e", txtEdad.Text),
                    new SQLiteParameter("@do", txtDosis.Text),
                    new SQLiteParameter("@id", idSeleccionado)
                };

                DatabaseHelper.ExecuteNonQuery(sql, parameters);
                MessageBox.Show("Vacuna modificada correctamente.");
                LimpiarCampos();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al modificar vacuna: " + ex.Message);
            }
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
                string query = "INSERT INTO Vacunas (nombre_biologico, siglas, descripcion_enfermedad, edad_recomendada, dosis_esquema) VALUES (@nombre, @siglas, @descripcion, @edad, @dosis)";
                var parameters = new SQLiteParameter[]
                {
                    new SQLiteParameter("@nombre", txtNombre.Text),
                    new SQLiteParameter("@siglas", txtSiglas.Text),
                    new SQLiteParameter("@descripcion", txtDescripcion.Text),
                    new SQLiteParameter("@edad", txtEdad.Text),
                    new SQLiteParameter("@dosis", txtDosis.Text)
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
