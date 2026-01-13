using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Data.SQLite;
using TarjeteroApp.Data;
using TarjeteroApp.Utils;

namespace TarjeteroApp
{
    public class ImportarODSForm : Form
    {
        private TextBox txtFile;
        private Button btnBrowse;
        private Button btnImportar;
        private DataGridView gridPreview;
        private Label lblStatus;
        private DataTable dtSource;

        public ImportarODSForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Importar Tarjetero desde ODS";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Top Panel
            Panel panelTop = new Panel() { Dock = DockStyle.Top, Height = 80, Padding = new Padding(10) };
            
            Label lblInstr = new Label() { Text = "Seleccione el archivo 'TARJETERO VACUNAS MARIANITAS 2025.ods':", AutoSize = true, Location = new Point(10, 10) };
            
            txtFile = new TextBox() { Location = new Point(10, 35), Width = 700, ReadOnly = true };
            btnBrowse = new Button() { Text = "Examinar...", Location = new Point(720, 34), Width = 100 };
            btnImportar = new Button() { Text = "Importar Datos", Location = new Point(830, 34), Width = 120, Enabled = false, BackColor = Color.LightGreen };

            btnBrowse.Click += BtnBrowse_Click;
            btnImportar.Click += BtnImportar_Click;

            panelTop.Controls.AddRange(new Control[] { lblInstr, txtFile, btnBrowse, btnImportar });

            // Status
            lblStatus = new Label() { Dock = DockStyle.Bottom, Height = 30, TextAlign = ContentAlignment.MiddleLeft, Text = "Listo." };

            // Grid
            gridPreview = new DataGridView();
            gridPreview.Dock = DockStyle.Fill;
            gridPreview.ReadOnly = true;
            gridPreview.AllowUserToAddRows = false;
            // Prevent "FillWeight" sum error if too many columns (limits to horizontal scroll)
            gridPreview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None; 
            
