using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Platform.Aggregator.Grpc;

/// <summary>Клієнтський interceptor: логує кожен вихідний унарний gRPC-виклик (метод, статус, тривалість).</summary>
public sealed class GrpcClientLoggingInterceptor(ILogger<GrpcClientLoggingInterceptor> logger) : Interceptor
{
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var started = Stopwatch.GetTimestamp();
        var call = continuation(request, context);

        return new AsyncUnaryCall<TResponse>(
            LogAsync(call.ResponseAsync, context.Method.FullName, started),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    private async Task<TResponse> LogAsync<TResponse>(Task<TResponse> response, string method, long started)
    {
        try
        {
            var result = await response;
            logger.LogInformation("gRPC client {Method}: {Status} in {Elapsed} ms",
                method, StatusCode.OK, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            return result;
        }
        catch (RpcException ex)
        {
            logger.LogWarning("gRPC client {Method}: {Status} in {Elapsed} ms",
                method, ex.StatusCode, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            throw;
        }
    }
}
