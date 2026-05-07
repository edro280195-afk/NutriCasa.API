namespace NutriCasa.Domain.Exceptions;

/// <summary>
/// Excepción base para errores de dominio.
/// </summary>
public class DomainException : Exception
{
    public DomainException() { }

    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
