using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Data.SQLite;
using TarjeteroApp.Data;

namespace TarjeteroApp
{
    public class ReportesForm : Form
    {
        private DateTimePicker dtpInicio;
        private DateTimePicker dtpFin;
        private CheckBox chkTodasFechas;
        private DataGridView gridReporte;
        private Button btnGenerar;
        private Button btnExportar;

        public ReportesForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Reportes de Vacunación";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterParent;

            // Panel de Filtros
            GroupBox grpFiltros = new GroupBox() { Text = "Filtros de Búsqueda", Dock = DockStyle.Top, Height = 80 };
            
            Label lblInicio = new Label() { Text = "Desde:", Location = new Point(20, 30), AutoSize = true };
            dtpInicio = new DateTimePicker() { Format = DateTimePickerFormat.Short, Location = new Point(70, 25), Width = 120 };
            
            Label lblFin = new Label() { Text = "Hasta:", Location = new Point(210, 30), AutoSize = true };
            dtpFin = new DateTimePicker() { Format = DateTimePickerFormat.Short, Location = new Point(260, 25), Width = 120 };

            chkTodasFechas = new CheckBox() { Text = "Todas las fechas", Location = new Point(400, 27), AutoSize = true };
            chkTodasFechas.CheckedChanged += (s, e) => { dtpInicio.Enabled = !chkTodasFechas.Checked; dtpFin.Enabled = !chkTodasFechas.Checked; };

            btnGenerar = new Button() { Text = "Generar Reporte", Location = new Point(550, 22), Width = 150, Height = 30, BackColor = Color.CornflowerBlue, ForeColor = Color.White };
            btnGenerar.Click += BtnGenerar_Click;

            btnExportar = new Button() { Text = "Exportar a CSV", Location = new Point(720, 22), Width = 150, Height = 30, Enabled = false };
            btnExportar.Click += BtnExportar_Click;

            grpFiltros.Controls.AddRange(new Control[] { lblInicio, dtpInicio, lblFin, dtpFin, chkTodasFechas, btnGenerar, btnExportar });

            // Grid
            gridReporte = new DataGridView();
            gridReporte.Dock = DockStyle.Fill;
            gridReporte.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            gridReporte.ReadOnly = true;
            gridReporte.AllowUserToAddRows = false;

            this.Controls.Add(gridReporte);
            this.Controls.Add(grpFiltros);
        }

        private void BtnGenerar_Click(object? sender, EventArgs e)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append(@"
                    SELECT 
                        rv.id_registro AS 'ID',
                        p.historia_clinica AS 'Historia Clinica',
                        p.apellidos || ' ' || p.nombres AS 'Paciente',
                        p.fecha_nacimiento AS 'F. Nacimiento',
                        CAST((julianday(rv.fecha_aplicacion) - julianday(p.fecha_nacimiento)) / 30.44 AS INT) AS 'Edad (Meses)',
                        p.sexo AS 'Sexo',
                        v.nombre_biologico AS 'Vacuna',
                        rv.numero_dosis AS 'Dosis',
                        rv.fecha_aplicacion AS 'Fecha Vacunacion',
                        rv.lote_biologico AS 'Lote',
                        ps.nombres_completos AS 'Vacunador',
                        r.nombres AS 'Representante',
                        r.direccion AS 'Direccion/Comunidad'
                    FROM Registro_Vacunacion rv
                    JOIN Pacientes p ON rv.id_paciente = p.id_paciente
                    JOIN Vacunas v ON rv.id_vacuna = v.id_vacuna
                    JOIN Personal_Salud ps ON rv.id_personal = ps.id_personal
                    LEFT JOIN Representantes r ON p.id_representante = r.id_representante
                ");

                SQLiteParameter[] parameters = null;

                if (!chkTodasFechas.Checked)
                {
                    sb.Append(" WHERE date(rv.fecha_aplicacion) BETWEEN date(@inicio) AND date(@fin)");
                    parameters = new SQLiteParameter[] {
                        new SQLiteParameter("@inicio", dtpInicio.Value.ToString("yyyy-MM-dd")),
                        new SQLiteParameter("@fin", dtpFin.Value.ToString("yyyy-MM-dd"))
                    };
                }

                sb.Append(" ORDER BY rv.fecha_aplicacion DESC");

                var dt = DatabaseHelper.ExecuteQuery(sb.ToString(), parameters);
                gridReporte.DataSource = dt;
                
                if (dt.Rows.Count > 0)
                {
                    btnExportar.Enabled = true;
                    MessageBox.Show($"Se encontraron {dt.Rows.Count} registros.");
                }
                else
                {
                    btnExportar.Enabled = false;
                    MessageBox.Show("No se encontraron registros en el rango seleccionado.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generando reporte: " + ex.Message);
            }
        }

        private void BtnExportar_Click(object? sender, EventArgs e)
        {
            if (gridReporte.DataSource is not DataTable dt || dt.Rows.Count == 0) return;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "CSV (Delimitado por comas)|*.csv";
            sfd.FileName = $"Reporte_Vacunacion_{DateTime.Now:yyyyMMdd_HHmm}.csv";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var sb = new StringBuilder();

                    // Headers
                    string[] columnNames = dt.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
                    sb.AppendLine(string.Join(";", columnNames));

                    // Rows
                    foreach (DataRow row in dt.Rows)
                    {
                        string[] fields = row.ItemArray.Select(field => field.ToString().Replace(";", ",")).ToArray();
                        sb.AppendLine(string.Join(";", fields));
                    }

                    File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Reporte exportado exitosamente.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error exportando archivo: " + ex.Message);
                }
            }
        }
    }
}
