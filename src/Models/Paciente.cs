using System;

namespace TarjeteroApp.Models
{
    public class Paciente
    {
        public int Id { get; set; }
        public string HistoriaClinica { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; }
        public char Sexo { get; set; }
        public int? IdRepresentante { get; set; }
        
        public string NombreCompleto => $"{Nombres} {Apellidos}";
    }
}
