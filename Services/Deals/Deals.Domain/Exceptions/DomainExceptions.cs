namespace Deals.Domain.Exceptions;

/// <summary>Сутність не знайдена -> HTTP 404.</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>Порушення бізнес-правила / конфлікт стану або версії -> HTTP 409.</summary>
public class BusinessConflictException(string message) : Exception(message);

/// <summary>Некоректні вхідні дані (бізнес-валідація в BLL) -> HTTP 400.</summary>
public class DomainValidationException(string message) : Exception(message);
