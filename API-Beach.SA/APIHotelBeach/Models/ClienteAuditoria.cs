using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIHotelBeach.Models
{
    public class ClienteAuditoria
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AuditId { get; set; }

        public string Accion { get; set; }

        public DateTime FechaCambio { get; set; }

        public string Cedula { get; set; }

        public string TipoCedula { get; set; }

        public string NombreCompleto { get; set; }

        public string Telefono { get; set; }

        public string Direccion { get; set; }

        public string Email { get; set; }

        public string? Password { get; set; }

        public int TipoUsuario { get; set; }

        public int Restablecer { get; set; }

        public DateTime FechaRegistro { get; set; }

        public char Estado { get; set; }
    }
}