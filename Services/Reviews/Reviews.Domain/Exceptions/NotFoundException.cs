namespace Reviews.Domain.Exceptions;

/// <summary>Сутність не знайдена. Перетворюється на 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entity, object key)
        : base($"{entity} з ідентифікатором '{key}' не знайдено.") { }
}
