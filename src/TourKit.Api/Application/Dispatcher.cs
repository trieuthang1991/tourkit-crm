using FluentValidation;
using TourKit.Shared.Application;

namespace TourKit.Api.Application;

/// <summary>
/// Điều phối command/query tới handler (resolve từ DI theo kiểu cụ thể) + chạy pipeline validation
/// (FluentValidation) trước handler. Không phụ thuộc MediatR (đã đổi license thương mại 2025).
/// </summary>
public sealed class Dispatcher : IDispatcher
{
    private readonly IServiceProvider _sp;

    public Dispatcher(IServiceProvider sp) => _sp = sp;

    public async Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken ct = default)
    {
        var error = await ValidateAsync(command, ct);
        if (error is not null)
        {
            return Result.Failure<TResult>(error);
        }

        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        dynamic handler = _sp.GetRequiredService(handlerType);
        return await handler.Handle((dynamic)command, ct);
    }

    public async Task<Result<TResult>> Send<TResult>(IQuery<TResult> query, CancellationToken ct = default)
    {
        var error = await ValidateAsync(query, ct);
        if (error is not null)
        {
            return Result.Failure<TResult>(error);
        }

        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        dynamic handler = _sp.GetRequiredService(handlerType);
        return await handler.Handle((dynamic)query, ct);
    }

    // Pipeline: nếu có IValidator<T> đăng ký thì chạy; fail → gom message thành Error.Validation.
    private async Task<Error?> ValidateAsync(object request, CancellationToken ct)
    {
        var validatorType = typeof(IValidator<>).MakeGenericType(request.GetType());
        if (_sp.GetService(validatorType) is not IValidator validator)
        {
            return null;
        }

        var context = new ValidationContext<object>(request);
        var result = await validator.ValidateAsync(context, ct);
        if (result.IsValid)
        {
            return null;
        }

        var message = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
        return Error.Validation(message);
    }
}
