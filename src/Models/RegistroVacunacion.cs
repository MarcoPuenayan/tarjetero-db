using System;

namespace TarjeteroApp.Models
{
    public class RegistroVacunacion
    {
        public int Id { get; set; }
        public int IdPaciente { get; set; }
        public int IdVacuna { get; set; }
        public int IdPersonal { get; set; }
        public DateTime FechaAplicacion { get; set; }
        public string NumeroDosis { get; set; } = string.Empty; // 1ra, 2da
        public string LoteBiologico { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        
        // Propiedades de navegación (simplificadas para el ejemplo)
        public Paciente? Paciente { get; set; }
        public Vacuna? Vacuna { get; set; }
    }
}
