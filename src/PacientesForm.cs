using System;
using System.Drawing;
using System.Windows.Forms;
using TarjeteroApp.Data;

namespace TarjeteroApp
{
    public class PacientesForm : Form
    {
        private DataGridView gridPacientes;
        private TextBox txtHC, txtNombres, txtApellidos;
        private DateTimePicker dtpNacimiento;
        private ComboBox cmbSexo;
        private Button btnGuardar, btnCancelar;

        public PacientesForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Gestión de Pacientes";
            this.Size = new Size(800, 600);
            
            // Layout Panels
            TableLayoutPanel panelInput = new TableLayoutPanel();
            panelInput.RowCount = 5;
            panelInput.ColumnCount = 2;
            panelInput.Dock = DockStyle.Top;
            panelInput.Height = 200;
            panelInput.Padding = new Padding(10);
            
            // Controls
            panelInput.Controls.Add(new Label() { Text = "Historia Clínica:" }, 0, 0);
            txtHC = new TextBox() { Width = 200 };
            panelInput.Controls.Add(txtHC, 1, 0);

            panelInput.Controls.Add(new Label() { Text = "Nombres:" }, 0, 1);
            txtNombres = new TextBox() { Width = 300 };
            panelInput.Controls.Add(txtNombres, 1, 1);

            panelInput.Controls.Add(new Label() { Text = "Apellidos:" }, 0, 2);
            txtApellidos = new TextBox() { Width = 300 };
            panelInput.Controls.Add(txtApellidos, 1, 2);

            panelInput.Controls.Add(new Label() { Text = "Fecha Nacimiento:" }, 0, 3);
            dtpNacimiento = new DateTimePicker();
            panelInput.Controls.Add(dtpNacimiento, 1, 3);

            panelInput.Controls.Add(new Label() { Text = "Sexo:" }, 0, 4);
            cmbSexo = new ComboBox();
            cmbSexo.Items.AddRange(new object[] { "M", "F" });
            panelInput.Controls.Add(cmbSexo, 1, 4);

            // Buttons
            FlowLayoutPanel panelBotones = new FlowLayoutPanel();
            panelBotones.Dock = DockStyle.Top;
            panelBotones.Height = 40;
            
            btnGuardar = new Button() { Text = "Guardar", Width = 100 };
            btnGuardar.Click += BtnGuardar_Click;
            
            btnCancelar = new Button() { Text = "Cancelar", Width = 100 };
            btnCancelar.Click += (s,e) => this.Close();

            panelBotones.Controls.Add(btnGuardar);
            panelBotones.Controls.Add(btnCancelar);

            // Grid
            gridPacientes = new DataGridView();
            gridPacientes.Dock = DockStyle.Fill;
            gridPacientes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            this.Controls.Add(gridPacientes);
            this.Controls.Add(panelBotones);
            this.Controls.Add(panelInput);
        }

        private void LoadData()
        {
            try
            {
                // En app real, esto no va directo, va a un Controller/Service
                // var dt = DatabaseHelper.ExecuteQuery("SELECT * FROM Pacientes");
                // gridPacientes.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando datos: " + ex.Message);
            }
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            // Lógica de guardado SQL
            MessageBox.Show($"Guardando paciente {txtNombres.Text}...");
            // DatabaseHelper.ExecuteNonQuery(...)
            LoadData();
        }
    }
}
