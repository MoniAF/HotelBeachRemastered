using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIHotelBeach.Models
{
    public class Cheque
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int IdCheque { get; set; }

        public int NumeroCheque { get; set; }

        public string NombreBanco { get; set; }

        public int IdReservacion { get; set; }
    }
}