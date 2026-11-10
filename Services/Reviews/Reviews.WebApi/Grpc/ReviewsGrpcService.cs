using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Reviews.Application.Common.Models;
using Reviews.Application.Queries.GetReviewById;
using Reviews.Application.Queries.GetReviewsList;
using Reviews.Domain.Exceptions;
using Reviews.Grpc;

namespace Reviews.WebApi.Grpc;

/// <summary>
/// gRPC-адаптер над шаром Application (як REST-контролер): надсилає ті самі запити MediatR, тож проходить ті самі
/// behaviors (логування, валідація, кеш). Доменні й валідаційні винятки перетворюються на <see cref="RpcException"/>.
/// </summary>
public class ReviewsGrpcService(ISender sender) : ReviewsService.ReviewsServiceBase
{
    public override async Task<ReviewReply> GetReview(GetReviewRequest request, ServerCallContext context)
    {
        var review = await SendAsync(new GetReviewByIdQuery(request.Id), context);
        return new ReviewReply
        {
            Id = review.Id,
            ProductId = review.ProductId,
            ProductName = review.ProductName,
            AuthorName = review.Author.DisplayName,
            Rating = review.Rating,
            Text = review.Text,
            CommentsCount = review.Comments.Count,
            CreatedAt = ToTimestamp(review.CreatedAt)
        };
    }

    public override async Task<ReviewsReply> GetReviewsByProduct(GetReviewsByProductRequest request, ServerCallContext context)
    {
        var page = await SendAsync(new GetReviewsListQuery(
            request.ProductId,
            MinRating: null,
            Search: null,
            Page: request.Page == 0 ? 1 : request.Page,
            PageSize: request.PageSize == 0 ? 20 : request.PageSize), context);

        var reply = new ReviewsReply { TotalCount = page.TotalCount };
        foreach (var r in page.Items)
        {
            reply.Reviews.Add(new ReviewReply
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.ProductName,
                AuthorName = r.AuthorName,
                Rating = r.Rating,
                Text = r.Text,
                CommentsCount = r.CommentsCount,
                CreatedAt = ToTimestamp(r.CreatedAt)
            });
        }
        return reply;
    }

    /// <summary>Виконує запит і перетворює винятки на правильні gRPC-статуси (а не Unknown/Internal).</summary>
    private async Task<T> SendAsync<T>(IRequest<T> query, ServerCallContext context)
    {
        try
        {
            return await sender.Send(query, context.CancellationToken);
        }
        catch (NotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (FluentValidation.ValidationException ex)
        {
            var message = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage));
            throw new RpcException(new Status(StatusCode.InvalidArgument, message));
        }
    }

    private static Timestamp ToTimestamp(DateTime value) =>
        Timestamp.FromDateTime(value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
