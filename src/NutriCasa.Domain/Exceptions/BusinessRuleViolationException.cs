namespace NutriCasa.Domain.Exceptions;

/// <summary>
/// Se lanza cuando una regla de negocio del dominio es violada.
/// </summary>
public class BusinessRuleViolationException : DomainException
{
    public string RuleCode { get; }

    public BusinessRuleViolationException(string ruleCode, string message)
        : base(message)
    {
        RuleCode = ruleCode;
    }
}
