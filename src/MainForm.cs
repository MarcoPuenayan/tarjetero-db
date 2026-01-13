using System;
using System.Drawing;
using System.Windows.Forms;

namespace TarjeteroApp
{
    public class MainForm : Form
    {
        private MenuStrip menuStrip;
        private ToolStripMenuItem menuArchivo;
        private ToolStripMenuItem menuPacientes;
        private ToolStripMenuItem menuVacunacion;
        private ToolStripMenuItem menuAdministracion;
        private ToolStripMenuItem itemSalir;
        private ToolStripMenuItem itemGestionarPacientes;
        private ToolStripMenuItem itemNuevaVacuna;
        private ToolStripMenuItem itemAdminVacunas;
        private ToolStripMenuItem itemAdminPersonal;
        private ToolStripMenuItem itemAdminRepresentantes;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Sistema de Tarjetero de Vacunación - Marianitas DB";
            this.Size = new Size(1024, 768);
            this.StartPosition = FormStartPosition.CenterScreen;

            // MENU
            menuStrip = new MenuStrip();
            
            // Archivo
            menuArchivo = new ToolStripMenuItem("Archivo");
            itemSalir = new ToolStripMenuItem("Salir", null, (s, e) => this.Close());
            menuArchivo.DropDownItems.Add(itemSalir);

            // Pacientes
            menuPacientes = new ToolStripMenuItem("Pacientes");
            itemGestionarPacientes = new ToolStripMenuItem("Gestionar Pacientes", null, OpenPacientesForm);
            menuPacientes.DropDownItems.Add(itemGestionarPacientes);

            // Vacunación
            menuVacunacion = new ToolStripMenuItem("Vacunación");
            itemNuevaVacuna = new ToolStripMenuItem("Registrar Vacuna", null, OpenVacunacionForm);
            menuVacunacion.DropDownItems.Add(itemNuevaVacuna);

            // Reportes
            var itemReportes = new ToolStripMenuItem("Reportes", null, (s, e) => new ReportesForm().ShowDialog(this));
            
            // Administración
            menuAdministracion = new ToolStripMenuItem("Administración");
            itemAdminVacunas = new ToolStripMenuItem("Gestión Biológicos (Vacunas)", null, (s, e) => new GestionVacunasForm().ShowDialog(this));
            itemAdminPersonal = new ToolStripMenuItem("Gestión Personal Salud", null, (s, e) => new GestionPersonalForm().ShowDialog(this));
            itemAdminRepresentantes = new ToolStripMenuItem("Gestión Representantes", null, (s, e) => new GestionRepresentantesForm().ShowDialog(this));
            var itemImportar = new ToolStripMenuItem("Importar Tarjetero (ODS)", null, (s, e) => new ImportarODSForm().ShowDialog(this));
            
            menuAdministracion.DropDownItems.AddRange(new ToolStripItem[] { itemAdminVacunas, itemAdminPersonal, itemAdminRepresentantes, new ToolStripSeparator(), itemImportar });

            menuStrip.Items.AddRange(new ToolStripItem[] { menuArchivo, menuPacientes, menuVacunacion, itemReportes, menuAdministracion });
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
            
            // Background branding
            Label lblTitle = new Label();
            lblTitle.Text = "DB Centro de Salud Marianitas";
            lblTitle.Font = new Font("Segoe UI", 24, FontStyle.Bold);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(50, 100);
            this.Controls.Add(lblTitle);
        }

        private void OpenPacientesForm(object? sender, EventArgs e)
        {
            // En una app real usaríamos inyección de dependencias o Singleton para formularios
            var frm = new PacientesForm();
            frm.ShowDialog(this);
        }

        private void OpenVacunacionForm(object? sender, EventArgs e)
        {
            var frm = new RegistroVacunasForm();
            frm.ShowDialog(this);
        }
    }
}
