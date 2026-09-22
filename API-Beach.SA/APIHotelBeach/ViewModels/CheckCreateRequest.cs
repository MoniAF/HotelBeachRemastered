namespace APIHotelBeach.ViewModels
{
    public class CheckCreateRequest
    {
        public int IdCheque { get; set; }

        public int NumeroCheque { get; set; }

        public string NombreBanco { get; set; }

        public int IdReservacion { get; set; }
    }
}
