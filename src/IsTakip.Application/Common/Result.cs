namespace IsTakip.Application.Common;

/// <summary>Servis sonuçları için basit, açık bir başarı/başarısızlık sarmalayıcısı.</summary>
public class Result
{
    public bool Succeeded { get; protected set; }
    public string? Error { get; protected set; }

    public static Result Success() => new() { Succeeded = true };
    public static Result Fail(string error) => new() { Succeeded = false, Error = error };
}

public class Result<T> : Result
{
    public T? Data { get; private set; }

    public static Result<T> Success(T data) => new() { Succeeded = true, Data = data };
    public static new Result<T> Fail(string error) => new() { Succeeded = false, Error = error };
}

/// <summary>Sayfalı liste sonucu (DataTables / liste ekranları için).</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
