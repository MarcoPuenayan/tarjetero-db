using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SQLite;
using TarjeteroApp.Data;

namespace TarjeteroApp
{
    public class TarjeteroDigitalForm : Form
    {
        private DataGridView gridTarjetero;
        private Button btnExportar;
        private TextBox txtFiltro;

        public TarjeteroDigitalForm()
        {
            InitializeComponent();
            CargarDatos();
        }

        private void InitializeComponent()
        {
            this.Text = "Tarjetero Digital Consolidado";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;

            var panelTop = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(10) };
            
            var lblFiltro = new Label { Text = "Buscar Paciente:", AutoSize = true, Location = new Point(20, 22) };
            txtFiltro = new TextBox { Location = new Point(130, 20), Width = 300 };
            txtFiltro.TextChanged += (s, e) => FiltrarGrid();

            btnExportar = new Button { Text = "Exportar CSV", Location = new Point(450, 18), Width = 120, Height = 30 };
            btnExportar.Click += BtnExportar_Click;

            panelTop.Controls.AddRange(new Control[] { lblFiltro, txtFiltro, btnExportar });

            gridTarjetero = new DataGridView();
            gridTarjetero.Dock = DockStyle.Fill;
            gridTarjetero.AllowUserToAddRows = false;
            gridTarjetero.ReadOnly = true;
            gridTarjetero.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            
            // Estilos para que se vea mas denso (tipo Excel)
            gridTarjetero.RowHeadersVisible = false;
            gridTarjetero.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;

            this.Controls.Add(gridTarjetero);
            this.Controls.Add(panelTop);
        }

        private void CargarDatos()
        {
            try
            {
                var dt = ConstruirTablaPivote();
                gridTarjetero.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando tarjetero: " + ex.Message);
            }
        }

