namespace Faturamento.Domain.Entities;

public sealed class InvoiceItem
{
    private InvoiceItem() { }

    public InvoiceItem(Guid productId, string productCode, string productDescription, int quantity)
    {
        ProductId = productId;
        ProductCode = productCode;
        ProductDescription = productDescription;
        Quantity = quantity;
    }

    public long Id { get; private set; }
    public long InvoiceId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductCode { get; private set; } = string.Empty;
    public string ProductDescription { get; private set; } = string.Empty;
    public int Quantity { get; private set; }

    public void ChangeQuantity(int quantity) => Quantity = quantity;
}
