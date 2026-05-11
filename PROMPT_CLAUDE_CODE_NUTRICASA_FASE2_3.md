# Prompt Claude Code · NutriCasa · Fase 2 + 3 — Camino A→B

> **Proyecto:** NutriCasa · Nutrición adaptada a tu vida.
> **Backend:** `C:\Codigos\NutriCasa\` → https://github.com/edro280195-afk/NutriCasa.API.git
> **Frontend:** `C:\Codigos\NutriCasa.App\` → https://github.com/edro280195-afk/NutriCasa.git
> **Estado actual:** Fase 0 + Fase 1 completas. Backend 30+ endpoints, 21/21 tests. Frontend Angular 21, 16 rutas al 93%. Ver sección de contexto para detalle.
>
> **Lee todo el documento antes de escribir una sola línea de código.**

---

## 📋 CONTEXTO DEL ESTADO ACTUAL

### Backend (NutriCasa.Api) — ya implementado
- Auth: register, verify-email, login, refresh, forgot-password, reset-password, logout, me
- Onboarding: 10 endpoints (steps 1-7 + status + override médico)
- Plans: `POST /api/plans/generate`, `GET /api/plans/current`
- CheckIns: `POST /api/checkins`
- Measurements: `POST /api/measurements`
- Progress: summary, weight-history, checkins, macros/weekly
- Family: members, feed, stats
- 8 controllers, 21/21 tests pasando, 0 warnings

### Frontend (Angular 21) — ya implementado
- 16 rutas: auth (5), onboarding wizard, dashboard, plan, family, progress, profile (5), legal (2)
- Servicios: ApiService, AuthService, OnboardingService, PlanService, FamilyService, ProgressService
- Guards: authGuard, loginGuard, onboardingGuard, dashboardGuard
- Interceptor: authInterceptor con refresh automático en 401
- PWA completo, 4 Lotties, design system en styles.scss
- Bottom nav flotante pill verde
- **Pendientes:** `shared/components/` vacío, 3 sub-páginas perfil "próximamente", tests mínimos

### Stubs pendientes (NO tocar aún)
- `CloudflareR2StorageService` — Fase 3 real
- `ModerationServiceStub` — Fase 4
- `MercadoPagoServiceStub` — Fase 8

---

## 🎯 ALCANCE DE ESTE SPRINT (en orden estricto de prioridad)

### BLOQUE 1 — Sprint 1.5: Catálogo curated de recetas
### BLOQUE 2 — Angular shared components
### BLOQUE 3 — Sub-páginas de perfil pendientes
### BLOQUE 4 — Tests mínimos de frontend
### BLOQUE 5 — Fase 2: Adherencia + Editor drag-and-drop del plan
### BLOQUE 6 — Fase 2: Gráficas de progreso en frontend
### BLOQUE 7 — Fase 3: Muro social con SignalR
### BLOQUE 8 — Fase 3: Leaderboard grupal

**Trabaja en este orden. No saltes bloques. Si terminas antes, reporta y espera confirmación.**

---

## 🍽️ BLOQUE 1 — Sprint 1.5: Catálogo curated de recetas

### Objetivo
Generar ~90 recetas curated (15 por modo × 6 modos) y poblarlas en la tabla `recipes` con `source = 'curated'`. Son el fallback cuando Gemini falla. Sin esto el producto no tiene red de seguridad.

### Crear script de generación

Crea `src/NutriCasa.Api/Scripts/SeedCuratedRecipes.cs` — un endpoint temporal solo en Development:

```
POST /api/admin/seed-curated-recipes
```

Requiere header `X-Admin-Key: nutricasa-admin-seed-2026` (hardcoded, solo dev).

**Algoritmo del endpoint:**

```
Para cada modo en ['economic', 'pantry_basic', 'simple_kitchen', 'busy_parent', 'athletic', 'gourmet']:
  Para cada tipo de comida en ['breakfast', 'lunch', 'dinner'] (5 de cada una) + ['snack'] (0):
    Total: 15 recetas por modo
  
  Construir prompt para Gemini Pro:
    "Genera 15 recetas keto para el modo {modo}. 
     5 desayunos, 5 comidas, 5 cenas.
     Restricciones del modo: {rules del budget_mode desde BD}
     
     Para CADA receta devuelve JSON con este esquema exacto:
     {
       name: string (en español, máx 60 chars),
       description: string (1 oración, máx 120 chars),
       meal_type: 'breakfast'|'lunch'|'dinner',
       nutrition_track: 'keto',
       ingredients: [{ name, amount_gr, kcal, protein_gr, fat_gr, carbs_gr, category }],
       instructions: string (pasos numerados, máx 400 chars),
       prep_time_min: number,
       cook_time_min: number,
       servings: 1,
       base_calories: number,
       base_protein_gr: number,
       base_fat_gr: number,
       base_carbs_gr: number,
       base_net_carbs_gr: number,
       difficulty_level: 1|2|3|4|5,
       tags: string[],
       estimated_cost_per_serving_mxn: number,
       compatible_mode_codes: string[],
       economic_tier: 1|2|3|4|5,
       is_batch_cookable: boolean,
       is_freezable: boolean,
       cooking_methods: string[]
     }
     
     Devuelve SOLO un array JSON con las 15 recetas. Sin texto adicional."
