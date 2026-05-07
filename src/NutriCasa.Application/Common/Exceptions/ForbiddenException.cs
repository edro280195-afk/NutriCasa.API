namespace NutriCasa.Application.Common.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException() : base("No tienes permiso para realizar esta acción.") { }

    public ForbiddenException(string message) : base(message) { }
}
