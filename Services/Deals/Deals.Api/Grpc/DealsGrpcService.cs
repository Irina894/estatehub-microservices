using Deals.Bll.DTOs;
using Deals.Bll.Services;
using Deals.Domain.Exceptions;
using Deals.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Deals.Api.Grpc;

/// <summary>
/// gRPC-адаптер над BLL: валідує вхід, викликає <see cref="IApplicationService"/> (той самий кешований сервіс, що й REST)
/// і перетворює доменну модель у повідомлення та доменні винятки у <see cref="RpcException"/>.
/// </summary>
public class DealsGrpcService(IApplicationService applications) : DealsService.DealsServiceBase
{
    public override async Task<ApplicationReply> GetApplication(GetApplicationRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id) || id == Guid.Empty)
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"'{request.Id}' is not a valid GUID"));

        try
        {
            return ToReply(await applications.GetAsync(id));
        }
        catch (NotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    private static ApplicationReply ToReply(ApplicationDto a) => new()
    {
        Id = a.Id.ToString(),
        ClientId = a.ClientId.ToString(),
        PropertyId = a.PropertyId.ToString(),
        PriceSnapshot = (double)a.PriceSnapshot,
        PropertyTitleSnapshot = a.PropertyTitleSnapshot,
        Status = a.Status,
        // з SQL дата приходить з Kind=Unspecified: вважаємо її UTC, бо так вона зберігається
        CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(a.CreatedAt, DateTimeKind.Utc))
    };
}
