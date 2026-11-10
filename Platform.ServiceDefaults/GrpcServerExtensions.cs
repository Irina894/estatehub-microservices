using Platform.ServiceDefaults;

namespace Microsoft.Extensions.DependencyInjection;

public static class GrpcServerExtensions
{
    /// <summary>
    /// Реєструє gRPC-сервер із логуванням викликів. gRPC працює поверх HTTP/2: Kestrel за замовчуванням
    /// узгоджує HTTP/2 через ALPN на HTTPS-endpoint (Http1AndHttp2), тому REST (HTTP/1.1) і gRPC живуть в одному порту.
    /// </summary>
    public static IServiceCollection AddServiceGrpc(this IServiceCollection services)
    {
        services.AddGrpc(options => options.Interceptors.Add<GrpcLoggingInterceptor>());
        return services;
    }
}
