namespace TarjeteroApp.Models
{
    public class Vacuna
    {
        public int Id { get; set; }
        public string NombreBiologico { get; set; } = string.Empty; // Ej: Pentavalente
        public string Siglas { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }
    
    public class PersonalSalud
    {
        public int Id { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
    }
}
