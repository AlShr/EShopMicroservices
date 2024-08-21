namespace BuildingBlocks.Exceptions
{
  public class InternalServerException : Exception
  {
    public InternalServerException(string message) : base(message) { }

    public InternalServerException(string message, string details) : base(message)
    {
      this.Details = details;
    }

    public string? Details { get; }
  }
}