```

**Validador automático por receta:**

```
✅ base_net_carbs_gr < 12 (keto)
✅ base_calories BETWEEN 200 AND 800
✅ base_protein_gr >= 15
✅ base_fat_gr / base_calories >= 0.55 (mínimo 55% de calorías de grasa)
✅ ingredients.length >= 3 AND <= 12
✅ prep_time_min + cook_time_min <= límite del modo
   (economic/basic/simple: 60min, busy_parent: 120min, athletic/gourmet: 90min)
✅ instructions.length > 20
✅ nombre NO contiene ingredientes premium si modo es 'economic' o 'simple_kitchen'
```

**Flujo del endpoint:**
1. Para cada modo, llamar a Gemini Pro
2. Parsear las 15 recetas
3. Validar cada una con el validador
4. Las que pasan → INSERT en `recipes` con `source='curated'`, `is_public=true`, `nutrition_track='keto'`
5. Las que fallan → logear con razón, continuar (no abortar)
6. Al final devolver: `{ total_generated, total_saved, total_failed, failures: [{name, reason}] }`

**Importante:**
- Registrar cada llamada a Gemini en `ai_interactions` con `interaction_type='curated_recipe_seed'`
- Si una receta ya existe (mismo `slug` calculado), skip (idempotente)
- Slug = nombre en snake_case + sufijo del modo: `huevos_rancheros_economic`
- Usar `IMemoryCache` para no duplicar si se llama el endpoint dos veces

### Crear AdminController

```csharp
[ApiController]
[Route("api/admin")]
public class AdminController : BaseApiController
{
    // Solo funciona en Development
    // Si no Development → 404
    
