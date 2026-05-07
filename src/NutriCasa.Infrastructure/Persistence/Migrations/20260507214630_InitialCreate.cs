using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriCasa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"pgcrypto\";");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"citext\";");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"pg_trgm\";");

            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION trg_set_updated_at()
                RETURNS trigger AS $$
                BEGIN
                    NEW.updated_at = NOW();
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION trg_check_group_depth()
                RETURNS trigger AS $$
                DECLARE
                    depth INT := 0;
                    current_parent UUID := NEW.parent_group_id;
                BEGIN
                    WHILE current_parent IS NOT NULL AND depth < 5 LOOP
                        SELECT parent_group_id INTO current_parent FROM groups WHERE group_id = current_parent;
                        depth := depth + 1;
                    END LOOP;
                    IF depth >= 5 THEN
                        RAISE EXCEPTION 'Group hierarchy depth cannot exceed 5 levels.';
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");
            migrationBuilder.CreateTable(
                name: "budget_modes",
                columns: table => new
                {
                    mode_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    short_description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    long_description = table.Column<string>(type: "text", nullable: true),
                    icon_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    color_theme = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    estimated_cost_min_mxn = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    estimated_cost_max_mxn = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    rules = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budget_modes", x => x.mode_id);
                });

            migrationBuilder.CreateTable(
                name: "disclaimer_versions",
                columns: table => new
                {
                    version_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    disclaimer_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    version_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_disclaimer_versions", x => x.version_id);
                });

            migrationBuilder.CreateTable(
                name: "feature_flags",
                columns: table => new
                {
                    flag_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    rollout_percent = table.Column<int>(type: "integer", nullable: false),
                    target_user_ids = table.Column<Guid[]>(type: "uuid[]", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feature_flags", x => x.flag_id);
                });

            migrationBuilder.CreateTable(
                name: "store_categories",
                columns: table => new
                {
                    category_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    short_description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    icon_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    typical_price_factor = table.Column<decimal>(type: "numeric(4,2)", nullable: false, defaultValue: 1.0m),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    google_place_types = table.Column<string[]>(type: "text[]", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_store_categories", x => x.category_id);
                });

            migrationBuilder.CreateTable(
                name: "subscription_plans",
                columns: table => new
                {
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    price_monthly_mxn = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    price_yearly_mxn = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    trial_days = table.Column<int>(type: "integer", nullable: false),
                    max_group_members = table.Column<int>(type: "integer", nullable: true),
                    max_regenerations_week = table.Column<int>(type: "integer", nullable: true),
                    max_swaps_week = table.Column<int>(type: "integer", nullable: true),
                    max_chat_messages_month = table.Column<int>(type: "integer", nullable: true),
                    has_ai_chat = table.Column<bool>(type: "boolean", nullable: false),
                    has_photo_analysis = table.Column<bool>(type: "boolean", nullable: false),
                    has_advanced_analytics = table.Column<bool>(type: "boolean", nullable: false),
                    has_priority_support = table.Column<bool>(type: "boolean", nullable: false),
                    features = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_plans", x => x.plan_id);
                });

            migrationBuilder.CreateTable(
                name: "toxic_words",
                columns: table => new
                {
                    word_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    word = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_word = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    severity = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_regex = table.Column<bool>(type: "boolean", nullable: false),
                    pattern = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_toxic_words", x => x.word_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    full_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    height_cm = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    activity_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    body_type_selected = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    profile_photo_url = table.Column<string>(type: "text", nullable: true),
                    timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "America/Mexico_City"),
                    preferred_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "es-MX"),
                    nutrition_track = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Keto"),
                    is_minor = table.Column<bool>(type: "boolean", nullable: false),
                    tutor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tutor_consent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tutor_consent_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email_verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    email_verification_token = table.Column<string>(type: "text", nullable: true),
                    disclaimer_accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    disclaimer_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_login_attempts = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletion_requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletion_scheduled_for = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletion_cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    budget_mode_id = table.Column<Guid>(type: "uuid", nullable: true),
                    budget_mode_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_users_budget_modes_budget_mode_id",
                        column: x => x.budget_mode_id,
                        principalTable: "budget_modes",
                        principalColumn: "mode_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_users_disclaimer_versions_disclaimer_version_id",
                        column: x => x.disclaimer_version_id,
                        principalTable: "disclaimer_versions",
                        principalColumn: "version_id");
                    table.ForeignKey(
                        name: "fk_users_disclaimer_versions_tutor_consent_version_id",
                        column: x => x.tutor_consent_version_id,
                        principalTable: "disclaimer_versions",
                        principalColumn: "version_id");
                    table.ForeignKey(
                        name: "fk_users_users_tutor_user_id",
                        column: x => x.tutor_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ai_interactions",
                columns: table => new
                {
                    interaction_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    thread_id = table.Column<Guid>(type: "uuid", nullable: true),
                    interaction_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    prompt_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    model_used = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    input_tokens = table.Column<int>(type: "integer", nullable: true),
                    output_tokens = table.Column<int>(type: "integer", nullable: true),
                    estimated_cost_usd = table.Column<decimal>(type: "numeric(10,6)", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    prompt_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    request_payload = table.Column<string>(type: "jsonb", nullable: true),
                    response_payload = table.Column<string>(type: "jsonb", nullable: true),
                    cache_hit = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_interactions", x => x.interaction_id);
                    table.ForeignKey(
                        name: "fk_ai_interactions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    audit_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log", x => x.audit_id);
                    table.ForeignKey(
                        name: "fk_audit_log_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "body_measurements",
                columns: table => new
                {
                    measurement_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weight_kg = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    body_fat_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    waist_cm = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    hip_cm = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    neck_cm = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    arm_cm = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    chest_cm = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    thigh_cm = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    measured_at = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_body_measurements", x => x.measurement_id);
                    table.ForeignKey(
                        name: "fk_body_measurements_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "daily_check_ins",
                columns: table => new
                {
                    check_in_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_in_date = table.Column<DateOnly>(type: "date", nullable: false),
                    hunger_level = table.Column<int>(type: "integer", nullable: true),
                    energy_level = table.Column<int>(type: "integer", nullable: true),
                    mood_level = table.Column<int>(type: "integer", nullable: true),
                    difficulty_level = table.Column<int>(type: "integer", nullable: true),
                    sleep_hours = table.Column<decimal>(type: "numeric(3,1)", nullable: true),
                    water_liters = table.Column<decimal>(type: "numeric(3,1)", nullable: true),
                    ketones_mmol = table.Column<decimal>(type: "numeric(4,2)", nullable: true),
                    had_cheat_meal = table.Column<bool>(type: "boolean", nullable: false),
                    cheat_description = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_check_ins", x => x.check_in_id);
                    table.ForeignKey(
                        name: "fk_daily_check_ins_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    group_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    parent_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    invite_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    invite_code_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    group_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    color_theme = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_frozen = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_groups", x => x.group_id);
                    table.ForeignKey(
                        name: "fk_groups_groups_parent_group_id",
                        column: x => x.parent_group_id,
                        principalTable: "groups",
                        principalColumn: "group_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_groups_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "ingredient_catalog",
                columns: table => new
                {
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_search = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    kcal_per100g = table.Column<decimal>(type: "numeric(7,2)", nullable: true),
                    protein_g_per100g = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    fat_g_per100g = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    carbs_g_per100g = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    fiber_g_per100g = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    avg_price_mxn = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    avg_price_unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    unit_grams_equivalent = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    last_price_update = table.Column<DateOnly>(type: "date", nullable: true),
                    economic_tier = table.Column<int>(type: "integer", nullable: false),
                    tags = table.Column<string[]>(type: "text[]", nullable: false),
                    primary_store_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    secondary_store_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_seasonal = table.Column<bool>(type: "boolean", nullable: false),
                    seasonal_months = table.Column<int[]>(type: "integer[]", nullable: false),
                    is_keto_friendly = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingredient_catalog", x => x.ingredient_id);
                    table.ForeignKey(
                        name: "fk_ingredient_catalog_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "keto_profiles",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bmr_kcal = table.Column<int>(type: "integer", nullable: true),
                    tdee_kcal = table.Column<int>(type: "integer", nullable: true),
                    daily_calories = table.Column<int>(type: "integer", nullable: false),
                    carbs_grams = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    protein_grams = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    fat_grams = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    carbs_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    protein_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    fat_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    calculation_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    last_calculated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    target_weekly_cost_mxn = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    target_meal_cost_mxn = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_keto_profiles", x => x.profile_id);
                    table.ForeignKey(
                        name: "fk_keto_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "medical_profiles",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    has_diabetes = table.Column<bool>(type: "boolean", nullable: false),
                    diabetes_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_pregnant_or_lactating = table.Column<bool>(type: "boolean", nullable: false),
                    has_kidney_issues = table.Column<bool>(type: "boolean", nullable: false),
                    has_liver_issues = table.Column<bool>(type: "boolean", nullable: false),
                    has_pancreas_issues = table.Column<bool>(type: "boolean", nullable: false),
                    has_thyroid_issues = table.Column<bool>(type: "boolean", nullable: false),
                    has_heart_condition = table.Column<bool>(type: "boolean", nullable: false),
                    has_eating_disorder_history = table.Column<bool>(type: "boolean", nullable: false),
                    has_gallbladder_issues = table.Column<bool>(type: "boolean", nullable: false),
                    other_conditions = table.Column<string>(type: "text", nullable: true),
                    allergies = table.Column<string[]>(type: "text[]", nullable: false),
                    medications = table.Column<string[]>(type: "text[]", nullable: false),
                    dietary_restrictions = table.Column<string[]>(type: "text[]", nullable: false),
                    disliked_ingredients = table.Column<string[]>(type: "text[]", nullable: false),
                    preferred_ingredients = table.Column<string[]>(type: "text[]", nullable: false),
                    keto_experience_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requires_human_review = table.Column<bool>(type: "boolean", nullable: false),
                    human_review_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    human_review_notes = table.Column<string>(type: "text", nullable: true),
                    override_accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    override_disclaimer_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    override_revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medical_profiles", x => x.profile_id);
                    table.ForeignKey(
                        name: "fk_medical_profiles_disclaimer_versions_override_disclaimer_ve",
                        column: x => x.override_disclaimer_version_id,
                        principalTable: "disclaimer_versions",
                        principalColumn: "version_id");
                    table.ForeignKey(
                        name: "fk_medical_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    priority = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    deep_link = table.Column<string>(type: "text", nullable: true),
                    icon_url = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    delivery_channels = table.Column<string[]>(type: "text[]", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.notification_id);
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                columns: table => new
                {
                    token_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_password_reset_tokens", x => x.token_id);
                    table.ForeignKey(
                        name: "fk_password_reset_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "privacy_settings",
                columns: table => new
                {
                    settings_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    share_weight = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    share_body_fat = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    share_measurements = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    share_photos = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    share_check_ins = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    allow_ai_mentions = table.Column<bool>(type: "boolean", nullable: false),
                    allow_push = table.Column<bool>(type: "boolean", nullable: false),
                    allow_email = table.Column<bool>(type: "boolean", nullable: false),
                    weekly_digest = table.Column<bool>(type: "boolean", nullable: false),
                    quiet_hours_start = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    quiet_hours_end = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_privacy_settings", x => x.settings_id);
                    table.ForeignKey(
                        name: "fk_privacy_settings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "push_subscriptions",
                columns: table => new
                {
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint = table.Column<string>(type: "text", nullable: false),
                    p256dh_key = table.Column<string>(type: "text", nullable: false),
                    auth_key = table.Column<string>(type: "text", nullable: false),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_push_subscriptions", x => x.subscription_id);
                    table.ForeignKey(
                        name: "fk_push_subscriptions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipes",
                columns: table => new
                {
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    meal_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nutrition_track = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ingredients = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    instructions = table.Column<string>(type: "text", nullable: true),
                    prep_time_min = table.Column<int>(type: "integer", nullable: true),
                    cook_time_min = table.Column<int>(type: "integer", nullable: true),
                    servings = table.Column<int>(type: "integer", nullable: false),
                    base_calories = table.Column<int>(type: "integer", nullable: false),
                    base_protein_gr = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    base_fat_gr = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    base_carbs_gr = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    base_net_carbs_gr = table.Column<decimal>(type: "numeric(7,2)", nullable: true),
                    difficulty_level = table.Column<int>(type: "integer", nullable: true),
                    photo_url = table.Column<string>(type: "text", nullable: true),
                    tags = table.Column<string[]>(type: "text[]", nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ai_model = table.Column<string>(type: "text", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    use_count = table.Column<int>(type: "integer", nullable: false),
                    avg_rating = table.Column<decimal>(type: "numeric(3,2)", nullable: true),
                    rating_count = table.Column<int>(type: "integer", nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    compatible_mode_codes = table.Column<string[]>(type: "text[]", nullable: false),
                    economic_tier = table.Column<int>(type: "integer", nullable: true),
                    estimated_cost_per_serving_mxn = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    total_prep_time_min = table.Column<int>(type: "integer", nullable: true),
                    yield_servings_min = table.Column<int>(type: "integer", nullable: true),
                    yield_servings_max = table.Column<int>(type: "integer", nullable: true),
                    is_batch_cookable = table.Column<bool>(type: "boolean", nullable: false),
                    is_freezable = table.Column<bool>(type: "boolean", nullable: false),
                    cooking_methods = table.Column<string[]>(type: "text[]", nullable: false),
                    difficulty_score = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipes", x => x.recipe_id);
                    table.ForeignKey(
                        name: "fk_recipes_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    token_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.token_id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "system_thresholds",
                columns: table => new
                {
                    threshold_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    numeric_value = table.Column<decimal>(type: "numeric(12,4)", nullable: true),
                    text_value = table.Column<string>(type: "text", nullable: true),
                    unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_thresholds", x => x.threshold_id);
                    table.ForeignKey(
                        name: "fk_system_thresholds_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "tutor_relationships",
                columns: table => new
                {
                    relationship_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    minor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tutor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relationship_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    minor_id_document_url = table.Column<string>(type: "text", nullable: true),
                    minor_id_document_type = table.Column<string>(type: "text", nullable: true),
                    tutor_id_document_url = table.Column<string>(type: "text", nullable: true),
                    tutor_id_document_type = table.Column<string>(type: "text", nullable: true),
                    parentage_proof_url = table.Column<string>(type: "text", nullable: true),
                    verification_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    consent_signed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    consent_revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tutor_relationships", x => x.relationship_id);
                    table.ForeignKey(
                        name: "fk_tutor_relationships_users_minor_user_id",
                        column: x => x.minor_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tutor_relationships_users_tutor_user_id",
                        column: x => x.tutor_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tutor_relationships_users_verified_by_user_id",
                        column: x => x.verified_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "user_goals",
                columns: table => new
                {
                    goal_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goal_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    start_weight_kg = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    target_weight_kg = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    target_date = table.Column<DateOnly>(type: "date", nullable: true),
                    motivation_text = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    achieved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    abandoned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_goals", x => x.goal_id);
                    table.ForeignKey(
                        name: "fk_user_goals_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_milestones",
                columns: table => new
                {
                    milestone_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    milestone_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    milestone_value = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    achieved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    posted_to_group = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_milestones", x => x.milestone_id);
                    table.ForeignKey(
                        name: "fk_user_milestones_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_preferred_stores",
                columns: table => new
                {
                    preferred_store_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    custom_store_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    google_place_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    visit_frequency = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_preferred_stores", x => x.preferred_store_id);
                    table.ForeignKey(
                        name: "fk_user_preferred_stores_store_categories_store_category_id",
                        column: x => x.store_category_id,
                        principalTable: "store_categories",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_preferred_stores_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_subscriptions",
                columns: table => new
                {
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    current_period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    current_period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancel_at_period_end = table.Column<bool>(type: "boolean", nullable: false),
                    payment_provider = table.Column<string>(type: "text", nullable: true),
                    provider_subscription_id = table.Column<string>(type: "text", nullable: true),
                    provider_customer_id = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_subscriptions", x => x.subscription_id);
                    table.ForeignKey(
                        name: "fk_user_subscriptions_subscription_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "subscription_plans",
                        principalColumn: "plan_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_subscriptions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_trials_used",
                columns: table => new
                {
                    trial_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trial_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    trial_ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    converted_to_paid = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_trials_used", x => x.trial_id);
                    table.ForeignKey(
                        name: "fk_user_trials_used_subscription_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "subscription_plans",
                        principalColumn: "plan_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_trials_used_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weekly_summaries",
                columns: table => new
                {
                    summary_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    week_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    week_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    avg_difficulty = table.Column<decimal>(type: "numeric(3,2)", nullable: true),
                    avg_hunger = table.Column<decimal>(type: "numeric(3,2)", nullable: true),
                    avg_energy = table.Column<decimal>(type: "numeric(3,2)", nullable: true),
                    adherence_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    weight_change_kg = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    check_ins_count = table.Column<int>(type: "integer", nullable: true),
                    meals_completed = table.Column<int>(type: "integer", nullable: true),
                    meals_planned = table.Column<int>(type: "integer", nullable: true),
                    is_in_plateau = table.Column<bool>(type: "boolean", nullable: false),
                    ai_recommendations = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weekly_summaries", x => x.summary_id);
                    table.ForeignKey(
                        name: "fk_weekly_summaries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "progress_photos",
                columns: table => new
                {
                    photo_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    photo_url = table.Column<string>(type: "text", nullable: false),
                    storage_key = table.Column<string>(type: "text", nullable: false),
                    angle = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    visibility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    associated_measurement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    width_px = table.Column<int>(type: "integer", nullable: true),
                    height_px = table.Column<int>(type: "integer", nullable: true),
                    taken_at = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_progress_photos", x => x.photo_id);
                    table.ForeignKey(
                        name: "fk_progress_photos_body_measurements_associated_measurement_id",
                        column: x => x.associated_measurement_id,
                        principalTable: "body_measurements",
                        principalColumn: "measurement_id");
                    table.ForeignKey(
                        name: "fk_progress_photos_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "challenges",
                columns: table => new
                {
                    challenge_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    goal_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    goal_description = table.Column<string>(type: "text", nullable: true),
                    reward_description = table.Column<string>(type: "text", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_finalized = table.Column<bool>(type: "boolean", nullable: false),
                    finalized_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_challenges", x => x.challenge_id);
                    table.ForeignKey(
                        name: "fk_challenges_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "group_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_challenges_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "group_memberships",
                columns: table => new
                {
                    membership_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nickname = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    left_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_memberships", x => x.membership_id);
                    table.ForeignKey(
                        name: "fk_group_memberships_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "group_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_posts",
                columns: table => new
                {
                    post_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    post_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    is_announcement = table.Column<bool>(type: "boolean", nullable: false),
                    is_under_review = table.Column<bool>(type: "boolean", nullable: false),
                    moderation_reason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    moderation_severity = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    moderation_resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    moderation_action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    moderated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_posts", x => x.post_id);
                    table.ForeignKey(
                        name: "fk_group_posts_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "group_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_posts_users_author_user_id",
                        column: x => x.author_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "fk_group_posts_users_moderated_by_user_id",
                        column: x => x.moderated_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "shopping_lists",
                columns: table => new
                {
                    list_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    week_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    week_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_estimated_mxn = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    budget_mode_id = table.Column<Guid>(type: "uuid", nullable: true),
                    total_estimated_cost_mxn = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    cost_breakdown = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopping_lists", x => x.list_id);
                    table.ForeignKey(
                        name: "fk_shopping_lists_budget_modes_budget_mode_id",
                        column: x => x.budget_mode_id,
                        principalTable: "budget_modes",
                        principalColumn: "mode_id");
                    table.ForeignKey(
                        name: "fk_shopping_lists_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "group_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weekly_plans",
                columns: table => new
                {
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    nutrition_track = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_override_plan = table.Column<bool>(type: "boolean", nullable: false),
                    is_refeed_week = table.Column<bool>(type: "boolean", nullable: false),
                    original_menu_content = table.Column<string>(type: "jsonb", nullable: false),
                    current_menu_content = table.Column<string>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    generation_source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    parent_plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ai_interaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    budget_mode_id = table.Column<Guid>(type: "uuid", nullable: true),
                    estimated_total_cost_mxn = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    estimated_cost_per_person_mxn = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    estimated_cost_gourmet_baseline_mxn = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    savings_vs_gourmet_mxn = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    savings_vs_gourmet_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weekly_plans", x => x.plan_id);
                    table.ForeignKey(
                        name: "fk_weekly_plans_ai_interactions_ai_interaction_id",
                        column: x => x.ai_interaction_id,
                        principalTable: "ai_interactions",
                        principalColumn: "interaction_id");
                    table.ForeignKey(
                        name: "fk_weekly_plans_budget_modes_budget_mode_id",
                        column: x => x.budget_mode_id,
                        principalTable: "budget_modes",
                        principalColumn: "mode_id");
                    table.ForeignKey(
                        name: "fk_weekly_plans_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "group_id");
                    table.ForeignKey(
                        name: "fk_weekly_plans_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_weekly_plans_weekly_plans_parent_plan_id",
                        column: x => x.parent_plan_id,
                        principalTable: "weekly_plans",
                        principalColumn: "plan_id");
                });

            migrationBuilder.CreateTable(
                name: "ingredient_substitutions",
                columns: table => new
                {
                    substitution_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    original_ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    replacement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    applicable_mode_codes = table.Column<string[]>(type: "text[]", nullable: false),
                    reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    cost_savings_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    quality_loss_score = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingredient_substitutions", x => x.substitution_id);
                    table.ForeignKey(
                        name: "fk_ingredient_substitutions_ingredient_catalog_original_ingred",
                        column: x => x.original_ingredient_id,
                        principalTable: "ingredient_catalog",
                        principalColumn: "ingredient_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ingredient_substitutions_ingredient_catalog_replacement_id",
                        column: x => x.replacement_id,
                        principalTable: "ingredient_catalog",
                        principalColumn: "ingredient_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipe_ratings",
                columns: table => new
                {
                    rating_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_ratings", x => x.rating_id);
                    table.ForeignKey(
                        name: "fk_recipe_ratings_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "recipe_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_recipe_ratings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "challenge_participants",
                columns: table => new
                {
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    challenge_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sub_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    starting_value = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    current_value = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    final_score = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    final_position = table.Column<int>(type: "integer", nullable: true),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_challenge_participants", x => x.participant_id);
                    table.ForeignKey(
                        name: "fk_challenge_participants_challenges_challenge_id",
                        column: x => x.challenge_id,
                        principalTable: "challenges",
                        principalColumn: "challenge_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_challenge_participants_groups_sub_group_id",
                        column: x => x.sub_group_id,
                        principalTable: "groups",
                        principalColumn: "group_id");
                    table.ForeignKey(
                        name: "fk_challenge_participants_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_comments",
                columns: table => new
                {
                    comment_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    parent_comment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_comments", x => x.comment_id);
                    table.ForeignKey(
                        name: "fk_post_comments_group_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "group_posts",
                        principalColumn: "post_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_post_comments_post_comments_parent_comment_id",
                        column: x => x.parent_comment_id,
                        principalTable: "post_comments",
                        principalColumn: "comment_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_post_comments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_reactions",
                columns: table => new
                {
                    reaction_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reaction_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_reactions", x => x.reaction_id);
                    table.ForeignKey(
                        name: "fk_post_reactions_group_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "group_posts",
                        principalColumn: "post_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_post_reactions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shopping_list_items",
                columns: table => new
                {
                    item_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    shopping_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingredient_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    estimated_cost_mxn = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    is_purchased = table.Column<bool>(type: "boolean", nullable: false),
                    purchased_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purchased_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: true),
                    store_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    substituted_from_id = table.Column<Guid>(type: "uuid", nullable: true),
                    substitution_reason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopping_list_items", x => x.item_id);
                    table.ForeignKey(
                        name: "fk_shopping_list_items_ingredient_catalog_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredient_catalog",
                        principalColumn: "ingredient_id");
                    table.ForeignKey(
                        name: "fk_shopping_list_items_ingredient_catalog_substituted_from_id",
                        column: x => x.substituted_from_id,
                        principalTable: "ingredient_catalog",
                        principalColumn: "ingredient_id");
                    table.ForeignKey(
                        name: "fk_shopping_list_items_shopping_lists_shopping_list_id",
                        column: x => x.shopping_list_id,
                        principalTable: "shopping_lists",
                        principalColumn: "list_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shopping_list_items_store_categories_store_category_id",
                        column: x => x.store_category_id,
                        principalTable: "store_categories",
                        principalColumn: "category_id");
                    table.ForeignKey(
                        name: "fk_shopping_list_items_users_purchased_by_user_id",
                        column: x => x.purchased_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "weekly_plan_meals",
                columns: table => new
                {
                    meal_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    meal_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: true),
                    portion_multiplier = table.Column<decimal>(type: "numeric(4,2)", nullable: false, defaultValue: 1.0m),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    user_note = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weekly_plan_meals", x => x.meal_id);
                    table.ForeignKey(
                        name: "fk_weekly_plan_meals_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "recipe_id");
                    table.ForeignKey(
                        name: "fk_weekly_plan_meals_weekly_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "weekly_plans",
                        principalColumn: "plan_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "meal_logs",
                columns: table => new
                {
                    log_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_meal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    substitution_note = table.Column<string>(type: "text", nullable: true),
                    actual_portion = table.Column<decimal>(type: "numeric(4,2)", nullable: true),
                    logged_for_date = table.Column<DateOnly>(type: "date", nullable: false),
                    logged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meal_logs", x => x.log_id);
                    table.ForeignKey(
                        name: "fk_meal_logs_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "recipe_id");
                    table.ForeignKey(
                        name: "fk_meal_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_meal_logs_weekly_plan_meals_plan_meal_id",
                        column: x => x.plan_meal_id,
                        principalTable: "weekly_plan_meals",
                        principalColumn: "meal_id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_interactions_prompt_hash",
                table: "ai_interactions",
                column: "prompt_hash");

            migrationBuilder.CreateIndex(
                name: "ix_ai_interactions_user_id_created_at",
                table: "ai_interactions",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_entity_type_entity_id_created_at",
                table: "audit_log",
                columns: new[] { "entity_type", "entity_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_user_id_created_at",
                table: "audit_log",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_body_measurements_user_id_measured_at",
                table: "body_measurements",
                columns: new[] { "user_id", "measured_at" });

            migrationBuilder.CreateIndex(
                name: "ix_budget_modes_code",
                table: "budget_modes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_challenge_participants_challenge_id_user_id",
                table: "challenge_participants",
                columns: new[] { "challenge_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_challenge_participants_sub_group_id",
                table: "challenge_participants",
                column: "sub_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_challenge_participants_user_id",
                table: "challenge_participants",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_challenges_created_by_user_id",
                table: "challenges",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_challenges_group_id_is_active",
                table: "challenges",
                columns: new[] { "group_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_daily_check_ins_user_id_check_in_date",
                table: "daily_check_ins",
                columns: new[] { "user_id", "check_in_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_disclaimer_versions_disclaimer_type_is_current",
                table: "disclaimer_versions",
                columns: new[] { "disclaimer_type", "is_current" });

            migrationBuilder.CreateIndex(
                name: "ix_disclaimer_versions_disclaimer_type_version_code",
                table: "disclaimer_versions",
                columns: new[] { "disclaimer_type", "version_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_feature_flags_code",
                table: "feature_flags",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_group_memberships_group_id_user_id",
                table: "group_memberships",
                columns: new[] { "group_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_group_memberships_user_id",
                table: "group_memberships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_posts_author_user_id",
                table: "group_posts",
                column: "author_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_posts_group_id",
                table: "group_posts",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_posts_moderated_by_user_id",
                table: "group_posts",
                column: "moderated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_groups_created_by_user_id",
                table: "groups",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_groups_invite_code",
                table: "groups",
                column: "invite_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_groups_parent_group_id",
                table: "groups",
                column: "parent_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_ingredient_catalog_code",
                table: "ingredient_catalog",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ingredient_catalog_name_search",
                table: "ingredient_catalog",
                column: "name_search");

            migrationBuilder.CreateIndex(
                name: "ix_ingredient_catalog_updated_by_user_id",
                table: "ingredient_catalog",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_ingredient_substitutions_original_ingredient_id_replacement",
                table: "ingredient_substitutions",
                columns: new[] { "original_ingredient_id", "replacement_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ingredient_substitutions_replacement_id",
                table: "ingredient_substitutions",
                column: "replacement_id");

            migrationBuilder.CreateIndex(
                name: "ix_keto_profiles_user_id",
                table: "keto_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_meal_logs_plan_meal_id",
                table: "meal_logs",
                column: "plan_meal_id");

            migrationBuilder.CreateIndex(
                name: "ix_meal_logs_recipe_id",
                table: "meal_logs",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_meal_logs_user_id_logged_for_date",
                table: "meal_logs",
                columns: new[] { "user_id", "logged_for_date" });

            migrationBuilder.CreateIndex(
                name: "ix_medical_profiles_override_disclaimer_version_id",
                table: "medical_profiles",
                column: "override_disclaimer_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_medical_profiles_user_id",
                table: "medical_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_created_at",
                table: "notifications",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_tokens_user_id",
                table: "password_reset_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_comments_parent_comment_id",
                table: "post_comments",
                column: "parent_comment_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_comments_post_id",
                table: "post_comments",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_comments_user_id",
                table: "post_comments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_reactions_post_id_user_id_reaction_type",
                table: "post_reactions",
                columns: new[] { "post_id", "user_id", "reaction_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_post_reactions_user_id",
                table: "post_reactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_privacy_settings_user_id",
                table: "privacy_settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_progress_photos_associated_measurement_id",
                table: "progress_photos",
                column: "associated_measurement_id");

            migrationBuilder.CreateIndex(
                name: "ix_progress_photos_user_id_taken_at",
                table: "progress_photos",
                columns: new[] { "user_id", "taken_at" });

            migrationBuilder.CreateIndex(
                name: "ix_push_subscriptions_user_id_endpoint",
                table: "push_subscriptions",
                columns: new[] { "user_id", "endpoint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recipe_ratings_recipe_id_user_id",
                table: "recipe_ratings",
                columns: new[] { "recipe_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recipe_ratings_user_id",
                table: "recipe_ratings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipes_created_by_user_id",
                table: "recipes",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipes_slug",
                table: "recipes",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_items_ingredient_id",
                table: "shopping_list_items",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_items_purchased_by_user_id",
                table: "shopping_list_items",
                column: "purchased_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_items_shopping_list_id",
                table: "shopping_list_items",
                column: "shopping_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_items_store_category_id",
                table: "shopping_list_items",
                column: "store_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_items_substituted_from_id",
                table: "shopping_list_items",
                column: "substituted_from_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_lists_budget_mode_id",
                table: "shopping_lists",
                column: "budget_mode_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_lists_group_id_week_start_date",
                table: "shopping_lists",
                columns: new[] { "group_id", "week_start_date" });

            migrationBuilder.CreateIndex(
                name: "ix_store_categories_code",
                table: "store_categories",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subscription_plans_code",
                table: "subscription_plans",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_system_thresholds_category",
                table: "system_thresholds",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_system_thresholds_code",
                table: "system_thresholds",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_system_thresholds_updated_by_user_id",
                table: "system_thresholds",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_toxic_words_normalized_word",
                table: "toxic_words",
                column: "normalized_word");

            migrationBuilder.CreateIndex(
                name: "ix_tutor_relationships_minor_user_id_tutor_user_id",
                table: "tutor_relationships",
                columns: new[] { "minor_user_id", "tutor_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tutor_relationships_tutor_user_id",
                table: "tutor_relationships",
                column: "tutor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tutor_relationships_verified_by_user_id",
                table: "tutor_relationships",
                column: "verified_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_goals_user_id_is_active",
                table: "user_goals",
                columns: new[] { "user_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_user_milestones_user_id_milestone_type",
                table: "user_milestones",
                columns: new[] { "user_id", "milestone_type" });

            migrationBuilder.CreateIndex(
                name: "ix_user_preferred_stores_store_category_id",
                table: "user_preferred_stores",
                column: "store_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_preferred_stores_user_id_store_category_id",
                table: "user_preferred_stores",
                columns: new[] { "user_id", "store_category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_subscriptions_plan_id",
                table: "user_subscriptions",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_subscriptions_user_id_status",
                table: "user_subscriptions",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_user_trials_used_plan_id",
                table: "user_trials_used",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_trials_used_user_id_plan_id",
                table: "user_trials_used",
                columns: new[] { "user_id", "plan_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_budget_mode_id",
                table: "users",
                column: "budget_mode_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_disclaimer_version_id",
                table: "users",
                column: "disclaimer_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_tutor_consent_version_id",
                table: "users",
                column: "tutor_consent_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_tutor_user_id",
                table: "users",
                column: "tutor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_plan_meals_plan_id_day_of_week_meal_type_sort_order",
                table: "weekly_plan_meals",
                columns: new[] { "plan_id", "day_of_week", "meal_type", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_weekly_plan_meals_recipe_id",
                table: "weekly_plan_meals",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_plans_ai_interaction_id",
                table: "weekly_plans",
                column: "ai_interaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_plans_budget_mode_id",
                table: "weekly_plans",
                column: "budget_mode_id");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_plans_group_id",
                table: "weekly_plans",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_plans_parent_plan_id",
                table: "weekly_plans",
                column: "parent_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_plans_user_id_start_date",
                table: "weekly_plans",
                columns: new[] { "user_id", "start_date" });

            migrationBuilder.CreateIndex(
                name: "ix_weekly_summaries_user_id_week_start_date",
                table: "weekly_summaries",
                columns: new[] { "user_id", "week_start_date" },
                unique: true);
            // All tables that need `updated_at` triggers
            var tablesWithUpdatedAt = new[] {
                "subscription_plans", "feature_flags", "system_thresholds", "toxic_words",
                "users", "user_subscriptions", "privacy_settings", "medical_profiles",
                "keto_profiles", "tutor_relationships", "user_goals", "groups",
                "recipes", "weekly_plans", "weekly_plan_meals", "group_posts",
                "post_comments", "challenges", "budget_modes", "ingredient_catalog",
                "ingredient_substitutions", "store_categories", "user_preferred_stores"
            };

            foreach (var table in tablesWithUpdatedAt)
            {
                migrationBuilder.Sql($@"
                    CREATE TRIGGER set_updated_at_{table}
                    BEFORE UPDATE ON {table}
                    FOR EACH ROW
                    EXECUTE FUNCTION trg_set_updated_at();
                ");
            }

            migrationBuilder.Sql(@"
                CREATE TRIGGER check_group_depth
                BEFORE INSERT OR UPDATE ON groups
                FOR EACH ROW
                EXECUTE FUNCTION trg_check_group_depth();
            ");

            // Deferred FK (not standard in EF)
            migrationBuilder.Sql(@"
                ALTER TABLE weekly_plans DROP CONSTRAINT IF EXISTS fk_weekly_plans_ai_interactions_ai_interaction_id;
                ALTER TABLE weekly_plans ADD CONSTRAINT fk_weekly_plans_ai_interactions_ai_interaction_id 
                FOREIGN KEY (ai_interaction_id) REFERENCES ai_interactions(interaction_id) 
                DEFERRABLE INITIALLY DEFERRED;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "challenge_participants");

            migrationBuilder.DropTable(
                name: "daily_check_ins");

            migrationBuilder.DropTable(
                name: "feature_flags");

            migrationBuilder.DropTable(
                name: "group_memberships");

            migrationBuilder.DropTable(
                name: "ingredient_substitutions");

            migrationBuilder.DropTable(
                name: "keto_profiles");

            migrationBuilder.DropTable(
                name: "meal_logs");

            migrationBuilder.DropTable(
                name: "medical_profiles");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "password_reset_tokens");

            migrationBuilder.DropTable(
                name: "post_comments");

            migrationBuilder.DropTable(
                name: "post_reactions");

            migrationBuilder.DropTable(
                name: "privacy_settings");

            migrationBuilder.DropTable(
                name: "progress_photos");

            migrationBuilder.DropTable(
                name: "push_subscriptions");

            migrationBuilder.DropTable(
                name: "recipe_ratings");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "shopping_list_items");

            migrationBuilder.DropTable(
                name: "system_thresholds");

            migrationBuilder.DropTable(
                name: "toxic_words");

            migrationBuilder.DropTable(
                name: "tutor_relationships");

            migrationBuilder.DropTable(
                name: "user_goals");

            migrationBuilder.DropTable(
                name: "user_milestones");

            migrationBuilder.DropTable(
                name: "user_preferred_stores");

            migrationBuilder.DropTable(
                name: "user_subscriptions");

            migrationBuilder.DropTable(
                name: "user_trials_used");

            migrationBuilder.DropTable(
                name: "weekly_summaries");

            migrationBuilder.DropTable(
                name: "challenges");

            migrationBuilder.DropTable(
                name: "weekly_plan_meals");

            migrationBuilder.DropTable(
                name: "group_posts");

            migrationBuilder.DropTable(
                name: "body_measurements");

            migrationBuilder.DropTable(
                name: "ingredient_catalog");

            migrationBuilder.DropTable(
                name: "shopping_lists");

            migrationBuilder.DropTable(
                name: "store_categories");

            migrationBuilder.DropTable(
                name: "subscription_plans");

            migrationBuilder.DropTable(
                name: "recipes");

            migrationBuilder.DropTable(
                name: "weekly_plans");

            migrationBuilder.DropTable(
                name: "ai_interactions");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "budget_modes");

            migrationBuilder.DropTable(
                name: "disclaimer_versions");
        }
    }
}
