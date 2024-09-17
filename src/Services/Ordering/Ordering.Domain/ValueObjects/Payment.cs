namespace Ordering.Domain.ValueObjects
{
  public record Payment
  {
    public string? CartName { get; set; } = default!;
    public string CartNumber { get; set; } = default!;
    public string Expiration { get; set; } = default!;
    public string Cvv { get; set; } = default!;
    public int PaymentMethod { get; set; } = default!;

    protected Payment()
    {
    }

    private Payment(string cartName, string cartNumber, string expiration, string cvv, int paymentMethod)
    {
      this.CartName = cartName;
      this.CartNumber = cartNumber;
      this.Expiration = expiration;
      this.Cvv = cvv;
      this.PaymentMethod = paymentMethod;
    }

    public static Payment Of(string cartName, string cartNumber, string expiration, string cVV, int paymentMethod)
    {
      ArgumentException.ThrowIfNullOrWhiteSpace(cartName);
      ArgumentException.ThrowIfNullOrWhiteSpace(cartNumber);
      ArgumentException.ThrowIfNullOrWhiteSpace(cVV);
      ArgumentOutOfRangeException.ThrowIfGreaterThan(cVV.Length, 3);

      return new Payment(cartName, cartNumber, expiration, cVV, paymentMethod);
    }
  }
}
