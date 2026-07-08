namespace TourKit.Shared.Application;

/// <summary>Lệnh ghi (thay đổi trạng thái) trả <typeparamref name="TResult"/>.</summary>
public interface ICommand<TResult>;

/// <summary>Truy vấn đọc trả <typeparamref name="TResult"/>.</summary>
public interface IQuery<TResult>;

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> Handle(TCommand command, CancellationToken ct);
}

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<Result<TResult>> Handle(TQuery query, CancellationToken ct);
}

/// <summary>Điều phối lệnh/truy vấn tới handler + chạy pipeline (validation...). Endpoint chỉ gọi Send.</summary>
public interface IDispatcher
{
    Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken ct = default);
    Task<Result<TResult>> Send<TResult>(IQuery<TResult> query, CancellationToken ct = default);
}
