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
        private DataGridView gridHistorial;

        public RegistroVacunasForm()
        {
            InitializeComponent();
            LoadCatalogs();
        }

        private void InitializeComponent()
        {
            this.Text = "Registrar Vacuna";
            this.Size = new Size(1100, 650);
            this.StartPosition = FormStartPosition.CenterParent;

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.Orientation = Orientation.Vertical;
            split.SplitterDistance = 450;
            split.BorderStyle = BorderStyle.FixedSingle;

            // --- Panel Izquierdo: Formulario ---
            var panelInput = new TableLayoutPanel();
            panelInput.Dock = DockStyle.Fill;
            panelInput.Padding = new Padding(20);
            panelInput.RowCount = 8;
            panelInput.ColumnCount = 2;
            
            // Paciente
            panelInput.Controls.Add(new Label() { Text = "Paciente:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            cmbPaciente = new ComboBox() { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            // Evento para cargar historial
            cmbPaciente.SelectionChangeCommitted += (s, e) => CargarHistorial();
            panelInput.Controls.Add(cmbPaciente, 1, 0);

            // Vacuna
            panelInput.Controls.Add(new Label() { Text = "Vacuna:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            cmbVacuna = new ComboBox() { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            panelInput.Controls.Add(cmbVacuna, 1, 1);
            
            // Personal
            panelInput.Controls.Add(new Label() { Text = "Personal Salud:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            cmbPersonal = new ComboBox() { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            panelInput.Controls.Add(cmbPersonal, 1, 2);

            // Dosis
            panelInput.Controls.Add(new Label() { Text = "Dosis:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
            cmbDosis = new ComboBox() { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbDosis.Items.AddRange(new object[] { "1ra Dosis", "2da Dosis", "3ra Dosis", "Refuerzo 1", "Refuerzo 2", "Unica" });
            cmbDosis.SelectedIndex = 0;
            panelInput.Controls.Add(cmbDosis, 1, 3);

            // Fecha
            panelInput.Controls.Add(new Label() { Text = "Fecha Aplicación:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 4);
            dtpFecha = new DateTimePicker();
            panelInput.Controls.Add(dtpFecha, 1, 4);

            // Lote
            panelInput.Controls.Add(new Label() { Text = "Lote Biológico:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 5);
            txtLote = new TextBox() { Width = 200 };
            panelInput.Controls.Add(txtLote, 1, 5);

            // Observaciones
            panelInput.Controls.Add(new Label() { Text = "Observaciones:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 6);
            txtObservaciones = new RichTextBox() { Height = 80, Width = 300 };
            panelInput.Controls.Add(txtObservaciones, 1, 6);

            // Button
            btnRegistrar = new Button() { Text = "Registrar Vacunación", Height = 40, Width = 150, BackColor = Color.LightGreen };
            btnRegistrar.Click += BtnRegistrar_Click;
            
            var panelBtn = new FlowLayoutPanel() { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
            panelBtn.Controls.Add(btnRegistrar);
            panelInput.Controls.Add(panelBtn, 1, 7);

            split.Panel1.Controls.Add(panelInput);

            // --- Panel Derecho: Historial ---
            var groupHistory = new GroupBox() { Text = "Historial de Vacunación del Paciente", Dock = DockStyle.Fill, Padding = new Padding(10) };
            gridHistorial = new DataGridView();
            gridHistorial.Dock = DockStyle.Fill;
            gridHistorial.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridHistorial.ReadOnly = true;
            gridHistorial.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridHistorial.AllowUserToAddRows = false;
            gridHistorial.RowHeadersVisible = false;
            gridHistorial.BackgroundColor = SystemColors.ControlLight;
            
            groupHistory.Controls.Add(gridHistorial);
            split.Panel2.Controls.Add(groupHistory);

            this.Controls.Add(split);
        }

        private void LoadCatalogs()
        {
            try {
                // Pacientes
                var dtP = DatabaseHelper.ExecuteQuery("SELECT id_paciente, nombres || ' ' || apellidos || ' (' || historia_clinica || ')' as display FROM Pacientes");
                cmbPaciente.DataSource = dtP;
                cmbPaciente.DisplayMember = "display";
                cmbPaciente.ValueMember = "id_paciente";
                cmbPaciente.SelectedIndex = -1; // Limpiar selección inicial

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
                // Recargar historial del paciente actual
                CargarHistorial();
                
                // Limpiar campos excepto paciente para facilitar carga masiva? O limpiar todo? 
                // Mejor limpiar todo excepto paciente si se quiere seguir cargando.
                // Por ahora cerramos o limpiamos. El requerimiento original cerraba el form. 
                // Si agregamos historial, quiza el usuario quiere ver que se agregó.
                // this.Close(); // Comentado para permitir seguir viendo historial
                
                // Limpiar campos vacunación
                txtLote.Clear();
                txtObservaciones.Clear();
            }
            catch(Exception ex) {
                MessageBox.Show("Error registrando: " + ex.Message);
            }
        }

        private void CargarHistorial()
        {
            if (cmbPaciente.SelectedValue == null) 
            {
                gridHistorial.DataSource = null;
                return;
            }
            
            try 
            {
                // Validación para evitar cast inválido si el valor no es numérico (bug combo vacío)
                if (!long.TryParse(cmbPaciente.SelectedValue.ToString(), out long idPaciente)) return;

                string sql = @"
                    SELECT 
                        r.id_registro,
                        v.nombre_biologico AS 'Vacuna',
                        r.numero_dosis AS 'Dosis',
                        STRFTIME('%Y-%m-%d', r.fecha_aplicacion) AS 'Fecha',
                        ps.nombres_completos AS 'Responsable',
                        r.lote_biologico AS 'Lote'
                    FROM Registro_Vacunacion r
                    JOIN Vacunas v ON r.id_vacuna = v.id_vacuna
                    JOIN Personal_Salud ps ON r.id_personal = ps.id_personal
                    WHERE r.id_paciente = @pid
                    ORDER BY r.fecha_aplicacion DESC";
                
                var parameters = new SQLiteParameter[] {
                    new SQLiteParameter("@pid", idPaciente)
                };

                var dt = DatabaseHelper.ExecuteQuery(sql, parameters);
                gridHistorial.DataSource = dt;
                
                if (gridHistorial.Columns["id_registro"] != null)
                    gridHistorial.Columns["id_registro"].Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando historial: " + ex.Message);
            }
        }
    }
}
