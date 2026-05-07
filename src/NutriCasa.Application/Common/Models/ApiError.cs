namespace NutriCasa.Application.Common.Models;

public class ApiError
{
    public string Code { get; set; } = null!;
    public string Message { get; set; } = null!;
    public IDictionary<string, string[]>? Details { get; set; }
}