    [HttpPost("seed-curated-recipes")]
    public async Task<IActionResult> SeedCuratedRecipes(
        [FromHeader(Name = "X-Admin-Key")] string adminKey)
    {
        if (!_env.IsDevelopment()) return NotFound();
        if (adminKey != "nutricasa-admin-seed-2026") return Unauthorized();
        // ...
    }
}
```

### Verificación esperada

Después de correr el endpoint:
```sql
SELECT source, count(*) FROM recipes GROUP BY source;
-- curated: ~80-90 (esperamos 80-90% tasa de éxito)
SELECT meal_type, count(*) FROM recipes WHERE source='curated' GROUP BY meal_type;
-- breakfast: ~30, lunch: ~30, dinner: ~30
```

---

## 🧩 BLOQUE 2 — Angular shared components

Crea los siguientes componentes en `src/app/shared/components/`. Todos deben respetar el design system (variables CSS ya en `styles.scss`).

### 2.1 `NcCardComponent` — tarjeta base reutilizable

```typescript
// selector: nc-card
// Inputs: title?, subtitle?, icon?, padding? ('sm'|'md'|'lg'), variant? ('default'|'pine'|'mint')
// ng-content para el body
```

UI: card con `border: 1.5px solid var(--line)`, `border-radius: var(--r-lg)`, `background: var(--paper)`. Variante 'pine' tiene fondo `var(--pine)` y texto `var(--cream)`. Variante 'mint' tiene `var(--mint-soft)`.

### 2.2 `NcLoadingComponent` — spinner y skeleton

```typescript
// selector: nc-loading
// Input: type: 'spinner'|'skeleton'|'dots'
// Input: lines?: number (para skeleton, default 3)
```

Spinner: círculo de 32px con border `var(--mint)` animado (CSS spin). Skeleton: rectángulos con shimmer animation en `var(--cream-warm)`. Dots: tres puntos pulsando con `var(--mint)`.

### 2.3 `NcEmptyStateComponent` — estado vacío

```typescript
// selector: nc-empty-state
// Inputs: title, subtitle?, icon? (SVG string), actionLabel?, actionFn?
```

UI: centered, icono 48px en `var(--mint-soft)`, título en Fraunces, subtítulo en ink-light, botón CTA opcional.

### 2.4 `NcMacroBarComponent` — barra de macros

```typescript
// selector: nc-macro-bar
// Inputs: label, value, target, unit, color ('mint'|'lake'|'coral')
```

UI: label arriba, número grande en Fraunces, barra de progreso con fill del color indicado. Muestra porcentaje de cumplimiento.

### 2.5 `NcBadgeComponent` — etiqueta de estado

```typescript
// selector: nc-badge
// Inputs: label, variant ('success'|'warning'|'error'|'info'|'neutral')
```

UI: pill pequeño (font-size 11px, padding 4px 10px). Colores: success=mint-soft/pine, warning=cream-warm/gold, error=coral-bg/coral, info=lake-light/lake.

### 2.6 `NcAvatarComponent` — avatar con fallback

```typescript
// selector: nc-avatar
// Inputs: name, photoUrl?, size ('sm'|'md'|'lg'), showBadge?, badgeColor?
```

UI: Si tiene photoUrl → imagen circular. Si no → círculo con gradiente mint→lake y primera letra del nombre en blanco. Badge opcional (dot de color en esquina).

### 2.7 `NcToastService` — notificaciones toast

Servicio global (providedIn: 'root') con método `show(message, type: 'success'|'error'|'info'|'warning', duration=3000)`. Componente `NcToastComponent` en el root que renderiza los toasts en la esquina superior derecha. Animación slide-down + fade-out.

### Exportar todo desde `shared/index.ts`

```typescript
export * from './components/nc-card/nc-card.component';
// ... todos los demás
export * from './services/nc-toast.service';
```

### Integrar en las páginas existentes

Reemplaza los loading states, cards y empty states inline que ya existen en las páginas por los nuevos shared components. No reescribir las páginas completas — solo hacer los reemplazos donde aplique.

---

## 👤 BLOQUE 3 — Sub-páginas de perfil pendientes

Las 3 rutas están declaradas apuntando a `ComingSoonPage`. Reemplazar con implementación real.

### 3.1 `/profile/medical` — Perfil médico

Muestra el `medical_profile` del usuario con opción de editar.

**Backend necesario:**
```
GET  /api/profile/medical   → retorna medical_profile del usuario autenticado
PUT  /api/profile/medical   → actualiza medical_profile (sin cambiar override_accepted_at)
```

**UI:**
- Lista de condiciones médicas con checkboxes (read-only por default, botón "Editar" para activar edición)
- Tags de alergias (editable con tag input igual al del wizard)
- Restricciones dietéticas (chips seleccionables)
- Si `requires_human_review = true` y `override_accepted_at IS NULL`: banner coral explicando el bloqueo
- Si `override_accepted_at NOT NULL`: banner amber "Plan generado con override médico"
- Nivel de experiencia keto (3 cards con emojis 🌱🌿🌳)
- Botón guardar que llama al PUT

### 3.2 `/profile/preferences` — Preferencias

**Backend necesario:**
```
GET  /api/profile/preferences   → retorna privacy_settings + budget_mode_id + timezone
PUT  /api/profile/preferences   → actualiza los campos anteriores
```

**UI:**
- Selector de modo de presupuesto (mismas 6 cards del wizard, con el actual seleccionado)
  - Al cambiar modo → mostrar modal de confirmación: "Cambiar tu modo afectará tu próximo plan. ¿Continuar?"
  - Si confirma → PUT + actualizar `users.budget_mode_changed_at`
- Privacidad (sección con toggles):
  - Compartir peso: privado / solo admin / mi grupo (radio buttons en pill)
  - Compartir body fat: idem
  - Compartir medidas: idem
  - Compartir fotos: idem
  - Compartir check-ins: idem
  - Permitir menciones de IA: toggle
- Quiet hours: time picker (hora inicio, hora fin)
- Timezone: select con las zonas de México (America/Mexico_City, America/Monterrey, America/Tijuana, America/Cancun, America/Hermosillo, America/Chihuahua, America/Mazatlan, America/Merida)

### 3.3 `/profile/notifications` — Notificaciones

**Backend necesario:**
```
GET  /api/profile/notifications/settings    → retorna preferencias de notificación del privacy_settings
PUT  /api/profile/notifications/settings    → actualiza allow_push, allow_email, weekly_digest
GET  /api/notifications                     → lista de notificaciones del usuario (paginada, max 20)
PUT  /api/notifications/{id}/read           → marcar como leída
PUT  /api/notifications/read-all            → marcar todas como leídas
```

**UI:**
- Sección "Canales":
  - Toggle push notifications (activa/desactiva toda la PWA push — si desactiva, revoke push_subscriptions)
  - Toggle email notifications
  - Toggle resumen semanal
- Sección "Historial":
  - Lista de notificaciones recientes (max 20, con badge de no leída)
  - Icono según tipo (P0=rojo, P1=naranja, P2=mint, P3=lake, P4=gris)
  - Título + body + timestamp relativo ("hace 2 horas")
  - Tap en notificación → marcar leída + navegar a deep_link si existe
  - Botón "Marcar todo como leído"
  - Empty state si no hay notificaciones

---

## 🧪 BLOQUE 4 — Tests mínimos del frontend

Crea tests en estos archivos (los más críticos para la lógica de negocio):

### `auth.service.spec.ts`
```typescript
describe('AuthService', () => {
  it('debería guardar tokens al hacer login exitoso')
  it('debería limpiar tokens al hacer logout')
  it('debería retornar isAuthenticated=true cuando hay token válido')
  it('debería retornar isAuthenticated=false cuando no hay token')
  it('debería detectar onboardingComplete desde el JWT')
})
```

### `plan.service.spec.ts`
```typescript
describe('PlanService', () => {
  it('debería obtener el plan actual del API')
  it('debería retornar null si no hay plan activo')
  it('debería calcular correctamente el total de calorías del día')
})
```

### `onboarding.service.spec.ts`
```typescript
describe('OnboardingService', () => {
  it('debería guardar el progreso en localStorage')
  it('debería recuperar el progreso de localStorage')
  it('debería limpiar localStorage al completar onboarding')
})
```

Usar `TestBed`, `HttpClientTestingModule`, `provideRouter([])`. Mocks con jasmine spies.

---

## 🖱️ BLOQUE 5 — Fase 2: Adherencia + Editor drag-and-drop

### 5.1 Backend: endpoint de adherencia

```
POST /api/plans/{planId}/meals/{planMealId}/log
```

**Request:**
```json
{ "status": "completed" | "partial" | "skipped" | "substituted", "substitutionNote": "string?" }
```

**Flujo:**
1. Verificar que el `plan_meal_id` pertenece a un plan activo del usuario autenticado
2. Verificar que `logged_for_date` corresponde a hoy o ayer
3. Upsert en `meal_logs` (si ya existe el log de hoy para esa comida, actualizar)
4. Respuesta: `{ logId, status, loggedAt }`

### 5.2 Backend: endpoints de drag-and-drop

```
PATCH /api/plans/{planId}/meals/{planMealId}/lock
PATCH /api/plans/{planId}/meals/{planMealId}/portion
POST  /api/plans/{planId}/meals/{planMealId}/swap
PATCH /api/plans/{planId}/meals/reorder
```

**PATCH /lock:**
```json
{ "isLocked": true | false }
```
Toggle lock de la comida. Verificar ownership del plan. Actualizar `weekly_plan_meals.is_locked`.

**PATCH /portion:**
```json
{ "portionMultiplier": 1.5 }
```
Validar BETWEEN 0.25 AND 3.0. Actualizar `portion_multiplier`. No hay costo de IA. Recalcular macros de la comida en la respuesta (base_* × multiplier).

**POST /swap:**
```json
{ "dayOfWeek": 3, "mealType": "lunch" }
```
Swap individual. Verificar `max_swaps_week` del plan de suscripción. Llamar a Gemini Flash con prompt de swap. Registrar en `ai_interactions`. Guardar nueva receta si no existe. Retornar la nueva receta completa.

**PATCH /reorder (move entre celdas):**
```json
{
  "moves": [
    { "planMealId": "uuid1", "newDayOfWeek": 2, "newMealType": "lunch", "rowVersion": 5 },
    { "planMealId": "uuid2", "newDayOfWeek": 1, "newMealType": "dinner", "rowVersion": 3 }
  ]
}
```
Optimistic concurrency: verificar `row_version` antes de actualizar. Si no coincide → `409 Conflict` con el estado actual. Actualizar `day_of_week`, `meal_type`, y actualizar `row_version + 1` de cada `weekly_plan_meals` afectado.

### 5.3 Frontend: editor drag-and-drop

En `/plan`, reemplazar la vista estática del plan semanal con el editor interactivo.

**Grid 7 días × 4 comidas:**

Cada celda muestra:
- Mini-thumbnail del color del tipo de comida (gradiente CSS)
- Nombre truncado de la receta (max 2 líneas)
- Kcal + g proteína en texto pequeño
- Ícono de candado si `is_locked = true`
- Badge de adherencia del día (completado/parcial/saltado) si ya fue registrada

**Interacciones a implementar con Angular CDK DragDrop:**

```typescript
// Eduardo tiene experiencia con esto desde Regi Bazar
import { DragDropModule, CdkDragDrop } from '@angular/cdk/drag-drop';

