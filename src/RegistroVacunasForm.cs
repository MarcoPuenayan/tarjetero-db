using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SQLite;
using TarjeteroApp.Data;

namespace TarjeteroApp
{
    public class RegistroVacunasForm : Form
    {
        private ComboBox cmbPaciente;
        private ComboBox cmbVacuna;
        private ComboBox cmbPersonal; 
        private ComboBox cmbDosis;
        private DateTimePicker dtpFecha;
        private TextBox txtLote;
        private RichTextBox txtObservaciones;
        private Button btnRegistrar;

        public RegistroVacunasForm()
        {
            InitializeComponent();
            LoadCatalogs();
        }

        private void InitializeComponent()
        {
            this.Text = "Registrar Vacuna";
            this.Size = new Size(600, 600);
            this.StartPosition = FormStartPosition.CenterParent;

            var panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(20);
            panel.RowCount = 8;
            panel.ColumnCount = 2;
            
            // Paciente
            panel.Controls.Add(new Label() { Text = "Paciente:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            cmbPaciente = new ComboBox() { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            panel.Controls.Add(cmbPaciente, 1, 0);

            // Vacuna
            panel.Controls.Add(new Label() { Text = "Vacuna:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            cmbVacuna = new ComboBox() { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            panel.Controls.Add(cmbVacuna, 1, 1);
            
            // Personal
            panel.Controls.Add(new Label() { Text = "Personal Salud:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            cmbPersonal = new ComboBox() { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            panel.Controls.Add(cmbPersonal, 1, 2);

            // Dosis
            panel.Controls.Add(new Label() { Text = "Dosis:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
            cmbDosis = new ComboBox() { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbDosis.Items.AddRange(new object[] { "1ra Dosis", "2da Dosis", "3ra Dosis", "Refuerzo 1", "Refuerzo 2", "Unica" });
            cmbDosis.SelectedIndex = 0;
            panel.Controls.Add(cmbDosis, 1, 3);

            // Fecha
            panel.Controls.Add(new Label() { Text = "Fecha Aplicación:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 4);
            dtpFecha = new DateTimePicker();
            panel.Controls.Add(dtpFecha, 1, 4);

            // Lote
            panel.Controls.Add(new Label() { Text = "Lote Biológico:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 5);
            txtLote = new TextBox() { Width = 200 };
            panel.Controls.Add(txtLote, 1, 5);

            // Observaciones
            panel.Controls.Add(new Label() { Text = "Observaciones:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 6);
            txtObservaciones = new RichTextBox() { Height = 80, Width = 300 };
            panel.Controls.Add(txtObservaciones, 1, 6);

            // Button
            btnRegistrar = new Button() { Text = "Registrar Vacunación", Height = 40, Width = 150, BackColor = Color.LightGreen };
            btnRegistrar.Click += BtnRegistrar_Click;
            
            var panelBtn = new FlowLayoutPanel() { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
            panelBtn.Controls.Add(btnRegistrar);
            panel.Controls.Add(panelBtn, 1, 7);

            this.Controls.Add(panel);
        }

        private void LoadCatalogs()
        {
            try {
                // Pacientes
                var dtP = DatabaseHelper.ExecuteQuery("SELECT id_paciente, nombres || ' ' || apellidos || ' (' || historia_clinica || ')' as display FROM Pacientes");
                cmbPaciente.DataSource = dtP;
                cmbPaciente.DisplayMember = "display";
                cmbPaciente.ValueMember = "id_paciente";

                // Vacunas
                var dtV = DatabaseHelper.ExecuteQuery("SELECT id_vacuna, nombre_biologico || ' (' || siglas || ')' as display FROM Vacunas");
                cmbVacuna.DataSource = dtV;
                cmbVacuna.DisplayMember = "display";
                cmbVacuna.ValueMember = "id_vacuna";

                // Personal
                var dtPer = DatabaseHelper.ExecuteQuery("SELECT id_personal, nombres_completos || ' - ' || cargo as display FROM Personal_Salud");
                cmbPersonal.DataSource = dtPer;
                cmbPersonal.DisplayMember = "display";
                cmbPersonal.ValueMember = "id_personal";
            }
            catch(Exception ex) {
                MessageBox.Show("Error cargando catálogos: " + ex.Message);
            }
        }

        private void BtnRegistrar_Click(object? sender, EventArgs e)
        {
            try {
                if (cmbPaciente.SelectedValue == null || cmbVacuna.SelectedValue == null || cmbPersonal.SelectedValue == null)
                {
                    MessageBox.Show("Seleccione Paciente, Vacuna y Personal.");
                    return;
                }

                string sql = @"
                    INSERT INTO Registro_Vacunacion 
                    (id_paciente, id_vacuna, id_personal, fecha_aplicacion, numero_dosis, lote_biologico, observaciones) 
                    VALUES (@pid, @vid, @psid, @fecha, @dosis, @lote, @obs)";

                var parameters = new SQLiteParameter[] {
                    new SQLiteParameter("@pid", cmbPaciente.SelectedValue),
                    new SQLiteParameter("@vid", cmbVacuna.SelectedValue),
                    new SQLiteParameter("@psid", cmbPersonal.SelectedValue),
                    new SQLiteParameter("@fecha", dtpFecha.Value.ToString("yyyy-MM-dd HH:mm:ss")),
                    new SQLiteParameter("@dosis", cmbDosis.SelectedItem.ToString()),
                    new SQLiteParameter("@lote", txtLote.Text),
                    new SQLiteParameter("@obs", txtObservaciones.Text)
                };

                DatabaseHelper.ExecuteNonQuery(sql, parameters);
                MessageBox.Show("Vacuna registrada exitosamente.");
                this.Close();
            }
            catch(Exception ex) {
                MessageBox.Show("Error registrando: " + ex.Message);
            }
        }
    }
}
