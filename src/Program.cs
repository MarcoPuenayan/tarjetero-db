using System;
using System.Windows.Forms;
using TarjeteroApp.Data;

namespace TarjeteroApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Inicializar base de datos (crear archivo si no existe)
            try 
            {
                DatabaseHelper.InitializeDatabase();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inicializando base de datos: " + ex.Message);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