// Configurar connectTo entre todas las celdas del mismo día
// (las comidas son intercambiables entre los 4 slots del mismo día)
// La versión inicial solo permite mover dentro del mismo día
// Mover entre días → PATCH /reorder
```

**Panel lateral al seleccionar comida (drawer):**
- Nombre completo de la receta en Fraunces
- Macros breakdown (proteína, grasa, carbs, kcal)
- Instrucciones
- Slider de porción (0.25x → 3x con steps de 0.25) → `PATCH /portion`
- Toggle de candado → `PATCH /lock`
- Botón "Cambiar esta comida" → `POST /swap` con spinner Lottie de cooking
- Botón "Registrar" → log de adherencia con selector completed/partial/skipped

**Barra de estado de macros del día (informativa, no bloqueante):**
- Verde si macros del día están ±15% del target
- Amarillo si ±25%
- Rojo si más de ±25%

**Optimistic updates:**
Actualizar la UI inmediatamente al arrastrar. Si el API retorna 409 → revertir con toast de error: "Este plan fue editado desde otro dispositivo. Recargando..."

---

## 📈 BLOQUE 6 — Fase 2: Gráficas de progreso

En la página `/progress`, reemplazar los placeholders con gráficas reales usando **CSS + SVG puro** (sin librerías externas de charts por ahora — ECharts se integrará en Fase 5 con el dashboard avanzado).

### 6.1 Gráfica de peso (línea SVG)

```typescript
// Componente: WeightChartComponent
// Input: dataPoints: Array<{ date: string, weightKg: number }>
// Input: targetWeightKg: number
```

SVG con:
- Línea de datos reales (color `var(--pine)`, 2px)
- Línea de meta (color `var(--mint)`, 1.5px dashed)
- Puntos en cada medición (círculo 6px relleno `var(--pine)`)
- Eje X: fechas abreviadas (últimas 8 semanas)
- Eje Y: rango entre min_peso-2 y max_peso+2
- Tooltip en hover (posición absoluta con el valor exacto)
- Empty state si menos de 2 puntos

### 6.2 Heatmap de check-ins (grid de días)

```typescript
// Componente: CheckinHeatmapComponent
// Input: checkins: Array<{ date: string, completed: boolean }>
// Muestra últimas 12 semanas (84 días)
```

Grid 12 columnas × 7 filas (semanas × días). Cada celda:
- Check-in completado: `var(--mint)` con opacidad según dificultad promedio
- Sin check-in: `var(--line)`
- Hoy: borde `var(--pine)`

### 6.3 Barras de macros semanales

```typescript
// Componente: WeeklyMacrosComponent
// Input: weekData: Array<{ dayName: string, protein, fat, carbs, calories }>
```

Barras agrupadas por día (7 barras de lunes a domingo). Stack vertical: carbs/proteína/grasa. CSS puro con flex y `height` calculado como `(value/target) * 100%`.

### 6.4 Ring de adherencia

```typescript
// Componente: AdherenceRingComponent
// Input: completedMeals, totalMeals
```

SVG circular (conic-gradient no funciona bien en SVG — usar `stroke-dasharray` + `stroke-dashoffset`). Porcentaje grande en el centro en Fraunces. Color: verde si ≥80%, amarillo si ≥50%, coral si <50%.

---

## 💬 BLOQUE 7 — Fase 3: Muro social con SignalR

### 7.1 Backend: Hub de grupos

Instalar en Infrastructure:
```bash
dotnet add src/NutriCasa.Infrastructure package Microsoft.AspNetCore.SignalR.Core
```

Crear `src/NutriCasa.Api/Hubs/GroupHub.cs`:

```csharp
[Authorize]
public class GroupHub : Hub
{
    // Métodos del servidor que el cliente puede llamar:
    
