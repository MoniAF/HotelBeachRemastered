using System.Text;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Layout.Properties;
using iText.Layout.Borders;
using APIHotelBeach.ViewModels;


namespace APIHotelBeach.Models
{
    public class ReservationPdfGenerator
    {

        public byte[] GenerateReservation(ReservationReceiptData reservation)
        {
            byte[] pdfBytes;

            using (MemoryStream ms = new MemoryStream())
            {
                PdfWriter writer = new PdfWriter(ms);
                PdfDocument pdf = new PdfDocument(writer);
                iText.Layout.Document document = new iText.Layout.Document(pdf);
                document.SetMargins(30, 36, 30, 36);

                Style titleStyle = new Style().SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)).SetFontSize(22).SetFontColor(ColorConstants.WHITE).SetBackgroundColor(new DeviceRgb(0, 139, 139)).SetTextAlignment(TextAlignment.LEFT).SetPadding(16);

                Style subtitleStyle = new Style().SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)).SetFontSize(12).SetFontColor(new DeviceRgb(35, 55, 55)).SetBackgroundColor(new DeviceRgb(245, 241, 233)).SetTextAlignment(TextAlignment.LEFT).SetPadding(8).SetMarginTop(12).SetMarginBottom(4);

                Style textStyle = new Style().SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA)).SetFontSize(11).SetFontColor(new DeviceRgb(40, 48, 48)).SetMarginTop(0).SetMarginBottom(0);

                //Título
                Paragraph title = new Paragraph("Hotel Beach").AddStyle(titleStyle);
                document.Add(title);

                document.Add(new Paragraph("Reservation receipt").SetFontSize(12).SetFontColor(new DeviceRgb(0, 139, 139)).SetMarginBottom(8));

                //Saludo
                // document.Add(new Paragraph($"¡Hola, {reservation.NombreCompleto}! ¡Bienvenido a nuestro hotel, esperamos que disfrute su estadia!").AddStyle(textStyle));

                //Detalles reservación
                document.Add(new Paragraph("Reservation details").AddStyle(subtitleStyle).SetKeepWithNext(true));

                Table table = new Table(new float[] { 2, 3 }).SetMarginTop(6).SetWidth(UnitValue.CreatePercentValue(100));
                table.SetBorder(Border.NO_BORDER);
                table.SetProperty(Property.BORDER_COLLAPSE, BorderCollapsePropertyValue.COLLAPSE);

                table.AddCell(new Cell().Add(new Paragraph("Customer name:")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table.AddCell(new Cell().Add(new Paragraph(reservation.NombreCompleto)).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                table.AddCell(new Cell().Add(new Paragraph("Customer ID:")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table.AddCell(new Cell().Add(new Paragraph(reservation.CedulaCliente)).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                table.AddCell(new Cell().Add(new Paragraph("Reservation date:")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table.AddCell(new Cell().Add(new Paragraph(reservation.FechaReserva.ToString("DD/MM/YYYY"))).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                table.AddCell(new Cell().Add(new Paragraph("Length of stay:")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table.AddCell(new Cell().Add(new Paragraph(reservation.Duracion.ToString() + " days")).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                document.Add(table);

                //Detalles pago
                document.Add(new Paragraph("Payment details").AddStyle(subtitleStyle).SetKeepWithNext(true));

                Table table3 = new Table(new float[] { 1, 1 }).SetMarginTop(6).SetWidth(UnitValue.CreatePercentValue(100));
                table3.SetBorder(Border.NO_BORDER);
                table3.SetProperty(Property.BORDER_COLLAPSE, BorderCollapsePropertyValue.COLLAPSE);

                table3.AddCell(new Cell().Add(new Paragraph("Payment method:")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table3.AddCell(new Cell().Add(new Paragraph(GetPaymentLabel(reservation.TipoPago))).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                document.Add(table3);

                //Detalles factura
                document.Add(new Paragraph("Reservation summary").AddStyle(subtitleStyle).SetKeepWithNext(true));

                Table table2 = new Table(new float[] { 2, 3 }).SetMarginTop(6).SetWidth(UnitValue.CreatePercentValue(100));
                table2.SetBorder(Border.NO_BORDER);
                table2.SetProperty(Property.BORDER_COLLAPSE, BorderCollapsePropertyValue.COLLAPSE);

                table2.AddCell(new Cell().Add(new Paragraph("Subtotal:")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table2.AddCell(new Cell().Add(new Paragraph("$" + reservation.Subtotal.ToString())).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                table2.AddCell(new Cell().Add(new Paragraph("VAT (13%):")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table2.AddCell(new Cell().Add(new Paragraph("$" + reservation.Impuesto.ToString())).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                table2.AddCell(new Cell().Add(new Paragraph("Discount:")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table2.AddCell(new Cell().Add(new Paragraph("$" + reservation.Descuento.ToString())).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                table2.AddCell(new Cell().Add(new Paragraph("Total (USD):")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table2.AddCell(new Cell().Add(new Paragraph("$" + reservation.MontoTotal.ToString())).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                table2.AddCell(new Cell().Add(new Paragraph("Equivalent total (CRC):")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table2.AddCell(new Cell().Add(new Paragraph("CRC " + reservation.TipoCambio.ToString("N2", System.Globalization.CultureInfo.InvariantCulture))).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                table2.AddCell(new Cell().Add(new Paragraph("Down payment::")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table2.AddCell(new Cell().Add(new Paragraph("$" + reservation.Adelanto.ToString("C"))).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                table2.AddCell(new Cell().Add(new Paragraph("Monthly payment:")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table2.AddCell(new Cell().Add(new Paragraph("$" + reservation.MontoMensualidad.ToString())).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                document.Add(table2);

                //Derechos reservados
                document.Add(new Paragraph($"© {DateTime.Now.Year} Hotel Beach S.A.\nA little escape. A lasting memory.").SetFontColor(new DeviceRgb(110, 120, 120)).SetTextAlignment(TextAlignment.CENTER).SetFontSize(9).SetMarginTop(18));

                document.Close();

                pdfBytes = ms.ToArray();
            }

            return pdfBytes;
        }

        public byte[] GenerateCheck(ReservaPDFCheque reservaPDFCheque)
        {
            byte[] pdfBytes;

            using (MemoryStream ms = new MemoryStream())
            {
                PdfWriter writer = new PdfWriter(ms);
                PdfDocument pdf = new PdfDocument(writer);
                iText.Layout.Document document = new iText.Layout.Document(pdf);
                document.SetMargins(30, 36, 30, 36);

                Style titleStyle = new Style().SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)).SetFontSize(22).SetFontColor(ColorConstants.WHITE).SetBackgroundColor(new DeviceRgb(0, 139, 139)).SetTextAlignment(TextAlignment.LEFT).SetPadding(16);

                Style subtitleStyle = new Style().SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)).SetFontSize(12).SetFontColor(new DeviceRgb(35, 55, 55)).SetBackgroundColor(new DeviceRgb(245, 241, 233)).SetTextAlignment(TextAlignment.LEFT).SetPadding(8).SetMarginTop(12).SetMarginBottom(4);

                Style textStyle = new Style().SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA)).SetFontSize(11).SetFontColor(new DeviceRgb(40, 48, 48)).SetMarginTop(0).SetMarginBottom(0);

                //Título
                Paragraph title = new Paragraph("Hotel Beach").AddStyle(titleStyle);
                document.Add(title);

                document.Add(new Paragraph("Reservation receipt").SetFontSize(12).SetFontColor(new DeviceRgb(0, 139, 139)).SetMarginBottom(8));

                //Detalles de la reservación
                document.Add(new Paragraph("Reservation details").AddStyle(subtitleStyle).SetKeepWithNext(true));

                Table table = new Table(new float[] { 2, 3 }).SetMarginTop(6).SetWidth(UnitValue.CreatePercentValue(100));
                table.SetBorder(Border.NO_BORDER);
                table.SetProperty(Property.BORDER_COLLAPSE, BorderCollapsePropertyValue.COLLAPSE);

                table.AddCell(new Cell().Add(new Paragraph("Customer name:")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table.AddCell(new Cell().Add(new Paragraph(reservaPDFCheque.NombreCompleto)).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                table.AddCell(new Cell().Add(new Paragraph("Customer ID:")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table.AddCell(new Cell().Add(new Paragraph(reservaPDFCheque.CedulaCliente)).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                table.AddCell(new Cell().Add(new Paragraph("Reservation date:")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table.AddCell(new Cell().Add(new Paragraph(reservaPDFCheque.FechaReserva.ToString("dd/MM/yyyy"))).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                table.AddCell(new Cell().Add(new Paragraph("Length of stay:")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table.AddCell(new Cell().Add(new Paragraph(reservaPDFCheque.Duracion.ToString() + " days")).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                document.Add(table);

                //Detalles pago
                document.Add(new Paragraph("Payment details").AddStyle(subtitleStyle).SetKeepWithNext(true));

                Table table3 = new Table(new float[] { 2, 3 }).SetMarginTop(6).SetWidth(UnitValue.CreatePercentValue(100));
                table3.SetBorder(Border.NO_BORDER);
                table3.SetProperty(Property.BORDER_COLLAPSE, BorderCollapsePropertyValue.COLLAPSE);

                table3.AddCell(new Cell().Add(new Paragraph("Payment method:")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table3.AddCell(new Cell().Add(new Paragraph(GetPaymentLabel(reservaPDFCheque.TipoPago))).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                document.Add(table3);

                //Detalles factura
                document.Add(new Paragraph("Reservation summary").AddStyle(subtitleStyle).SetKeepWithNext(true));

                Table table2 = new Table(new float[] { 2, 3 }).SetMarginTop(6).SetWidth(UnitValue.CreatePercentValue(100));
                table2.SetBorder(Border.NO_BORDER);
                table2.SetProperty(Property.BORDER_COLLAPSE, BorderCollapsePropertyValue.COLLAPSE);

                table2.AddCell(new Cell().Add(new Paragraph("Subtotal:")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table2.AddCell(new Cell().Add(new Paragraph("$" + reservaPDFCheque.Subtotal.ToString())).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                table2.AddCell(new Cell().Add(new Paragraph("VAT (13%):")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table2.AddCell(new Cell().Add(new Paragraph("$" + reservaPDFCheque.Impuesto.ToString())).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                table2.AddCell(new Cell().Add(new Paragraph("Discount:")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table2.AddCell(new Cell().Add(new Paragraph(reservaPDFCheque.Descuento.ToString())).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                table2.AddCell(new Cell().Add(new Paragraph("Total (USD):")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table2.AddCell(new Cell().Add(new Paragraph("$" + reservaPDFCheque.MontoTotal.ToString())).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                table2.AddCell(new Cell().Add(new Paragraph("Equivalent total (CRC):")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table2.AddCell(new Cell().Add(new Paragraph("CRC " + reservaPDFCheque.TipoCambio.ToString("N2", System.Globalization.CultureInfo.InvariantCulture))).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                table2.AddCell(new Cell().Add(new Paragraph("Down payment:")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table2.AddCell(new Cell().Add(new Paragraph("$" + reservaPDFCheque.Adelanto.ToString())).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                table2.AddCell(new Cell().Add(new Paragraph("Monthly payment:")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table2.AddCell(new Cell().Add(new Paragraph("$" + reservaPDFCheque.MontoMensualidad.ToString())).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                document.Add(table2);

                //Detalles cheque
                document.Add(new Paragraph("Check details").AddStyle(subtitleStyle).SetKeepWithNext(true));

                Table table4 = new Table(new float[] { 1, 2 }).SetMarginTop(6).SetWidth(UnitValue.CreatePercentValue(100));
                table4.SetBorder(Border.NO_BORDER);
                table4.SetProperty(Property.BORDER_COLLAPSE, BorderCollapsePropertyValue.COLLAPSE);

                table4.AddCell(new Cell().Add(new Paragraph("Check number:")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table4.AddCell(new Cell().Add(new Paragraph(reservaPDFCheque.NumeroCheque.ToString())).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                table4.AddCell(new Cell().Add(new Paragraph("Bank name:")).SetFontColor(new DeviceRgb(0, 139, 139)).SetFontSize(11).SetBorder(Border.NO_BORDER));
                table4.AddCell(new Cell().Add(new Paragraph(reservaPDFCheque.NombreBanco.ToString())).AddStyle(textStyle).SetBorder(Border.NO_BORDER));

                table4.SetKeepTogether(true);
                document.Add(table4);

                // Derechos reservados
                document.Add(new Paragraph($"© {DateTime.Now.Year} Hotel Beach S.A.\nA little escape. A lasting memory.").SetFontColor(new DeviceRgb(110, 120, 120)).SetTextAlignment(TextAlignment.CENTER).SetFontSize(9).SetMarginTop(18));

                document.Close();

                pdfBytes = ms.ToArray();
            }

            return pdfBytes;
        }

        private static string GetPaymentLabel(string paymentMethod)
        {
            return paymentMethod switch
            {
                "Cheque" => "Check",
                "Efectivo" => "Cash",
                "Tarjeta" => "Card",
                _ => paymentMethod ?? ""
            };
        }
    }
}
