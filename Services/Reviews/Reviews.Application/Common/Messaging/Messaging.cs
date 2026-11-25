using MediatR;

namespace Reviews.Application.Common.Messaging;

/// <summary>Команда без результату (змінює стан).</summary>
public interface ICommand : IRequest { }

/// <summary>Команда з результатом.</summary>
public interface ICommand<out TResponse> : IRequest<TResponse> { }

/// <summary>Запит (лише читає дані).</summary>
public interface IQuery<out TResponse> : IRequest<TResponse> { }