    // Unirse al grupo (auto-invocado al conectar)
    public async Task JoinGroup(string groupId) { ... }
    
    // El cliente envía un post nuevo
    public async Task SendPost(string groupId, string content, string postType) { ... }
    
    // El cliente envía una reacción
    public async Task SendReaction(string postId, string reactionType) { ... }
    
    // Métodos que el servidor invoca en los clientes:
    // "ReceivePost" — nuevo post en tiempo real
    // "ReceiveReaction" — nueva reacción
    // "MemberOnline" — miembro se conectó
    // "MemberOffline" — miembro se desconectó
}
```

**Flujo de `SendPost`:**
1. Verificar que el userId del JWT sea miembro del `groupId`
2. Validar content con `ToxicWordService` (capa 1 sincrónica)
3. Si pasa, guardar en `group_posts`
4. `Clients.Group(groupId).SendAsync("ReceivePost", postDto)`
5. Si content.length > 200 → encolar en Hangfire para moderación Gemini (background)

Registrar el hub en `Program.cs`:
```csharp
app.MapHub<GroupHub>("/hubs/group");
```

CORS ya configurado para nutricasa.app debe incluir WebSockets.

### 7.2 Backend: endpoints REST del muro

```
GET  /api/groups/{groupId}/posts?page=1&pageSize=20   → paginado, ordenado por fecha desc
POST /api/groups/{groupId}/posts                       → crear post (también vía SignalR, este es el fallback REST)
POST /api/groups/{groupId}/posts/{postId}/react        → { reactionType: "like"|"fire"|... }
DELETE /api/groups/{groupId}/posts/{postId}/react/{type} → quitar reacción
POST /api/groups/{groupId}/posts/{postId}/comments     → { content: string }
GET  /api/groups/{groupId}/posts/{postId}/comments     → paginado
DELETE /api/groups/{groupId}/posts/{postId}            → soft delete (solo author o admin)
```

**Reglas del muro:**
- Rate limit: max 10 posts/usuario/hora (verificar contra `system_thresholds`)
- Comentarios: max 30/usuario/hora
- Content máximo: 2000 chars para posts, 500 para comentarios
- Posts borrados muestran `content = null` con flag `isDeleted = true` (soft delete respetado)
- Privacy: si `author.privacy_settings.allow_ai_mentions = false` → no incluir el nombre en posts de tipo 'ai_motivation'

### 7.3 Frontend: integración SignalR

Instalar:
```bash
npm install @microsoft/signalr
```

Crear `src/app/core/services/group-hub.service.ts`:

```typescript
import * as signalR from '@microsoft/signalr';

