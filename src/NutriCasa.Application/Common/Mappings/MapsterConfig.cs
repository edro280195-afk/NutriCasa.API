using Mapster;

namespace NutriCasa.Application.Common.Mappings;

/// <summary>
/// Configuración global de Mapster. Se registra en DI al startup.
/// En Fase 0 está vacía. Se pobla en fases posteriores conforme se agreguen DTOs.
/// </summary>
public static class MapsterConfig
{
    public static void Configure()
    {
        // Fase 1+: aquí se registran los mapeos personalizados entre entidades y DTOs.
        // Ejemplo:
        // TypeAdapterConfig<User, UserDto>.NewConfig()
        //     .Map(dest => dest.Name, src => src.FullName);

        TypeAdapterConfig.GlobalSettings.Default
            .PreserveReference(true);
    }
}
