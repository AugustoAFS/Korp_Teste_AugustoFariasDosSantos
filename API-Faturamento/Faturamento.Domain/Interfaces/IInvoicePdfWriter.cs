using Faturamento.Domain.Entities;

namespace Faturamento.Domain.Interfaces;

public interface IInvoicePdfWriter
{
    byte[] Write(Invoice invoice);
}
