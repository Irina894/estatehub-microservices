using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Platform.ServiceDefaults;

/// <summary>
/// Серверний interceptor: структурований лог кожного унарного gRPC-виклику (метод, адреса клієнта, статус, тривалість).
/// Властивості CorrelationId, TraceId, SpanId додають Serilog (LogContext від CorrelationIdMiddleware) та OpenTelemetry.
/// Підключається викликом <c>services.AddServiceGrpc()</c>.
/// </summary>
public sealed class GrpcLoggingInterceptor(ILogger<GrpcLoggingInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var started = Stopwatch.GetTimestamp();

        // бізнес-контекст у span (rpc.service / rpc.method вже додає інструментація ASP.NET Core)
        Activity.Current?.SetTag("rpc.peer", context.Peer);
        Activity.Current?.SetTag("correlation.id", context.RequestHeaders.GetValue("x-correlation-id"));

        try
        {
            var response = await continuation(request, context);
            logger.LogInformation("gRPC {Method} from {Peer}: {Status} in {Elapsed} ms",
                context.Method, context.Peer, StatusCode.OK, Elapsed(started));
            return response;
        }
        catch (RpcException ex)
        {
            // очікувані помилки (NotFound, InvalidArgument) - попередження зі статусом, без шуму stack trace
            logger.LogWarning(ex, "gRPC {Method} from {Peer}: {Status} in {Elapsed} ms",
                context.Method, context.Peer, ex.StatusCode, Elapsed(started));
            throw;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("gRPC {Method} from {Peer}: {Status} after {Elapsed} ms",
                context.Method, context.Peer, StatusCode.Cancelled, Elapsed(started));
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "gRPC {Method} from {Peer}: {Status} in {Elapsed} ms",
                context.Method, context.Peer, StatusCode.Unknown, Elapsed(started));
            throw;
        }
    }

    private static double Elapsed(long started) => Stopwatch.GetElapsedTime(started).TotalMilliseconds;
}