@Injectable({ providedIn: 'root' })
export class GroupHubService {
  private connection: signalR.HubConnection | null = null;
  
  // Signals para el estado
  posts = signal<GroupPost[]>([]);
  isConnected = signal<boolean>(false);
  
  async connect(groupId: string, token: string) { ... }
  async disconnect() { ... }
  async sendPost(content: string, postType: string) { ... }
  async sendReaction(postId: string, reactionType: string) { ... }
}
```

Configuración de conexión:
```typescript
this.connection = new signalR.HubConnectionBuilder()
  .withUrl(`${environment.apiUrl}/hubs/group`, {
    accessTokenFactory: () => this.authService.getToken()
  })
  .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
  .build();

// Handlers
this.connection.on('ReceivePost', (post: GroupPost) => {
  this.posts.update(current => [post, ...current]);
});

this.connection.on('ReceiveReaction', (postId: string, reaction: Reaction) => {
  // Actualizar el post en el array de signals
});
```

### 7.4 Frontend: FamilyPage actualizada

Reemplazar la FamilyPage estática con el muro en tiempo real.

**Layout:**
- Header con nombre del grupo + contador de miembros online (dots verdes)
- Feed de posts (infinite scroll)
- Cada post:
  - Avatar del autor (usar `NcAvatarComponent`)
  - Nombre + tiempo relativo
  - Contenido
  - Barra de reacciones (6 emojis) con contadores
  - Link "Ver comentarios" (expandible inline)
- Botón flotante "+" para crear post (abre modal con textarea + tipo)
- Tipos disponibles para usuarios: 'user_text', 'meal_share', 'progress_share'
- Posts de IA ('ai_motivation', 'milestone') tienen badge especial con ícono hoja NutriCasa

**Indicator de conexión:**
Badge en header: verde "En vivo" si SignalR está conectado, gris "Offline" si no.

**Composer de post:**
Modal con:
- Textarea (max 2000 chars con contador)
- Selector de tipo (text, compartir comida, compartir progreso)
- Botón enviar que llama a `GroupHubService.sendPost()`
- Mientras envía → deshabilitar botón con spinner

---

## 🏆 BLOQUE 8 — Fase 3: Leaderboard grupal

### 8.1 Backend

```
GET /api/groups/{groupId}/leaderboard?metric=weight_loss|streak|adherence
```

**Cálculo por métrica:**

`weight_loss`: para cada miembro, calcular `(start_weight - current_weight) / start_weight * 100`. Respetar privacy_settings (si share_weight='private' → aparece como "Anónimo" con posición pero sin nombre ni foto).

`streak`: días consecutivos de check-in. Calcular desde `daily_check_ins` ordenado por fecha.

`adherence`: `completed_meals / total_planned_meals * 100` de las últimas 2 semanas desde `meal_logs`.

**Response:**
```json
{
  "metric": "weight_loss",
  "updatedAt": "...",
  "entries": [
    {
      "position": 1,
      "userId": "uuid | null (si private)",
      "displayName": "Eduardo P. | Miembro anónimo",
      "avatarUrl": "url | null",
      "value": 4.5,
      "unit": "% perdido | días | %",
      "isCurrentUser": true,
      "trend": "up | down | same"
    }
  ]
}
```

### 8.2 Frontend: LeaderboardComponent

En `FamilyPage` o nueva tab `/family/leaderboard`:

- 3 tabs: Pérdida de peso | Racha | Adherencia
- Lista ordenada con posiciones (1er lugar con icono 🥇, 2do 🥈, 3ro 🥉)
- Fila del usuario actual resaltada con `var(--mint-soft)`
- Avatar + nombre + valor + trend arrow
- Si `isAnonymous`: silueta gris + "Miembro del grupo"
- Loading skeleton mientras carga (usar `NcLoadingComponent`)
- Refresh automático cada 5 minutos (polling simple, sin SignalR por ahora)

---

## ⚙️ CONVENCIONES OBLIGATORIAS (recordatorio)

- **Código:** variables/métodos/clases en inglés, comentarios y mensajes UI en español MX
- **Angular:** control flow `@if`, `@for` (no *ngIf, *ngFor), Signals (no NgRx)
- **CSS:** variables del design system, sin Tailwind
- **Commits:** español natural, un commit por bloque: `feat: catálogo de recetas curated`, `feat: shared components del design system`, etc.
- **Sin code-skeletons:** código completo y funcional

---

## 🚫 LO QUE NO DEBES IMPLEMENTAR EN ESTE SPRINT

- MercadoPago (Fase 8)
- Cloudflare R2 real (sigue como stub — no hay fotos de progreso todavía)
- Milestones automáticos y jobs de Hangfire (Fase 4)
- Chat Q&A con IA (Fase 6)
- Reconocimiento de comida por foto (Fase 8)
- Análisis dominical automático (Fase 5)

---

## ✅ PASOS FINALES Y VERIFICACIÓN

Al terminar cada bloque, verificar:

**Backend (después de Bloque 1, 5 y 7-8):**
```bash
dotnet build  # debe dar 0 errores, 0 warnings
dotnet test   # debe dar todos los tests pasando (agregar los nuevos)
```

**Frontend (después de Bloque 2-4 y 5-8):**
```bash
ng build --configuration=production  # debe compilar sin errores
ng test --watch=false                # todos los tests del Bloque 4 pasando
```

**Commit por bloque completado:**
```
feat: sprint 1.5 — catálogo curated de 90 recetas keto por modo
feat: shared components del design system (NcCard, NcLoading, NcEmpty, NcMacroBar, NcBadge, NcAvatar, NcToast)
feat: sub-páginas de perfil — médico, preferencias y notificaciones
test: tests mínimos de servicios (auth, plan, onboarding)
feat: fase 2 — adherencia de comidas y editor drag-and-drop del plan
feat: fase 2 — gráficas de progreso (peso, check-ins, macros, adherencia)
feat: fase 3 — muro grupal con SignalR en tiempo real
feat: fase 3 — leaderboard grupal por peso, racha y adherencia
```

---

## 📝 REPORTE FINAL ESPERADO

Cuando termines, reporta:

1. **Bloque por bloque:** ✅/❌ + archivos creados/modificados
2. **Backend:** `dotnet build` + `dotnet test` con conteos
3. **Frontend:** `ng build` exitoso + tests pasando
4. **Endpoints nuevos** disponibles en Swagger
5. **Recetas curated generadas:** total_saved, total_failed, desglose por modo
6. **Decisiones en zona gris** que tomaste sin instrucción explícita
7. **Algo que no pudiste implementar** y por qué

NutriCasa · Nutrición adaptada a tu vida · nutricasa.app 🌿
