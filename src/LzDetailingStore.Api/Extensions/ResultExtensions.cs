using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Api.Extensions;

public static class ResultExtensions
{
    public static IResult ToApiResponse(this Result result) => result switch
    {
        { IsSuccess: true } => Results.Ok(),
        { StatusCode: 401 } => Results.Json(ToErrorBody(result), statusCode: 401),
        { StatusCode: 404 } => Results.NotFound(ToErrorBody(result)),
        { StatusCode: 409 } => Results.Conflict(ToErrorBody(result)),
        { StatusCode: 403 } => Results.Json(ToErrorBody(result), statusCode: 403),
        _ => Results.BadRequest(ToErrorBody(result))
    };

    public static IResult ToApiResponse<T>(this Result<T> result) => result switch
    {
        { IsSuccess: true } => Results.Ok(result.Value),
        _ => ((Result)result).ToApiResponse()
    };

    public static IResult ToCreatedResponse<T>(this Result<T> result, string uri) => result switch
    {
        { IsSuccess: true } => Results.Created(uri, result.Value),
        _ => ((Result)result).ToApiResponse()
    };

    private static object ToErrorBody(Result result) => new
    {
        error = result.Error!.Code,
        message = result.Error.Message,
        statusCode = result.StatusCode ?? 400
    };
}