        private DataTable ConstruirTablaPivote()
        {
            var dt = new DataTable();
            
            // 1. Columnas Fijas
            dt.Columns.Add("Historia", typeof(string));
            dt.Columns.Add("Apellidos", typeof(string));
            dt.Columns.Add("Nombres", typeof(string));
            dt.Columns.Add("F. Nacimiento", typeof(string));
            dt.Columns.Add("Edad (Meses)", typeof(int));
            dt.Columns.Add("Sexo", typeof(string));

            // 2. Obtener Columnas Dinámicas (Vacuna + Dosis)
            // Estructura de columna: "VACUNA (DOSIS)"
            var columnasDinamicas = new List<string>();
            var mapaColumnas = new Dictionary<string, string>(); // Key: "IDVacuna_Dosis", Value: "NombreColumna"

            // Usamos una conexión para todo
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                
                // Descubrir todas las combinaciones existentes de Vacuna+Dosis
                // Ordenamos por ID de vacuna para intentar mantener el orden cronológico lógico (BCG=1, HB=2...)
                string sqlCols = @"
                    SELECT DISTINCT v.siglas, rv.numero_dosis, v.id_vacuna
                    FROM Registro_Vacunacion rv
                    JOIN Vacunas v ON rv.id_vacuna = v.id_vacuna
                    ORDER BY v.id_vacuna, rv.numero_dosis
                ";

                using (var cmd = new SQLiteCommand(sqlCols, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string siglas = reader["siglas"].ToString();
                        string dosis = reader["numero_dosis"].ToString();
                        int idVacuna = Convert.ToInt32(reader["id_vacuna"]);
                        
                        string key = $"{idVacuna}_{dosis}";
                        string colName = $"{siglas} ({dosis})";

                        if (!mapaColumnas.ContainsKey(key))
                        {
                            mapaColumnas[key] = colName;
                            columnasDinamicas.Add(colName);
                            dt.Columns.Add(colName, typeof(string));
                        }
                    }
                }

                // 3. Obtener Datos de Pacientes y Vacunas
                string sqlDatos = @"
                    SELECT 
                        p.id_paciente, 
                        p.historia_clinica, 
                        p.apellidos, 
                        p.nombres, 
                        p.fecha_nacimiento,
                        p.sexo,
                        v.id_vacuna, 
                        v.siglas, 
                        rv.numero_dosis, 
                        rv.fecha_aplicacion,
                        rv.edad_al_vacunar_meses
                    FROM Pacientes p
                    LEFT JOIN Registro_Vacunacion rv ON p.id_paciente = rv.id_paciente
                    LEFT JOIN Vacunas v ON rv.id_vacuna = v.id_vacuna
                    ORDER BY p.apellidos, p.nombres
                ";

                // Diccionario temporal para armar filas: IdPaciente -> DataRow
                var filasPaciente = new Dictionary<long, DataRow>();

                using (var cmd = new SQLiteCommand(sqlDatos, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        long idPac = Convert.ToInt64(reader["id_paciente"]);

                        if (!filasPaciente.ContainsKey(idPac))
                        {
                            DataRow row = dt.NewRow();
                            row["Historia"] = reader["historia_clinica"];
                            row["Apellidos"] = reader["apellidos"];
                            row["Nombres"] = reader["nombres"];
                            
                            string dobStr = reader["fecha_nacimiento"].ToString();
                            row["F. Nacimiento"] = dobStr;
                            row["Sexo"] = reader["sexo"];

                            // Calcular edad actual
                            if (DateTime.TryParse(dobStr, out DateTime dob))
                            {
                                int meses = (DateTime.Now.Year - dob.Year) * 12 + DateTime.Now.Month - dob.Month;
                                if (DateTime.Now.Day < dob.Day) meses--;
                                row["Edad (Meses)"] = meses; // Edad actual
                            }
                            else 
                            { 
                                row["Edad (Meses)"] = 0; 
                            }

                            dt.Rows.Add(row);
                            filasPaciente[idPac] = row;
                        }

                        // Llenar celda de vacuna si existe
                        if (reader["id_vacuna"] != DBNull.Value)
                        {
                            int idVac = Convert.ToInt32(reader["id_vacuna"]);
                            string dosis = reader["numero_dosis"].ToString();
                            string colKey = $"{idVac}_{dosis}";
                            
                            if (mapaColumnas.ContainsKey(colKey))
                            {
                                string colName = mapaColumnas[colKey];
                                
                                string fechaApp = "";
                                if (DateTime.TryParse(reader["fecha_aplicacion"].ToString(), out DateTime fApp))
                                {
                                    fechaApp = fApp.ToString("dd/MM/yy");
                                }
                                
                                // Mostrar: Fecha (Edad) -> ej: "12/05/23 (2m)"
                                string edadVac = "";
                                if (reader["edad_al_vacunar_meses"] != DBNull.Value)
                                {
                                    edadVac = $" ({reader["edad_al_vacunar_meses"]}m)";
                                }

                                filasPaciente[idPac][colName] = $"{fechaApp}{edadVac}";
                            }
                        }
                    }
                }
            }
            
            return dt;
        }

        private void FiltrarGrid()
        {
            if (gridTarjetero.DataSource is DataTable dt)
            {
                var filtro = txtFiltro.Text;
                dt.DefaultView.RowFilter = string.Format("Apellidos LIKE '%{0}%' OR Nombres LIKE '%{0}%' OR Historia LIKE '%{0}%'", filtro);
            }
        }

        private void BtnExportar_Click(object? sender, EventArgs e)
        {
             if (gridTarjetero.DataSource is DataTable dt)
            {
                SaveFileDialog sfd = new SaveFileDialog() { Filter = "CSV|*.csv", FileName = "TarjeteroDigital.csv" };
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var sb = new StringBuilder();
                        var headers = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
                        sb.AppendLine(string.Join(",", headers));

                        foreach (DataRow row in dt.Rows)
                        {
                            var fields = row.ItemArray.Select(field => field.ToString().Replace(",", ";"));
                            sb.AppendLine(string.Join(",", fields));
                        }

                        System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
                        MessageBox.Show("Exportado correctamente.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al exportar: " + ex.Message);
                    }
                }
            }
        }
    }
}