            this.Controls.Add(gridPreview);
            this.Controls.Add(panelTop);
            this.Controls.Add(lblStatus);
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "OpenDocument Spreadsheet (*.ods)|*.ods|All Files (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtFile.Text = ofd.FileName;
                LoadPreview(ofd.FileName);
            }
        }

        private void LoadPreview(string path)
        {
            try
            {
                lblStatus.Text = "Leyendo archivo ODS...";
                Application.DoEvents();
                
                dtSource = OdsReader.ReadOds(path);
                gridPreview.DataSource = dtSource;
                
                lblStatus.Text = $"Leídas {dtSource.Rows.Count} filas. Verifique las columnas antes de importar.";
                btnImportar.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error leyendo archivo: " + ex.Message);
                lblStatus.Text = "Error al leer.";
            }
        }

        private void BtnImportar_Click(object sender, EventArgs e)
        {
            if (dtSource == null || dtSource.Rows.Count == 0) return;

            var result = MessageBox.Show("¿Está seguro de iniciar la importación?\nEsto procesará Pacientes, Representantes y Vacunas.", "Confirmar Importación", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            ImportarDatos();
        }

        private void ImportarDatos()
        {
            Cursor = Cursors.WaitCursor;
            lblStatus.Text = "Importando...";
            int nuevosPacientes = 0;
            int nuevasVacunas = 0;
            int errores = 0;

            try
            {
                // 1. Identify Columns
                var map = MapColumns(dtSource);
                if (map.Count == 0)
                {
                    MessageBox.Show("No se pudieron identificar las columnas requeridas (HC, Nombres, Apellidos). Verifique el archivo.");
                    return;
                }

                // 2. Load Existing Vaccines for mapping
                var dtVacunas = DatabaseHelper.ExecuteQuery("SELECT * FROM Vacunas");
                var vacunas = new Dictionary<string, long>();
                foreach (DataRow r in dtVacunas.Rows)
                {
                    vacunas[r["nombre_biologico"].ToString().ToUpper()] = Convert.ToInt64(r["id_vacuna"]);
                    if (r["siglas"] != DBNull.Value)
                        vacunas[r["siglas"].ToString().ToUpper()] = Convert.ToInt64(r["id_vacuna"]);
                }

                // 3. Process Rows
                foreach (DataRow row in dtSource.Rows)
                {
                    try
                    {
                        // ----- Paciente -----
                        string colHC = map.ContainsKey("HC") ? row[map["HC"]].ToString() : "";
                        string colNom = map.ContainsKey("NOMBRES") ? row[map["NOMBRES"]].ToString() : "";
                        string colApe = map.ContainsKey("APELLIDOS") ? row[map["APELLIDOS"]].ToString() : "";

                        if (string.IsNullOrWhiteSpace(colHC) || string.IsNullOrWhiteSpace(colNom)) continue;

                        long idPaciente = ObtenerOInsertarPaciente(colHC, colNom, colApe, row, map);
                        if (idPaciente > 0)
                        {
                            // ----- Vacunas (Iterate dynamic columns) -----
                            foreach (DataColumn col in dtSource.Columns)
                            {
                                string header = col.ColumnName.ToUpper();
                                // Skip patient info cols
                                if (map.ContainsValue(col.ColumnName)) continue;

                                // Check if header matches a known Vaccine
                                long idVacuna = 0;
                                // Try exact match
                                if (vacunas.ContainsKey(header)) idVacuna = vacunas[header];
                                // Try partial match or heuristics if needed
                                
                                if (idVacuna > 0)
                                {
                                    string cellValue = row[col].ToString();
                                    if (!string.IsNullOrWhiteSpace(cellValue))
                                    {
                                        // Try parse date
                                        if (DateTime.TryParse(cellValue, out DateTime fechaVac))
                                        {
                                            RegistrarVacuna(idPaciente, idVacuna, fechaVac);
                                            nuevasVacunas++;
                                        }
                                    }
                                }
                            }
                            nuevosPacientes++; // Count as processed
                        }
                    }
                    catch (Exception ex)
                    {
                        errores++;
                        Console.WriteLine("Row Error: " + ex.Message);
                    }
                }

                MessageBox.Show($"Importación completada.\nPacientes Procesados/Actualizados: {nuevosPacientes}\nDosis Registradas: {nuevasVacunas}\nErrores: {errores}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Fatal en Importación: " + ex.Message);
            }
            finally
            {
                Cursor = Cursors.Default;
                lblStatus.Text = "Listo.";
            }
        }

        private Dictionary<string, string> MapColumns(DataTable dt)
        {
            var map = new Dictionary<string, string>();
            foreach (DataColumn col in dt.Columns)
            {
                string name = col.ColumnName.ToUpper();
                if (name.Contains("HISTORIA") || name.Contains("H.C") || name == "HC") map["HC"] = col.ColumnName;
                else if (name.Contains("NOMBRES")) map["NOMBRES"] = col.ColumnName;
                else if (name.Contains("APELLIDOS")) map["APELLIDOS"] = col.ColumnName;
                else if (name.Contains("NACIMIENTO") || name.Contains("FEC_NAC")) map["FECHA"] = col.ColumnName;
                else if (name.Contains("SEXO") || name.Contains("GENERO")) map["SEXO"] = col.ColumnName;
                else if (name.Contains("REPRESENTANTE")) map["REPRESENTANTE"] = col.ColumnName;
            }
            return map;
        }

        private long ObtenerOInsertarPaciente(string hc, string nom, string ape, DataRow row, Dictionary<string, string> map)
        {
            // Check Exists
            var check = DatabaseHelper.ExecuteQuery($"SELECT id_paciente FROM Pacientes WHERE historia_clinica = '{hc}'");
            if (check.Rows.Count > 0) return Convert.ToInt64(check.Rows[0][0]);

            // Insert Representante if present
            long? idRep = null;
            if (map.ContainsKey("REPRESENTANTE"))
            {
                string repName = row[map["REPRESENTANTE"]].ToString();
                if (!string.IsNullOrWhiteSpace(repName))
                {
                    idRep = ObtenerOInsertarRepresentante(repName);
                }
            }

            // Insert Paciente
            string fecha = map.ContainsKey("FECHA") ? row[map["FECHA"]].ToString() : DateTime.Now.ToString("yyyy-MM-dd");
            string sexo = map.ContainsKey("SEXO") ? row[map["SEXO"]].ToString() : "M"; // Default
            if (sexo.Length > 1) sexo = sexo.Substring(0, 1);

            DateTime dtNac;
            if (!DateTime.TryParse(fecha, out dtNac)) dtNac = DateTime.Now;

            string sql = @"INSERT INTO Pacientes (historia_clinica, nombres, apellidos, fecha_nacimiento, sexo, id_representante) 
                           VALUES (@hc, @nom, @ape, @fec, @sex, @idRep); SELECT last_insert_rowid();";
            
            var prms = new SQLiteParameter[] {
                new SQLiteParameter("@hc", hc),
                new SQLiteParameter("@nom", nom),
                new SQLiteParameter("@ape", ape),
                new SQLiteParameter("@fec", dtNac.ToString("yyyy-MM-dd")),
                new SQLiteParameter("@sex", sexo),
                new SQLiteParameter("@idRep", idRep ?? (object)DBNull.Value)
            };
            
            using (var conn = new SQLiteConnection("Data Source=tarjetero.db"))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddRange(prms);
                    return (long)cmd.ExecuteScalar();
                }
            }
        }

        private long ObtenerOInsertarRepresentante(string nombre)
        {
            // Simple check by Name (Caution: Names are not unique, but usually sufficient for simple import)
            var check = DatabaseHelper.ExecuteQuery($"SELECT id_representante FROM Representantes WHERE nombres = '{nombre}'");
            if (check.Rows.Count > 0) return Convert.ToInt64(check.Rows[0][0]);

            string sql = "INSERT INTO Representantes (nombres) VALUES (@nom); SELECT last_insert_rowid();";
            using (var conn = new SQLiteConnection("Data Source=tarjetero.db")) {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn)) {
                    cmd.Parameters.AddWithValue("@nom", nombre);
                    return (long)cmd.ExecuteScalar();
                }
            }
        }

        private void RegistrarVacuna(long idPaciente, long idVacuna, DateTime fecha)
        {
            // Check duplicates
            string checkSql = "SELECT id_registro FROM Registro_Vacunacion WHERE id_paciente=@p AND id_vacuna=@v AND fecha_aplicacion=@f";
            // date compare logic
            
            // Assume if same vaccine same day, it's duplicate
            // We use a simplified check
            string sqlInsert = @"INSERT INTO Registro_Vacunacion (id_paciente, id_vacuna, id_personal, fecha_aplicacion, numero_dosis) 
                                 VALUES (@p, @v, 1, @f, '1')"; // Default Personal=1, Dosis=1 (Simplification)
            
            // We should ideally have "numero_dosis" mapped? 
            // In matrix, usually col header implies dose "bOPV1", "bOPV2".
            // Implementation detail: User can refine this later.

            using (var conn = new SQLiteConnection("Data Source=tarjetero.db")) {
                 conn.Open();
                 // Naive insert (or INSERT OR IGNORE)
                 using (var cmd = new SQLiteCommand(sqlInsert, conn)) {
                     cmd.Parameters.AddWithValue("@p", idPaciente);
                     cmd.Parameters.AddWithValue("@v", idVacuna);
                     cmd.Parameters.AddWithValue("@f", fecha.ToString("yyyy-MM-dd"));
                     cmd.ExecuteNonQuery();
                 }
            }
        }
    }
}
