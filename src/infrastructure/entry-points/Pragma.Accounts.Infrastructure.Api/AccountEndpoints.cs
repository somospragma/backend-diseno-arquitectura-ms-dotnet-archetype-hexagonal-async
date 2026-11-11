using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Pragma.Accounts.Domain.Model.Exceptions;
using Pragma.Accounts.Domain.UseCases;

namespace Pragma.Accounts.Infrastructure.Api;

/// <summary>
/// Endpoints de cuentas: GET, POST, PUT, PATCH, DELETE y el metodo QUERY
/// (busqueda con body, draft IETF httpbis-safe-method-w-body).
/// </summary>
public static class AccountEndpoints
{
    public const string BasePath = "/api/v1/accounts";
    private const int DefaultLimit = 20;

    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);
        var group = routes.MapGroup(BasePath).WithTags("Accounts");

        group.MapGet("/", FindAllAsync).WithName("ListAccounts");
        group.MapGet("/{id}", FindByIdAsync).WithName("GetAccount").ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/", CreateAsync).WithName("CreateAccount").ProducesValidationProblem().ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPut("/{id}", UpdateAsync).WithName("UpdateAccount").ProducesValidationProblem().ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPatch("/{id}", PatchAsync).WithName("PatchAccount").ProducesValidationProblem().ProducesProblem(StatusCodes.Status404NotFound);
        group.MapDelete("/{id}", DeleteAsync).WithName("DeleteAccount").ProducesProblem(StatusCodes.Status404NotFound);
        group.MapMethods("/search", ["QUERY"], SearchAsync).WithName("SearchAccounts");
        group.MapGet("/{id}/credit-score", CreditScoreAsync).WithName("GetCreditScore")
            .ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status503ServiceUnavailable);
        return routes;
    }

    internal static async Task<Ok<List<AccountResponse>>> FindAllAsync(
        [FromServices] GetAccountUseCase useCase, CancellationToken ct, [FromQuery] int limit = DefaultLimit)
    {
        var accounts = await useCase.FindAllAsync(limit, ct).ConfigureAwait(false);
        return TypedResults.Ok(accounts.Select(AccountRestMapper.ToResponse).ToList());
    }

    internal static async Task<Ok<AccountResponse>> FindByIdAsync(string id, [FromServices] GetAccountUseCase useCase, CancellationToken ct)
    {
        var account = await useCase.ExecuteAsync(id, ct).ConfigureAwait(false);
        return TypedResults.Ok(AccountRestMapper.ToResponse(account));
    }

    internal static async Task<Results<Created<AccountResponse>, ValidationProblem>> CreateAsync(
        [FromBody] AccountRequest request, [FromServices] CreateAccountUseCase useCase, CancellationToken ct)
    {
        var errors = RequestValidator.Validate(request);
        if (errors.Count > 0)
        {
            return Validation(errors);
        }

        var created = await useCase.ExecuteAsync(AccountRestMapper.ToDomain(request), ct).ConfigureAwait(false);
        return TypedResults.Created($"{BasePath}/{created.Id}", AccountRestMapper.ToResponse(created));
    }

    internal static async Task<Results<Ok<AccountResponse>, ValidationProblem>> UpdateAsync(
        string id, [FromBody] AccountRequest request, [FromServices] UpdateAccountUseCase useCase, CancellationToken ct)
    {
        var errors = RequestValidator.Validate(request);
        if (errors.Count > 0)
        {
            return Validation(errors);
        }

        var updated = await useCase.ExecuteAsync(id, AccountRestMapper.ToDomain(request), ct).ConfigureAwait(false);
        return TypedResults.Ok(AccountRestMapper.ToResponse(updated));
    }

    internal static async Task<Results<Ok<AccountResponse>, ValidationProblem>> PatchAsync(
        string id, [FromBody] AccountPatchRequest request, [FromServices] PatchAccountUseCase useCase, CancellationToken ct)
    {
        var errors = RequestValidator.Validate(request);
        if (errors.Count > 0)
        {
            return Validation(errors);
        }

        var patched = await useCase.ExecuteAsync(id, AccountRestMapper.ToDomain(request), ct).ConfigureAwait(false);
        return TypedResults.Ok(AccountRestMapper.ToResponse(patched));
    }

    internal static async Task<NoContent> DeleteAsync(string id, [FromServices] DeleteAccountUseCase useCase, CancellationToken ct)
    {
        await useCase.ExecuteAsync(id, ct).ConfigureAwait(false);
        return TypedResults.NoContent();
    }

    internal static async Task<Ok<List<AccountResponse>>> SearchAsync(
        [FromBody] AccountSearchRequest request, [FromServices] SearchAccountsUseCase useCase, CancellationToken ct,
        [FromQuery] int limit = DefaultLimit)
    {
        var accounts = await useCase.ExecuteAsync(AccountRestMapper.ToDomain(request), limit, ct).ConfigureAwait(false);
        return TypedResults.Ok(accounts.Select(AccountRestMapper.ToResponse).ToList());
    }

    internal static async Task<Ok<CreditScoreResponse>> CreditScoreAsync(string id, [FromServices] GetCreditScoreUseCase useCase, CancellationToken ct)
    {
        var score = await useCase.ExecuteAsync(id, ct).ConfigureAwait(false);
        return TypedResults.Ok(AccountRestMapper.ToResponse(score));
    }

    private static ValidationProblem Validation(Dictionary<string, string[]> errors) =>
        TypedResults.ValidationProblem(
            errors,
            title: ErrorCatalog.ValidationError.DefaultMessage,
            extensions: new Dictionary<string, object?> { ["code"] = ErrorCatalog.ValidationError.Code });
}
