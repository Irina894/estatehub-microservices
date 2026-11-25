namespace Catalog.Domain.Exceptions;

/// <summary>Порушення бізнес-правила сутності (ціна &lt;= 0, недопустимий перехід статусу тощо). API: 409.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

/// <summary>Ресурс не знайдено. API: 404.</summary>
public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found") { }

    public EntityNotFoundException(string message) : base(message) { }
}

/// <summary>Конфлікт з поточним станом даних (дублікат назви, дублікат зв'язку). API: 409.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
