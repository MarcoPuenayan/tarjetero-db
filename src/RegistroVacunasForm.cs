using System;
using System.Drawing;
using System.Windows.Forms;

namespace TarjeteroApp
{
    public class RegistroVacunasForm : Form
    {
        private ComboBox cmbPaciente;
        private ComboBox cmbVacuna;
        private ComboBox cmbDosis;
        private DateTimePicker dtpFecha;
        private TextBox txtLote;
        private RichTextBox txtObservaciones;
        private Button btnRegistrar;

        public RegistroVacunasForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Registrar Vacuna";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;

            var panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(20);
            panel.RowCount = 7;
            panel.ColumnCount = 2;
            
            // Paciente
            panel.Controls.Add(new Label() { Text = "Paciente:", AutoSize = true }, 0, 0);
            cmbPaciente = new ComboBox() { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            // Populate cmbPaciente
            panel.Controls.Add(cmbPaciente, 1, 0);

            // Vacuna
            panel.Controls.Add(new Label() { Text = "Vacuna:", AutoSize = true }, 0, 1);
            cmbVacuna = new ComboBox() { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            // Populate cmbVacuna
            panel.Controls.Add(cmbVacuna, 1, 1);

            // Dosis
            panel.Controls.Add(new Label() { Text = "Dosis:", AutoSize = true }, 0, 2);
            cmbDosis = new ComboBox() { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbDosis.Items.AddRange(new object[] { "1ra Dosis", "2da Dosis", "3ra Dosis", "Refuerzo 1", "Refuerzo 2", "Unica" });
            panel.Controls.Add(cmbDosis, 1, 2);

            // Fecha
            panel.Controls.Add(new Label() { Text = "Fecha Aplicación:", AutoSize = true }, 0, 3);
            dtpFecha = new DateTimePicker();
            panel.Controls.Add(dtpFecha, 1, 3);

            // Lote
            panel.Controls.Add(new Label() { Text = "Lote Biológico:", AutoSize = true }, 0, 4);
            txtLote = new TextBox() { Width = 200 };
            panel.Controls.Add(txtLote, 1, 4);

            // Observaciones
            panel.Controls.Add(new Label() { Text = "Observaciones:", AutoSize = true }, 0, 5);
            txtObservaciones = new RichTextBox() { Height = 80, Width = 300 };
            panel.Controls.Add(txtObservaciones, 1, 5);

            // Button
            btnRegistrar = new Button() { Text = "Registrar Vacunación", Height = 40, Width = 150 };
            btnRegistrar.Click += BtnRegistrar_Click;
            
            // Add button centered in the last row spanning 2 columns
            var panelBtn = new FlowLayoutPanel() { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
            panelBtn.Controls.Add(btnRegistrar);
            panel.Controls.Add(panelBtn, 1, 6);

            this.Controls.Add(panel);
        }

        private void BtnRegistrar_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Vacuna registrada exitosamente");
            this.Close();
        }
    }
}
