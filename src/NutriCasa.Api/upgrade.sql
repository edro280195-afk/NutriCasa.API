CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE EXTENSION IF NOT EXISTS "pgcrypto";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE EXTENSION IF NOT EXISTS "citext";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE EXTENSION IF NOT EXISTS "pg_trgm";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                    CREATE OR REPLACE FUNCTION trg_set_updated_at()
                    RETURNS trigger AS $$
                    BEGIN
                        NEW.updated_at = NOW();
                        RETURN NEW;
                    END;
                    $$ LANGUAGE plpgsql;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

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
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE budget_modes (
        mode_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        code character varying(50) NOT NULL,
        name character varying(100) NOT NULL,
        short_description character varying(200) NOT NULL,
        long_description text,
        icon_code character varying(30),
        color_theme character varying(7),
        sort_order integer NOT NULL,
        is_active boolean NOT NULL,
        estimated_cost_min_mxn numeric(10,2),
        estimated_cost_max_mxn numeric(10,2),
        rules jsonb NOT NULL DEFAULT ('{}'::jsonb),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_budget_modes PRIMARY KEY (mode_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE disclaimer_versions (
        version_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        disclaimer_type character varying(20) NOT NULL,
        version_code character varying(20) NOT NULL,
        title character varying(200) NOT NULL,
        content text NOT NULL,
        effective_from date NOT NULL,
        is_current boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_disclaimer_versions PRIMARY KEY (version_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE feature_flags (
        flag_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        code character varying(60) NOT NULL,
        description text,
        is_enabled boolean NOT NULL,
        rollout_percent integer NOT NULL,
        target_user_ids uuid[],
        metadata jsonb,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_feature_flags PRIMARY KEY (flag_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE store_categories (
        category_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        code character varying(50) NOT NULL,
        name character varying(100) NOT NULL,
        short_description character varying(200),
        icon_code character varying(30),
        typical_price_factor numeric(4,2) NOT NULL DEFAULT 1.0,
        sort_order integer NOT NULL,
        is_active boolean NOT NULL,
        google_place_types text[],
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_store_categories PRIMARY KEY (category_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE subscription_plans (
        plan_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        code character varying(30) NOT NULL,
        name character varying(50) NOT NULL,
        description text,
        price_monthly_mxn numeric(10,2) NOT NULL,
        price_yearly_mxn numeric(10,2),
        trial_days integer NOT NULL,
        max_group_members integer,
        max_regenerations_week integer,
        max_swaps_week integer,
        max_chat_messages_month integer,
        has_ai_chat boolean NOT NULL,
        has_photo_analysis boolean NOT NULL,
        has_advanced_analytics boolean NOT NULL,
        has_priority_support boolean NOT NULL,
        features jsonb NOT NULL DEFAULT ('{}'::jsonb),
        sort_order integer NOT NULL,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_subscription_plans PRIMARY KEY (plan_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE toxic_words (
        word_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        word character varying(100) NOT NULL,
        normalized_word character varying(100) NOT NULL,
        category character varying(30) NOT NULL,
        severity character varying(10) NOT NULL,
        language character varying(10) NOT NULL,
        is_regex boolean NOT NULL,
        pattern text,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_toxic_words PRIMARY KEY (word_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE users (
        user_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        full_name character varying(120) NOT NULL,
        email character varying(255) NOT NULL,
        password_hash character varying(255) NOT NULL,
        birth_date date NOT NULL,
        gender character varying(20) NOT NULL,
        height_cm numeric(5,2) NOT NULL,
        activity_level character varying(20) NOT NULL,
        body_type_selected character varying(20),
        profile_photo_url text,
        timezone character varying(50) NOT NULL DEFAULT 'America/Mexico_City',
        preferred_language character varying(10) NOT NULL DEFAULT 'es-MX',
        nutrition_track character varying(20) NOT NULL DEFAULT 'Keto',
        is_minor boolean NOT NULL,
        tutor_user_id uuid,
        tutor_consent_at timestamp with time zone,
        tutor_consent_version_id uuid,
        email_verified_at timestamp with time zone,
        email_verification_token text,
        disclaimer_accepted_at timestamp with time zone,
        disclaimer_version_id uuid,
        last_login_at timestamp with time zone,
        failed_login_attempts integer NOT NULL,
        locked_until timestamp with time zone,
        deletion_requested_at timestamp with time zone,
        deletion_scheduled_for timestamp with time zone,
        deletion_cancelled_at timestamp with time zone,
        budget_mode_id uuid,
        budget_mode_changed_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        deleted_at timestamp with time zone,
        CONSTRAINT pk_users PRIMARY KEY (user_id),
        CONSTRAINT fk_users_budget_modes_budget_mode_id FOREIGN KEY (budget_mode_id) REFERENCES budget_modes (mode_id) ON DELETE SET NULL,
        CONSTRAINT fk_users_disclaimer_versions_disclaimer_version_id FOREIGN KEY (disclaimer_version_id) REFERENCES disclaimer_versions (version_id),
        CONSTRAINT fk_users_disclaimer_versions_tutor_consent_version_id FOREIGN KEY (tutor_consent_version_id) REFERENCES disclaimer_versions (version_id),
        CONSTRAINT fk_users_users_tutor_user_id FOREIGN KEY (tutor_user_id) REFERENCES users (user_id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE ai_interactions (
        interaction_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid,
        thread_id uuid,
        interaction_type character varying(40) NOT NULL,
        prompt_version character varying(20) NOT NULL,
        model_used character varying(50) NOT NULL,
        input_tokens integer,
        output_tokens integer,
        estimated_cost_usd numeric(10,6),
        duration_ms integer,
        success boolean NOT NULL,
        error_message text,
        prompt_hash character varying(64),
        request_payload jsonb,
        response_payload jsonb,
        cache_hit boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_ai_interactions PRIMARY KEY (interaction_id),
        CONSTRAINT fk_ai_interactions_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE audit_log (
        audit_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid,
        entity_type character varying(50) NOT NULL,
        entity_id uuid NOT NULL,
        action character varying(40) NOT NULL,
        old_values jsonb,
        new_values jsonb,
        ip_address inet,
        user_agent text,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_audit_log PRIMARY KEY (audit_id),
        CONSTRAINT fk_audit_log_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE body_measurements (
        measurement_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid NOT NULL,
        weight_kg numeric(5,2) NOT NULL,
        body_fat_percentage numeric(5,2),
        waist_cm numeric(5,2),
        hip_cm numeric(5,2),
        neck_cm numeric(5,2),
        arm_cm numeric(5,2),
        chest_cm numeric(5,2),
        thigh_cm numeric(5,2),
        notes text,
        measured_at date NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_body_measurements PRIMARY KEY (measurement_id),
        CONSTRAINT fk_body_measurements_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE daily_check_ins (
        check_in_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid NOT NULL,
        check_in_date date NOT NULL,
        hunger_level integer,
        energy_level integer,
        mood_level integer,
        difficulty_level integer,
        sleep_hours numeric(3,1),
        water_liters numeric(3,1),
        ketones_mmol numeric(4,2),
        had_cheat_meal boolean NOT NULL,
        cheat_description text,
        notes text,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_daily_check_ins PRIMARY KEY (check_in_id),
        CONSTRAINT fk_daily_check_ins_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE groups (
        group_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        parent_group_id uuid,
        name character varying(100) NOT NULL,
        description text,
        invite_code character varying(20) NOT NULL,
        invite_code_expires_at timestamp with time zone,
        group_type character varying(20) NOT NULL,
        avatar_url text,
        color_theme character varying(7),
        created_by_user_id uuid,
        is_archived boolean NOT NULL,
        archived_at timestamp with time zone,
        is_frozen boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        deleted_at timestamp with time zone,
        CONSTRAINT pk_groups PRIMARY KEY (group_id),
        CONSTRAINT fk_groups_groups_parent_group_id FOREIGN KEY (parent_group_id) REFERENCES groups (group_id) ON DELETE RESTRICT,
        CONSTRAINT fk_groups_users_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (user_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE ingredient_catalog (
        ingredient_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        code character varying(60) NOT NULL,
        name character varying(100) NOT NULL,
        name_search character varying(100),
        category character varying(30) NOT NULL,
        kcal_per100g numeric(7,2),
        protein_g_per100g numeric(6,2),
        fat_g_per100g numeric(6,2),
        carbs_g_per100g numeric(6,2),
        fiber_g_per100g numeric(6,2),
        avg_price_mxn numeric(8,2),
        avg_price_unit character varying(20),
        unit_grams_equivalent numeric(8,2),
        last_price_update date,
        economic_tier integer NOT NULL,
        tags text[] NOT NULL,
        primary_store_category character varying(50),
        secondary_store_category character varying(50),
        is_seasonal boolean NOT NULL,
        seasonal_months integer[] NOT NULL,
        is_keto_friendly boolean NOT NULL,
        is_active boolean NOT NULL,
        updated_by_user_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_ingredient_catalog PRIMARY KEY (ingredient_id),
        CONSTRAINT fk_ingredient_catalog_users_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (user_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE keto_profiles (
        profile_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid NOT NULL,
        bmr_kcal integer,
        tdee_kcal integer,
        daily_calories integer NOT NULL,
        carbs_grams numeric(6,2) NOT NULL,
        protein_grams numeric(6,2) NOT NULL,
        fat_grams numeric(6,2) NOT NULL,
        carbs_percent numeric(5,2),
        protein_percent numeric(5,2),
        fat_percent numeric(5,2),
        calculation_method character varying(30) NOT NULL,
        last_calculated_at timestamp with time zone NOT NULL,
        target_weekly_cost_mxn numeric(10,2),
        target_meal_cost_mxn numeric(10,2),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_keto_profiles PRIMARY KEY (profile_id),
        CONSTRAINT fk_keto_profiles_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE medical_profiles (
        profile_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid NOT NULL,
        has_diabetes boolean NOT NULL,
        diabetes_type character varying(20),
        is_pregnant_or_lactating boolean NOT NULL,
        has_kidney_issues boolean NOT NULL,
        has_liver_issues boolean NOT NULL,
        has_pancreas_issues boolean NOT NULL,
        has_thyroid_issues boolean NOT NULL,
        has_heart_condition boolean NOT NULL,
        has_eating_disorder_history boolean NOT NULL,
        has_gallbladder_issues boolean NOT NULL,
        other_conditions text,
        allergies text[] NOT NULL,
        medications text[] NOT NULL,
        dietary_restrictions text[] NOT NULL,
        disliked_ingredients text[] NOT NULL,
        preferred_ingredients text[] NOT NULL,
        keto_experience_level character varying(20) NOT NULL,
        requires_human_review boolean NOT NULL,
        human_review_completed_at timestamp with time zone,
        human_review_notes text,
        override_accepted_at timestamp with time zone,
        override_disclaimer_version_id uuid,
        override_revoked_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_medical_profiles PRIMARY KEY (profile_id),
        CONSTRAINT fk_medical_profiles_disclaimer_versions_override_disclaimer_ve FOREIGN KEY (override_disclaimer_version_id) REFERENCES disclaimer_versions (version_id),
        CONSTRAINT fk_medical_profiles_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE notifications (
        notification_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid NOT NULL,
        type character varying(40) NOT NULL,
        priority character varying(5) NOT NULL,
        title character varying(150) NOT NULL,
        body text NOT NULL,
        deep_link text,
        icon_url text,
        metadata jsonb,
        delivery_channels text[] NOT NULL,
        sent_at timestamp with time zone,
        read_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_notifications PRIMARY KEY (notification_id),
        CONSTRAINT fk_notifications_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE password_reset_tokens (
        token_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid NOT NULL,
        token_hash character varying(255) NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        used_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_password_reset_tokens PRIMARY KEY (token_id),
        CONSTRAINT fk_password_reset_tokens_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE privacy_settings (
        settings_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid NOT NULL,
        share_weight character varying(20) NOT NULL,
        share_body_fat character varying(20) NOT NULL,
        share_measurements character varying(20) NOT NULL,
        share_photos character varying(20) NOT NULL,
        share_check_ins character varying(20) NOT NULL,
        allow_ai_mentions boolean NOT NULL,
        allow_push boolean NOT NULL,
        allow_email boolean NOT NULL,
        weekly_digest boolean NOT NULL,
        quiet_hours_start time without time zone NOT NULL,
        quiet_hours_end time without time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_privacy_settings PRIMARY KEY (settings_id),
        CONSTRAINT fk_privacy_settings_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE push_subscriptions (
        subscription_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid NOT NULL,
        endpoint text NOT NULL,
        p256dh_key text NOT NULL,
        auth_key text NOT NULL,
        user_agent text,
        is_active boolean NOT NULL,
        last_used_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_push_subscriptions PRIMARY KEY (subscription_id),
        CONSTRAINT fk_push_subscriptions_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE recipes (
        recipe_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        name character varying(200) NOT NULL,
        slug character varying(220),
        description text,
        meal_type character varying(20) NOT NULL,
        nutrition_track character varying(20) NOT NULL,
        ingredients jsonb NOT NULL DEFAULT ('[]'::jsonb),
        instructions text,
        prep_time_min integer,
        cook_time_min integer,
        servings integer NOT NULL,
        base_calories integer NOT NULL,
        base_protein_gr numeric(7,2) NOT NULL,
        base_fat_gr numeric(7,2) NOT NULL,
        base_carbs_gr numeric(7,2) NOT NULL,
        base_net_carbs_gr numeric(7,2),
        difficulty_level integer,
        photo_url text,
        tags text[] NOT NULL,
        source character varying(20) NOT NULL,
        ai_model text,
        created_by_user_id uuid,
        use_count integer NOT NULL,
        avg_rating numeric(3,2),
        rating_count integer NOT NULL,
        is_public boolean NOT NULL,
        compatible_mode_codes text[] NOT NULL,
        economic_tier integer,
        estimated_cost_per_serving_mxn numeric(10,2),
        total_prep_time_min integer,
        yield_servings_min integer,
        yield_servings_max integer,
        is_batch_cookable boolean NOT NULL,
        is_freezable boolean NOT NULL,
        cooking_methods text[] NOT NULL,
        difficulty_score integer,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        deleted_at timestamp with time zone,
        CONSTRAINT pk_recipes PRIMARY KEY (recipe_id),
        CONSTRAINT fk_recipes_users_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (user_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE refresh_tokens (
        token_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid NOT NULL,
        token_hash character varying(255) NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        revoked_at timestamp with time zone,
        user_agent text,
        ip_address inet,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_refresh_tokens PRIMARY KEY (token_id),
        CONSTRAINT fk_refresh_tokens_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE system_thresholds (
        threshold_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        code character varying(60) NOT NULL,
        name character varying(100) NOT NULL,
        description text,
        category character varying(30) NOT NULL,
        numeric_value numeric(12,4),
        text_value text,
        unit character varying(30),
        is_active boolean NOT NULL,
        updated_by_user_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_system_thresholds PRIMARY KEY (threshold_id),
        CONSTRAINT fk_system_thresholds_users_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (user_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE tutor_relationships (
        relationship_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        minor_user_id uuid NOT NULL,
        tutor_user_id uuid NOT NULL,
        relationship_type character varying(30) NOT NULL,
        minor_id_document_url text,
        minor_id_document_type text,
        tutor_id_document_url text,
        tutor_id_document_type text,
        parentage_proof_url text,
        verification_status character varying(20) NOT NULL,
        verified_at timestamp with time zone,
        verified_by_user_id uuid,
        rejection_reason text,
        consent_signed_at timestamp with time zone,
        consent_revoked_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_tutor_relationships PRIMARY KEY (relationship_id),
        CONSTRAINT fk_tutor_relationships_users_minor_user_id FOREIGN KEY (minor_user_id) REFERENCES users (user_id) ON DELETE CASCADE,
        CONSTRAINT fk_tutor_relationships_users_tutor_user_id FOREIGN KEY (tutor_user_id) REFERENCES users (user_id) ON DELETE CASCADE,
        CONSTRAINT fk_tutor_relationships_users_verified_by_user_id FOREIGN KEY (verified_by_user_id) REFERENCES users (user_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE user_goals (
        goal_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid NOT NULL,
        goal_type character varying(20) NOT NULL,
        start_weight_kg numeric(5,2) NOT NULL,
        target_weight_kg numeric(5,2),
        target_date date,
        motivation_text text,
        is_active boolean NOT NULL,
        achieved_at timestamp with time zone,
        abandoned_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_user_goals PRIMARY KEY (goal_id),
        CONSTRAINT fk_user_goals_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE user_milestones (
        milestone_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid NOT NULL,
        milestone_type character varying(50) NOT NULL,
        milestone_value numeric(8,2),
        achieved_at timestamp with time zone NOT NULL,
        posted_to_group boolean NOT NULL,
        CONSTRAINT pk_user_milestones PRIMARY KEY (milestone_id),
        CONSTRAINT fk_user_milestones_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE user_preferred_stores (
        preferred_store_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid NOT NULL,
        store_category_id uuid NOT NULL,
        custom_store_name character varying(100),
        google_place_id character varying(200),
        visit_frequency character varying(30),
        notes text,
        sort_order integer NOT NULL,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_user_preferred_stores PRIMARY KEY (preferred_store_id),
        CONSTRAINT fk_user_preferred_stores_store_categories_store_category_id FOREIGN KEY (store_category_id) REFERENCES store_categories (category_id) ON DELETE CASCADE,
        CONSTRAINT fk_user_preferred_stores_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE user_subscriptions (
        subscription_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid NOT NULL,
        plan_id uuid NOT NULL,
        status character varying(20) NOT NULL,
        started_at timestamp with time zone NOT NULL,
        current_period_start timestamp with time zone NOT NULL,
        current_period_end timestamp with time zone,
        cancelled_at timestamp with time zone,
        cancel_at_period_end boolean NOT NULL,
        payment_provider text,
        provider_subscription_id text,
        provider_customer_id text,
        metadata jsonb,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_user_subscriptions PRIMARY KEY (subscription_id),
        CONSTRAINT fk_user_subscriptions_subscription_plans_plan_id FOREIGN KEY (plan_id) REFERENCES subscription_plans (plan_id) ON DELETE CASCADE,
        CONSTRAINT fk_user_subscriptions_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE user_trials_used (
        trial_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid NOT NULL,
        plan_id uuid NOT NULL,
        trial_started_at timestamp with time zone NOT NULL,
        trial_ended_at timestamp with time zone,
        converted_to_paid boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_user_trials_used PRIMARY KEY (trial_id),
        CONSTRAINT fk_user_trials_used_subscription_plans_plan_id FOREIGN KEY (plan_id) REFERENCES subscription_plans (plan_id) ON DELETE CASCADE,
        CONSTRAINT fk_user_trials_used_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE weekly_summaries (
        summary_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid NOT NULL,
        week_start_date date NOT NULL,
        week_end_date date NOT NULL,
        avg_difficulty numeric(3,2),
        avg_hunger numeric(3,2),
        avg_energy numeric(3,2),
        adherence_percent numeric(5,2),
        weight_change_kg numeric(5,2),
        check_ins_count integer,
        meals_completed integer,
        meals_planned integer,
        is_in_plateau boolean NOT NULL,
        ai_recommendations jsonb,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_weekly_summaries PRIMARY KEY (summary_id),
        CONSTRAINT fk_weekly_summaries_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE progress_photos (
        photo_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid NOT NULL,
        photo_url text NOT NULL,
        storage_key text NOT NULL,
        angle character varying(20),
        visibility character varying(20) NOT NULL,
        associated_measurement_id uuid,
        file_size_bytes bigint,
        width_px integer,
        height_px integer,
        taken_at date NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        deleted_at timestamp with time zone,
        CONSTRAINT pk_progress_photos PRIMARY KEY (photo_id),
        CONSTRAINT fk_progress_photos_body_measurements_associated_measurement_id FOREIGN KEY (associated_measurement_id) REFERENCES body_measurements (measurement_id),
        CONSTRAINT fk_progress_photos_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE challenges (
        challenge_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        group_id uuid NOT NULL,
        title character varying(150) NOT NULL,
        description text,
        goal_type character varying(30) NOT NULL,
        goal_description text,
        reward_description text,
        start_date date NOT NULL,
        end_date date NOT NULL,
        is_active boolean NOT NULL,
        is_finalized boolean NOT NULL,
        finalized_at timestamp with time zone,
        created_by_user_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_challenges PRIMARY KEY (challenge_id),
        CONSTRAINT fk_challenges_groups_group_id FOREIGN KEY (group_id) REFERENCES groups (group_id) ON DELETE CASCADE,
        CONSTRAINT fk_challenges_users_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (user_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE group_memberships (
        membership_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        group_id uuid NOT NULL,
        user_id uuid NOT NULL,
        role character varying(20) NOT NULL,
        nickname character varying(50),
        joined_at timestamp with time zone NOT NULL,
        left_at timestamp with time zone,
        CONSTRAINT pk_group_memberships PRIMARY KEY (membership_id),
        CONSTRAINT fk_group_memberships_groups_group_id FOREIGN KEY (group_id) REFERENCES groups (group_id) ON DELETE CASCADE,
        CONSTRAINT fk_group_memberships_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE group_posts (
        post_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        group_id uuid NOT NULL,
        author_user_id uuid,
        post_type character varying(30) NOT NULL,
        content text,
        metadata jsonb,
        is_pinned boolean NOT NULL,
        is_announcement boolean NOT NULL,
        is_under_review boolean NOT NULL,
        moderation_reason character varying(50),
        moderation_severity character varying(10),
        moderation_resolved_at timestamp with time zone,
        moderation_action character varying(20),
        moderated_by_user_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        deleted_at timestamp with time zone,
        CONSTRAINT pk_group_posts PRIMARY KEY (post_id),
        CONSTRAINT fk_group_posts_groups_group_id FOREIGN KEY (group_id) REFERENCES groups (group_id) ON DELETE CASCADE,
        CONSTRAINT fk_group_posts_users_author_user_id FOREIGN KEY (author_user_id) REFERENCES users (user_id),
        CONSTRAINT fk_group_posts_users_moderated_by_user_id FOREIGN KEY (moderated_by_user_id) REFERENCES users (user_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE shopping_lists (
        list_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        group_id uuid NOT NULL,
        week_start_date date NOT NULL,
        week_end_date date NOT NULL,
        total_estimated_mxn numeric(10,2),
        notes text,
        generated_at timestamp with time zone NOT NULL,
        completed_at timestamp with time zone,
        budget_mode_id uuid,
        total_estimated_cost_mxn numeric(10,2),
        cost_breakdown jsonb,
        CONSTRAINT pk_shopping_lists PRIMARY KEY (list_id),
        CONSTRAINT fk_shopping_lists_budget_modes_budget_mode_id FOREIGN KEY (budget_mode_id) REFERENCES budget_modes (mode_id),
        CONSTRAINT fk_shopping_lists_groups_group_id FOREIGN KEY (group_id) REFERENCES groups (group_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE weekly_plans (
        plan_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid NOT NULL,
        group_id uuid,
        start_date date NOT NULL,
        end_date date NOT NULL,
        nutrition_track character varying(20) NOT NULL,
        is_override_plan boolean NOT NULL,
        is_refeed_week boolean NOT NULL,
        original_menu_content jsonb NOT NULL,
        current_menu_content jsonb,
        is_active boolean NOT NULL,
        generation_source character varying(20) NOT NULL,
        parent_plan_id uuid,
        ai_interaction_id uuid,
        budget_mode_id uuid,
        estimated_total_cost_mxn numeric(10,2),
        estimated_cost_per_person_mxn numeric(10,2),
        estimated_cost_gourmet_baseline_mxn numeric(10,2),
        savings_vs_gourmet_mxn numeric(10,2),
        savings_vs_gourmet_percent numeric(5,2),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_weekly_plans PRIMARY KEY (plan_id),
        CONSTRAINT fk_weekly_plans_ai_interactions_ai_interaction_id FOREIGN KEY (ai_interaction_id) REFERENCES ai_interactions (interaction_id),
        CONSTRAINT fk_weekly_plans_budget_modes_budget_mode_id FOREIGN KEY (budget_mode_id) REFERENCES budget_modes (mode_id),
        CONSTRAINT fk_weekly_plans_groups_group_id FOREIGN KEY (group_id) REFERENCES groups (group_id),
        CONSTRAINT fk_weekly_plans_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE,
        CONSTRAINT fk_weekly_plans_weekly_plans_parent_plan_id FOREIGN KEY (parent_plan_id) REFERENCES weekly_plans (plan_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE ingredient_substitutions (
        substitution_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        original_ingredient_id uuid NOT NULL,
        replacement_id uuid NOT NULL,
        applicable_mode_codes text[] NOT NULL,
        reason character varying(30) NOT NULL,
        cost_savings_percent numeric(5,2),
        quality_loss_score integer,
        notes text,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_ingredient_substitutions PRIMARY KEY (substitution_id),
        CONSTRAINT fk_ingredient_substitutions_ingredient_catalog_original_ingred FOREIGN KEY (original_ingredient_id) REFERENCES ingredient_catalog (ingredient_id) ON DELETE CASCADE,
        CONSTRAINT fk_ingredient_substitutions_ingredient_catalog_replacement_id FOREIGN KEY (replacement_id) REFERENCES ingredient_catalog (ingredient_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE recipe_ratings (
        rating_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        recipe_id uuid NOT NULL,
        user_id uuid NOT NULL,
        rating integer NOT NULL,
        comment text,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_recipe_ratings PRIMARY KEY (rating_id),
        CONSTRAINT fk_recipe_ratings_recipes_recipe_id FOREIGN KEY (recipe_id) REFERENCES recipes (recipe_id) ON DELETE CASCADE,
        CONSTRAINT fk_recipe_ratings_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE challenge_participants (
        participant_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        challenge_id uuid NOT NULL,
        user_id uuid NOT NULL,
        sub_group_id uuid,
        starting_value numeric(8,2),
        current_value numeric(8,2),
        final_score numeric(8,2),
        final_position integer,
        joined_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_challenge_participants PRIMARY KEY (participant_id),
        CONSTRAINT fk_challenge_participants_challenges_challenge_id FOREIGN KEY (challenge_id) REFERENCES challenges (challenge_id) ON DELETE CASCADE,
        CONSTRAINT fk_challenge_participants_groups_sub_group_id FOREIGN KEY (sub_group_id) REFERENCES groups (group_id),
        CONSTRAINT fk_challenge_participants_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE post_comments (
        comment_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        post_id uuid NOT NULL,
        user_id uuid NOT NULL,
        content text NOT NULL,
        parent_comment_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        deleted_at timestamp with time zone,
        CONSTRAINT pk_post_comments PRIMARY KEY (comment_id),
        CONSTRAINT fk_post_comments_group_posts_post_id FOREIGN KEY (post_id) REFERENCES group_posts (post_id) ON DELETE CASCADE,
        CONSTRAINT fk_post_comments_post_comments_parent_comment_id FOREIGN KEY (parent_comment_id) REFERENCES post_comments (comment_id) ON DELETE RESTRICT,
        CONSTRAINT fk_post_comments_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE post_reactions (
        reaction_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        post_id uuid NOT NULL,
        user_id uuid NOT NULL,
        reaction_type character varying(20) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_post_reactions PRIMARY KEY (reaction_id),
        CONSTRAINT fk_post_reactions_group_posts_post_id FOREIGN KEY (post_id) REFERENCES group_posts (post_id) ON DELETE CASCADE,
        CONSTRAINT fk_post_reactions_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE shopping_list_items (
        item_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        shopping_list_id uuid NOT NULL,
        ingredient_name character varying(100) NOT NULL,
        total_amount numeric(10,2) NOT NULL,
        unit character varying(30) NOT NULL,
        category character varying(50),
        estimated_cost_mxn numeric(10,2),
        is_purchased boolean NOT NULL,
        purchased_by_user_id uuid,
        purchased_at timestamp with time zone,
        notes text,
        sort_order integer NOT NULL,
        ingredient_id uuid,
        store_category_id uuid,
        substituted_from_id uuid,
        substitution_reason character varying(50),
        CONSTRAINT pk_shopping_list_items PRIMARY KEY (item_id),
        CONSTRAINT fk_shopping_list_items_ingredient_catalog_ingredient_id FOREIGN KEY (ingredient_id) REFERENCES ingredient_catalog (ingredient_id),
        CONSTRAINT fk_shopping_list_items_ingredient_catalog_substituted_from_id FOREIGN KEY (substituted_from_id) REFERENCES ingredient_catalog (ingredient_id),
        CONSTRAINT fk_shopping_list_items_shopping_lists_shopping_list_id FOREIGN KEY (shopping_list_id) REFERENCES shopping_lists (list_id) ON DELETE CASCADE,
        CONSTRAINT fk_shopping_list_items_store_categories_store_category_id FOREIGN KEY (store_category_id) REFERENCES store_categories (category_id),
        CONSTRAINT fk_shopping_list_items_users_purchased_by_user_id FOREIGN KEY (purchased_by_user_id) REFERENCES users (user_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE weekly_plan_meals (
        meal_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        plan_id uuid NOT NULL,
        day_of_week integer NOT NULL,
        meal_type character varying(20) NOT NULL,
        recipe_id uuid,
        portion_multiplier numeric(4,2) NOT NULL DEFAULT 1.0,
        is_locked boolean NOT NULL,
        user_note text,
        sort_order integer NOT NULL,
        row_version bigint NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_weekly_plan_meals PRIMARY KEY (meal_id),
        CONSTRAINT fk_weekly_plan_meals_recipes_recipe_id FOREIGN KEY (recipe_id) REFERENCES recipes (recipe_id),
        CONSTRAINT fk_weekly_plan_meals_weekly_plans_plan_id FOREIGN KEY (plan_id) REFERENCES weekly_plans (plan_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE TABLE meal_logs (
        log_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid NOT NULL,
        plan_meal_id uuid,
        recipe_id uuid,
        status character varying(20) NOT NULL,
        substitution_note text,
        actual_portion numeric(4,2),
        logged_for_date date NOT NULL,
        logged_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_meal_logs PRIMARY KEY (log_id),
        CONSTRAINT fk_meal_logs_recipes_recipe_id FOREIGN KEY (recipe_id) REFERENCES recipes (recipe_id),
        CONSTRAINT fk_meal_logs_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE,
        CONSTRAINT fk_meal_logs_weekly_plan_meals_plan_meal_id FOREIGN KEY (plan_meal_id) REFERENCES weekly_plan_meals (meal_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_ai_interactions_prompt_hash ON ai_interactions (prompt_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_ai_interactions_user_id_created_at ON ai_interactions (user_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_audit_log_entity_type_entity_id_created_at ON audit_log (entity_type, entity_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_audit_log_user_id_created_at ON audit_log (user_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_body_measurements_user_id_measured_at ON body_measurements (user_id, measured_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_budget_modes_code ON budget_modes (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_challenge_participants_challenge_id_user_id ON challenge_participants (challenge_id, user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_challenge_participants_sub_group_id ON challenge_participants (sub_group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_challenge_participants_user_id ON challenge_participants (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_challenges_created_by_user_id ON challenges (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_challenges_group_id_is_active ON challenges (group_id, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_daily_check_ins_user_id_check_in_date ON daily_check_ins (user_id, check_in_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_disclaimer_versions_disclaimer_type_is_current ON disclaimer_versions (disclaimer_type, is_current);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_disclaimer_versions_disclaimer_type_version_code ON disclaimer_versions (disclaimer_type, version_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_feature_flags_code ON feature_flags (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_group_memberships_group_id_user_id ON group_memberships (group_id, user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_group_memberships_user_id ON group_memberships (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_group_posts_author_user_id ON group_posts (author_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_group_posts_group_id ON group_posts (group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_group_posts_moderated_by_user_id ON group_posts (moderated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_groups_created_by_user_id ON groups (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_groups_invite_code ON groups (invite_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_groups_parent_group_id ON groups (parent_group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_ingredient_catalog_code ON ingredient_catalog (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_ingredient_catalog_name_search ON ingredient_catalog (name_search);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_ingredient_catalog_updated_by_user_id ON ingredient_catalog (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_ingredient_substitutions_original_ingredient_id_replacement ON ingredient_substitutions (original_ingredient_id, replacement_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_ingredient_substitutions_replacement_id ON ingredient_substitutions (replacement_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_keto_profiles_user_id ON keto_profiles (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_meal_logs_plan_meal_id ON meal_logs (plan_meal_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_meal_logs_recipe_id ON meal_logs (recipe_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_meal_logs_user_id_logged_for_date ON meal_logs (user_id, logged_for_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_medical_profiles_override_disclaimer_version_id ON medical_profiles (override_disclaimer_version_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_medical_profiles_user_id ON medical_profiles (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_notifications_user_id_created_at ON notifications (user_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_password_reset_tokens_user_id ON password_reset_tokens (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_post_comments_parent_comment_id ON post_comments (parent_comment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_post_comments_post_id ON post_comments (post_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_post_comments_user_id ON post_comments (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_post_reactions_post_id_user_id_reaction_type ON post_reactions (post_id, user_id, reaction_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_post_reactions_user_id ON post_reactions (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_privacy_settings_user_id ON privacy_settings (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_progress_photos_associated_measurement_id ON progress_photos (associated_measurement_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_progress_photos_user_id_taken_at ON progress_photos (user_id, taken_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_push_subscriptions_user_id_endpoint ON push_subscriptions (user_id, endpoint);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_recipe_ratings_recipe_id_user_id ON recipe_ratings (recipe_id, user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_recipe_ratings_user_id ON recipe_ratings (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_recipes_created_by_user_id ON recipes (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_recipes_slug ON recipes (slug);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_refresh_tokens_user_id ON refresh_tokens (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_shopping_list_items_ingredient_id ON shopping_list_items (ingredient_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_shopping_list_items_purchased_by_user_id ON shopping_list_items (purchased_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_shopping_list_items_shopping_list_id ON shopping_list_items (shopping_list_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_shopping_list_items_store_category_id ON shopping_list_items (store_category_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_shopping_list_items_substituted_from_id ON shopping_list_items (substituted_from_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_shopping_lists_budget_mode_id ON shopping_lists (budget_mode_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_shopping_lists_group_id_week_start_date ON shopping_lists (group_id, week_start_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_store_categories_code ON store_categories (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_subscription_plans_code ON subscription_plans (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_system_thresholds_category ON system_thresholds (category);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_system_thresholds_code ON system_thresholds (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_system_thresholds_updated_by_user_id ON system_thresholds (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_toxic_words_normalized_word ON toxic_words (normalized_word);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_tutor_relationships_minor_user_id_tutor_user_id ON tutor_relationships (minor_user_id, tutor_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_tutor_relationships_tutor_user_id ON tutor_relationships (tutor_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_tutor_relationships_verified_by_user_id ON tutor_relationships (verified_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_user_goals_user_id_is_active ON user_goals (user_id, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_user_milestones_user_id_milestone_type ON user_milestones (user_id, milestone_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_user_preferred_stores_store_category_id ON user_preferred_stores (store_category_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_user_preferred_stores_user_id_store_category_id ON user_preferred_stores (user_id, store_category_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_user_subscriptions_plan_id ON user_subscriptions (plan_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_user_subscriptions_user_id_status ON user_subscriptions (user_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_user_trials_used_plan_id ON user_trials_used (plan_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_user_trials_used_user_id_plan_id ON user_trials_used (user_id, plan_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_users_budget_mode_id ON users (budget_mode_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_users_disclaimer_version_id ON users (disclaimer_version_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_users_email ON users (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_users_tutor_consent_version_id ON users (tutor_consent_version_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_users_tutor_user_id ON users (tutor_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_weekly_plan_meals_plan_id_day_of_week_meal_type_sort_order ON weekly_plan_meals (plan_id, day_of_week, meal_type, sort_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_weekly_plan_meals_recipe_id ON weekly_plan_meals (recipe_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_weekly_plans_ai_interaction_id ON weekly_plans (ai_interaction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_weekly_plans_budget_mode_id ON weekly_plans (budget_mode_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_weekly_plans_group_id ON weekly_plans (group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_weekly_plans_parent_plan_id ON weekly_plans (parent_plan_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE INDEX ix_weekly_plans_user_id_start_date ON weekly_plans (user_id, start_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_weekly_summaries_user_id_week_start_date ON weekly_summaries (user_id, week_start_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_subscription_plans
                        BEFORE UPDATE ON subscription_plans
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_feature_flags
                        BEFORE UPDATE ON feature_flags
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_system_thresholds
                        BEFORE UPDATE ON system_thresholds
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_toxic_words
                        BEFORE UPDATE ON toxic_words
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_users
                        BEFORE UPDATE ON users
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_user_subscriptions
                        BEFORE UPDATE ON user_subscriptions
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_privacy_settings
                        BEFORE UPDATE ON privacy_settings
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_medical_profiles
                        BEFORE UPDATE ON medical_profiles
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_keto_profiles
                        BEFORE UPDATE ON keto_profiles
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_tutor_relationships
                        BEFORE UPDATE ON tutor_relationships
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_user_goals
                        BEFORE UPDATE ON user_goals
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_groups
                        BEFORE UPDATE ON groups
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_recipes
                        BEFORE UPDATE ON recipes
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_weekly_plans
                        BEFORE UPDATE ON weekly_plans
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_weekly_plan_meals
                        BEFORE UPDATE ON weekly_plan_meals
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_group_posts
                        BEFORE UPDATE ON group_posts
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_post_comments
                        BEFORE UPDATE ON post_comments
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_challenges
                        BEFORE UPDATE ON challenges
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_budget_modes
                        BEFORE UPDATE ON budget_modes
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_ingredient_catalog
                        BEFORE UPDATE ON ingredient_catalog
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_ingredient_substitutions
                        BEFORE UPDATE ON ingredient_substitutions
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_store_categories
                        BEFORE UPDATE ON store_categories
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                        CREATE TRIGGER set_updated_at_user_preferred_stores
                        BEFORE UPDATE ON user_preferred_stores
                        FOR EACH ROW
                        EXECUTE FUNCTION trg_set_updated_at();
                    
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                    CREATE TRIGGER check_group_depth
                    BEFORE INSERT OR UPDATE ON groups
                    FOR EACH ROW
                    EXECUTE FUNCTION trg_check_group_depth();
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN

                    ALTER TABLE weekly_plans DROP CONSTRAINT IF EXISTS fk_weekly_plans_ai_interactions_ai_interaction_id;
                    ALTER TABLE weekly_plans ADD CONSTRAINT fk_weekly_plans_ai_interactions_ai_interaction_id 
                    FOREIGN KEY (ai_interaction_id) REFERENCES ai_interactions(interaction_id) 
                    DEFERRABLE INITIALLY DEFERRED;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260507214630_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260507214630_InitialCreate', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260511175802_AddFavoriteRecipes') THEN
    CREATE TABLE favorite_recipes (
        favorite_id uuid NOT NULL DEFAULT (uuid_generate_v4()),
        user_id uuid NOT NULL,
        recipe_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_favorite_recipes PRIMARY KEY (favorite_id),
        CONSTRAINT fk_favorite_recipes_recipes_recipe_id FOREIGN KEY (recipe_id) REFERENCES recipes (recipe_id) ON DELETE CASCADE,
        CONSTRAINT fk_favorite_recipes_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260511175802_AddFavoriteRecipes') THEN
    CREATE INDEX ix_favorite_recipes_recipe_id ON favorite_recipes (recipe_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260511175802_AddFavoriteRecipes') THEN
    CREATE UNIQUE INDEX ix_favorite_recipes_user_id_recipe_id ON favorite_recipes (user_id, recipe_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260511175802_AddFavoriteRecipes') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260511175802_AddFavoriteRecipes', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260511182954_AddUserRole') THEN
    ALTER TABLE users ADD role character varying(20) NOT NULL DEFAULT 'user';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260511182954_AddUserRole') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260511182954_AddUserRole', '8.0.11');
    END IF;
END $EF$;
COMMIT;

