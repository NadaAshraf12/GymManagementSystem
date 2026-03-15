using System.Text;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;

namespace GymManagementSystem.Infrastructure.Services;

public class InvoicePdfGenerator : IInvoicePdfGenerator
{
    public async Task<string> GenerateInvoicePdfAsync(InvoiceReadDto dto, CancellationToken cancellationToken = default)
    {
        var rootPath = Path.Combine(Directory.GetCurrentDirectory(), "GymManagementSystem.WebUI", "wwwroot");
        var directory = Path.Combine(rootPath, "uploads", "invoices");
        Directory.CreateDirectory(directory);

        var fileName = $"{dto.InvoiceNumber}.pdf";
        var path = Path.Combine(directory, fileName);

        // Minimal valid PDF payload for server-side invoice export.
        var pdf = BuildSimplePdf(dto);
        await File.WriteAllBytesAsync(path, pdf, cancellationToken);
        return path;
    }

    private static byte[] BuildSimplePdf(InvoiceReadDto dto)
    {
        var text = $"Invoice {dto.InvoiceNumber}  Membership:{dto.MembershipId}  Amount:{dto.Amount:0.00}  Type:{dto.Type}";
        var escaped = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        var streamContent = $"BT /F1 12 Tf 50 750 Td ({escaped}) Tj ET";
        var contentLen = Encoding.ASCII.GetByteCount(streamContent);

        var body = $"""
1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj
2 0 obj << /Type /Pages /Count 1 /Kids [3 0 R] >> endobj
3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj
4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj
5 0 obj << /Length {contentLen} >> stream
{streamContent}
endstream endobj
""";

        var header = "%PDF-1.4\n";
        var objects = header + body;
        var xrefPosition = Encoding.ASCII.GetByteCount(objects);
        var xref = """
xref
0 6
0000000000 65535 f 
0000000009 00000 n 
0000000058 00000 n 
0000000115 00000 n 
0000000241 00000 n 
0000000311 00000 n 
""";
        var trailer = $"""
trailer << /Size 6 /Root 1 0 R >>
startxref
{xrefPosition}
%%EOF
""";

        return Encoding.ASCII.GetBytes(objects + xref + trailer);
    }
}
