namespace MyAdmin.Core.Common;

public class ApiResponse<T>
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Success(T? data, string message = "操作成功")
        => new() { Code = 200, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, int code = 500)
        => new() { Code = code, Message = message, Data = default };
}

public class PagedResult<T>
{
    public int Total { get; set; }
    public List<T> Items { get; set; } = new();
}
