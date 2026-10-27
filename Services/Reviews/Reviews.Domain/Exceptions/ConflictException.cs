namespace Reviews.Domain.Exceptions;

/// <summary>Конфлікт версій / дублікат. Перетворюється на 409.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
