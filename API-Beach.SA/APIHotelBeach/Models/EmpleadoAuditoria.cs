using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIHotelBeach.Models
{
    public class EmpleadoAuditoria
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AuditId { get; set; }

        [Required]
        [StringLength(25)]
        public string Accion { get; set; } = string.Empty;

        public DateTime FechaCambio { get; set; }

        public int ID { get; set; }

        [Required]
        [StringLength(150)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Password { get; set; }

        public int TipoUsuario { get; set; }

        public DateTime FechaRegistro { get; set; }

        public char Estado { get; set; }
    }
}