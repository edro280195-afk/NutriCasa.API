using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Infrastructure.BackgroundJobs;

public class MotivationPostJob
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<MotivationPostJob> _logger;

    private static readonly string[] MotivationalMessages =
    [
        "Recuerda que cada día es una oportunidad para elegir salud. ¡Tú puedes! 💪",
        "El keto no es una dieta, es un estilo de vida. Celebra cada pequeño logro 🌱",
        "Hidrátate bien hoy. El agua es tu mejor aliada en cetosis 💧",
        "¿Ya hiciste tu check-in hoy? La consistencia construye hábitos imbatibles 🔥",
        "Tip del día: prepara tus alimentos con anticipación para no salirte del plan 📋",
        "La proteína es clave para mantener tu masa muscular. ¡No la descuides! 🥩",
        "Duerme bien esta noche. El descanso de calidad potencia los resultados del keto 😴",
        "Confía en el proceso. Los cambios sostenibles toman tiempo, pero valen la pena ⏳",
        "Celebra tus avances, por pequeños que sean. Cada kilo cuenta 🎯",
        "¿Sabías que el keto puede mejorar tu claridad mental? Nota la diferencia hoy 🧠",
        "No te compares con otros. Tu viaje keto es único y válido 🌟",
        "Un día fuera del plan no arruina tu progreso. Solo vuelve a empezar mañana 🔄",
        "Incluye suficientes electrolitos: sodio, potasio y magnesio. Tu cuerpo te lo agradecerá ⚡",
        "Comparte tus recetas favoritas con tu grupo. Inspirar a otros también te motiva 👨‍👩‍👧‍👦",
    ];

    private static readonly Random _random = new();

    public MotivationPostJob(
        IApplicationDbContext context,
        ILogger<MotivationPostJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Obtener grupos activos (con miembros activos)
        var activeGroupIds = await _context.GroupMemberships
            .Where(m => m.LeftAt == null)
            .Select(m => m.GroupId)
            .Distinct()
            .ToListAsync(ct);

        int postCount = 0;

        foreach (var groupId in activeGroupIds)
        {
            // Verificar si ya se publicó un post de motivación hoy en este grupo
            var alreadyPosted = await _context.GroupPosts
                .AnyAsync(p => p.GroupId == groupId
                            && p.PostType == PostType.AiMotivation
                            && p.CreatedAt.Date == DateTime.UtcNow.Date, ct);

            if (alreadyPosted) continue;

            var message = MotivationalMessages[_random.Next(MotivationalMessages.Length)];

            var post = new GroupPost
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                AuthorUserId = null,
                PostType = PostType.AiMotivation,
                Content = message,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _context.GroupPosts.Add(post);
            postCount++;
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("MotivationPostJob: {Count} mensajes publicados en {Groups} grupos activos.", postCount, activeGroupIds.Count);
    }
}
