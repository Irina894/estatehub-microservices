namespace Reviews.Domain.Exceptions;

/// <summary>Порушення бізнес-правила (інваріанта) домену. Перетворюється на 400.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
