using MongoDB.Bson;

namespace Reviews.Domain.Common;

/// <summary>Базова сутність: ідентифікатор, службові дати та версія для оптимістичної конкурентності.</summary>
public abstract class BaseEntity
{
    public ObjectId Id { get; protected set; } = ObjectId.GenerateNewId();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>Номер версії документа. Збільшується при кожному збереженні.</summary>
    public int Version { get; protected set; }

    protected void Touch() => UpdatedAt = DateTime.UtcNow;

    /// <summary>Викликається репозиторієм після успішного ReplaceOne.</summary>
    public void BumpVersion() => Version++;
}
