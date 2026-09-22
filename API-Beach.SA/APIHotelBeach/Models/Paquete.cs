using System.ComponentModel.DataAnnotations;

namespace APIHotelBeach.Models
{
    public class Paquete : IValidatableObject
    {
        [Key]
        public int ID { get; set; }

        [Required(ErrorMessage = "Package name is required.")]
        [StringLength(150, ErrorMessage = "Package name cannot exceed 150 characters.")]
        public string NombrePaquete { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "Down payment is required.")]
        public decimal PorcentajePrima { get; set; }

        [Required(ErrorMessage = "Month limit is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Month limit must be zero or greater.")]
        public int LimiteMeses { get; set; }

        [Required]
        public DateTime FechaRegistro { get; set; }

        [Required]
        public char Estado { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Precio <= 0 || Precio > 9999999999.99m)
            {
                yield return new ValidationResult("Price must be between 0.01 and 9,999,999,999.99.", new[] { nameof(Precio) });
            }

            if (decimal.Round(Precio, 2) != Precio)
            {
                yield return new ValidationResult("Price can have no more than two decimal places.", new[] { nameof(Precio) });
            }

            if (PorcentajePrima < 0 || PorcentajePrima > 100)
            {
                yield return new ValidationResult("Down payment must be between 0 and 100%.", new[] { nameof(PorcentajePrima) });
            }

            if (decimal.Round(PorcentajePrima, 2) != PorcentajePrima)
            {
                yield return new ValidationResult("Down payment can have no more than two decimal places.", new[] { nameof(PorcentajePrima) });
            }

            if (Estado != 'A' && Estado != 'I')
            {
                yield return new ValidationResult("Select a valid status.", new[] { nameof(Estado) });
            }
        }
    }
}