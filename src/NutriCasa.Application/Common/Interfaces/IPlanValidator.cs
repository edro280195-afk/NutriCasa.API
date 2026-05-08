using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Common.Interfaces;

/// <summary>
/// Valida la respuesta de Gemini contra todas las reglas de negocio del dominio.
/// Síncrono — no hace IO, trabaja con el plan ya deserializado.
/// </summary>
public interface IPlanValidator
{
    PlanValidationResult Validate(GeneratePlanResponse plan, PlanValidationContext context);
}
