-- =============================================
-- Script: Crear Base de Datos Central de Sphere Time Control
-- Descripción: Crea la base de datos central que gestiona todos los tenants
-- Autor: Sistema Sphere Time Control
-- Fecha: 2025
-- =============================================

-- Crear base de datos central si no existe
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_database WHERE datname = 'SphereTimeControl_Central') THEN
        CREATE DATABASE "SphereTimeControl_Central"
            WITH 
            OWNER = postgres
            ENCODING = 'UTF8'
            LC_COLLATE = 'en_US.utf8'
            LC_CTYPE = 'en_US.utf8'
            TABLESPACE = pg_default
            CONNECTION LIMIT = -1;
    END IF;
END
$$;

-- Conectar a la base de datos central
\c SphereTimeControl_Central;

-- Crear extensiones necesarias
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =============================================
-- TABLA: Tenants (Empresas/Clientes)
-- =============================================
CREATE TABLE IF NOT EXISTS "Tenants" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Subdomain" VARCHAR(50) NOT NULL UNIQUE,
    "DatabaseName" VARCHAR(200) NOT NULL UNIQUE,
    "ContactEmail" VARCHAR(255) NOT NULL UNIQUE,
    "Phone" VARCHAR(20),
    "Active" BOOLEAN NOT NULL DEFAULT true,
    "LicenseType" INTEGER NOT NULL DEFAULT 0, -- 0=Trial, 1=Basic, 2=Professional, 3=Enterprise
    "MaxEmployees" INTEGER NOT NULL DEFAULT 10,
    "LicenseExpiresAt" TIMESTAMP NOT NULL DEFAULT (NOW() + INTERVAL '30 days'),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Índices para optimizar consultas
CREATE INDEX IF NOT EXISTS "IX_Tenants_Subdomain" ON "Tenants" ("Subdomain");
CREATE INDEX IF NOT EXISTS "IX_Tenants_Active" ON "Tenants" ("Active");
CREATE INDEX IF NOT EXISTS "IX_Tenants_LicenseType" ON "Tenants" ("LicenseType");

-- =============================================
-- TABLA: Licenses (Licencias)
-- =============================================
CREATE TABLE IF NOT EXISTS "Licenses" (
    "Id" SERIAL PRIMARY KEY,
    "TenantId" INTEGER NOT NULL,
    "Type" INTEGER NOT NULL, -- 0=Trial, 1=Basic, 2=Professional, 3=Enterprise
    "MaxEmployees" INTEGER NOT NULL,
    "MonthlyPrice" DECIMAL(10,2) NOT NULL,
    "Active" BOOLEAN NOT NULL DEFAULT true,
    "StartsAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "ExpiresAt" TIMESTAMP NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_Licenses_Tenants" FOREIGN KEY ("TenantId") 
        REFERENCES "Tenants" ("Id") ON DELETE CASCADE
);

-- Índices
CREATE INDEX IF NOT EXISTS "IX_Licenses_TenantId" ON "Licenses" ("TenantId");
CREATE INDEX IF NOT EXISTS "IX_Licenses_Active" ON "Licenses" ("Active");

-- =============================================
-- TABLA: SphereAdmins (Super Administradores)
-- =============================================
CREATE TABLE IF NOT EXISTS "SphereAdmins" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(255) NOT NULL UNIQUE,
    "PasswordHash" TEXT NOT NULL,
    "Active" BOOLEAN NOT NULL DEFAULT true,
    "LastLoginAt" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Índices
CREATE INDEX IF NOT EXISTS "IX_SphereAdmins_Email" ON "SphereAdmins" ("Email");
CREATE INDEX IF NOT EXISTS "IX_SphereAdmins_Active" ON "SphereAdmins" ("Active");

-- =============================================
-- TABLA: SystemConfigs (Configuraciones del Sistema)
-- =============================================
CREATE TABLE IF NOT EXISTS "SystemConfigs" (
    "Id" SERIAL PRIMARY KEY,
    "Key" VARCHAR(100) NOT NULL UNIQUE,
    "Value" TEXT NOT NULL,
    "Description" VARCHAR(500),
    "IsPublic" BOOLEAN NOT NULL DEFAULT false,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Índices
CREATE INDEX IF NOT EXISTS "IX_SystemConfigs_Key" ON "SystemConfigs" ("Key");
CREATE INDEX IF NOT EXISTS "IX_SystemConfigs_IsPublic" ON "SystemConfigs" ("IsPublic");

-- =============================================
-- FUNCIONES Y TRIGGERS
-- =============================================

-- Función para actualizar automáticamente UpdatedAt
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Triggers para actualizar UpdatedAt automáticamente
DROP TRIGGER IF EXISTS update_tenants_updated_at ON "Tenants";
CREATE TRIGGER update_tenants_updated_at 
    BEFORE UPDATE ON "Tenants" 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS update_sphere_admins_updated_at ON "SphereAdmins";
CREATE TRIGGER update_sphere_admins_updated_at 
    BEFORE UPDATE ON "SphereAdmins" 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS update_system_configs_updated_at ON "SystemConfigs";
CREATE TRIGGER update_system_configs_updated_at 
    BEFORE UPDATE ON "SystemConfigs" 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =============================================
-- DATOS INICIALES
-- =============================================

-- Insertar configuraciones del sistema por defecto
INSERT INTO "SystemConfigs" ("Key", "Value", "Description", "IsPublic") VALUES
('SystemName', 'Sphere Time Control', 'Nombre del sistema', true),
('SystemVersion', '1.0.0', 'Versión del sistema', true),
('MaxTenantsPerInstance', '100', 'Máximo número de tenants por instancia', false),
('DefaultTrialDays', '30', 'Días de prueba por defecto', false),
('MaintenanceMode', 'false', 'Modo de mantenimiento', true),
('AllowRegistration', 'true', 'Permitir registro de nuevos tenants', false),
('DefaultTimeZone', 'UTC', 'Zona horaria por defecto', false),
('SystemEmail', 'system@spheretimecontrol.com', 'Email del sistema', false)
ON CONFLICT ("Key") DO NOTHING;

-- Insertar super administrador por defecto
-- Contraseña: admin123 (hasheada con bcrypt)
INSERT INTO "SphereAdmins" ("Name", "Email", "PasswordHash", "Active") VALUES
('Super Admin', 'admin@spheretimecontrol.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj7/N8.D8eo.', true)
ON CONFLICT ("Email") DO NOTHING;

-- Insertar tenant de demostración
INSERT INTO "Tenants" ("Name", "Subdomain", "DatabaseName", "ContactEmail", "Phone", "LicenseType", "MaxEmployees") VALUES
('Empresa Demo', 'demo', 'SphereTimeControl_demo', 'admin@empresademo.com', '+34 666 777 888', 0, 50)
ON CONFLICT ("Subdomain") DO NOTHING;

-- Insertar licencia para el tenant demo
INSERT INTO "Licenses" ("TenantId", "Type", "MaxEmployees", "MonthlyPrice", "ExpiresAt")
SELECT t."Id", 0, 50, 0.00, (NOW() + INTERVAL '90 days')
FROM "Tenants" t 
WHERE t."Subdomain" = 'demo'
AND NOT EXISTS (SELECT 1 FROM "Licenses" l WHERE l."TenantId" = t."Id");

-- =============================================
-- VISTAS ÚTILES
-- =============================================

-- Vista para obtener información completa de tenants
CREATE OR REPLACE VIEW "TenantsView" AS
SELECT 
    t."Id",
    t."Name",
    t."Subdomain",
    t."DatabaseName",
    t."ContactEmail",
    t."Phone",
    t."Active",
    t."LicenseType",
    t."MaxEmployees",
    t."LicenseExpiresAt",
    t."CreatedAt",
    t."UpdatedAt",
    l."MonthlyPrice",
    l."StartsAt" as "LicenseStartsAt",
    CASE 
        WHEN t."LicenseExpiresAt" < NOW() THEN 'Expired'
        WHEN t."LicenseExpiresAt" < NOW() + INTERVAL '7 days' THEN 'Expiring'
        ELSE 'Active'
    END as "LicenseStatus"
FROM "Tenants" t
LEFT JOIN "Licenses" l ON t."Id" = l."TenantId" AND l."Active" = true;

-- Vista para estadísticas del sistema
CREATE OR REPLACE VIEW "SystemStats" AS
SELECT
    (SELECT COUNT(*) FROM "Tenants" WHERE "Active" = true) as "ActiveTenants",
    (SELECT COUNT(*) FROM "Tenants") as "TotalTenants",
    (SELECT COUNT(*) FROM "Tenants" WHERE "LicenseType" = 0) as "TrialTenants",
    (SELECT COUNT(*) FROM "Tenants" WHERE "LicenseExpiresAt" < NOW()) as "ExpiredTenants",
    (SELECT COUNT(*) FROM "SphereAdmins" WHERE "Active" = true) as "ActiveAdmins",
    (SELECT SUM("MonthlyPrice") FROM "Licenses" WHERE "Active" = true) as "MonthlyRevenue";

-- =============================================
-- FUNCIONES ÚTILES
-- =============================================

-- Función para generar nombre de base de datos único
CREATE OR REPLACE FUNCTION generate_database_name(tenant_subdomain VARCHAR(50))
RETURNS VARCHAR(200) AS $
BEGIN
    RETURN 'SphereTimeControl_' || LOWER(tenant_subdomain);
END;
$ LANGUAGE plpgsql;

-- Función para validar subdomain
CREATE OR REPLACE FUNCTION is_valid_subdomain(subdomain VARCHAR(50))
RETURNS BOOLEAN AS $
BEGIN
    -- Solo letras, números y guiones, entre 3 y 50 caracteres
    RETURN subdomain ~ '^[a-z0-9-]{3,50} 
           AND subdomain NOT LIKE '-%' 
           AND subdomain NOT LIKE '%-'
           AND subdomain NOT IN ('www', 'api', 'admin', 'mail', 'ftp', 'test', 'dev', 'stage', 'prod');
END;
$ LANGUAGE plpgsql;

-- Función para obtener configuración del sistema
CREATE OR REPLACE FUNCTION get_system_config(config_key VARCHAR(100))
RETURNS TEXT AS $
DECLARE
    config_value TEXT;
BEGIN
    SELECT "Value" INTO config_value
    FROM "SystemConfigs"
    WHERE "Key" = config_key;
    
    RETURN config_value;
END;
$ LANGUAGE plpgsql;

-- =============================================
-- PERMISOS Y SEGURIDAD
-- =============================================

-- Crear rol para aplicación
DO $
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'sphere_app_user') THEN
        CREATE ROLE sphere_app_user WITH LOGIN PASSWORD 'sphere_app_2025!';
    END IF;
END
$;

-- Otorgar permisos necesarios
GRANT CONNECT ON DATABASE "SphereTimeControl_Central" TO sphere_app_user;
GRANT USAGE ON SCHEMA public TO sphere_app_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO sphere_app_user;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO sphere_app_user;

-- =============================================
-- COMENTARIOS EN TABLAS
-- =============================================

COMMENT ON TABLE "Tenants" IS 'Tabla principal de empresas/clientes (tenants) del sistema';
COMMENT ON TABLE "Licenses" IS 'Licencias y configuraciones de facturación por tenant';
COMMENT ON TABLE "SphereAdmins" IS 'Super administradores del sistema central';
COMMENT ON TABLE "SystemConfigs" IS 'Configuraciones globales del sistema';

COMMENT ON COLUMN "Tenants"."Subdomain" IS 'Subdominio único para acceso (ej: empresa1.tudominio.com)';
COMMENT ON COLUMN "Tenants"."DatabaseName" IS 'Nombre de la base de datos específica del tenant';
COMMENT ON COLUMN "Tenants"."LicenseType" IS '0=Trial, 1=Basic, 2=Professional, 3=Enterprise';

-- =============================================
-- FINALIZACIÓN
-- =============================================

-- Mostrar resumen de creación
DO $
BEGIN
    RAISE NOTICE '==============================================';
    RAISE NOTICE 'Base de datos central creada exitosamente';
    RAISE NOTICE '==============================================';
    RAISE NOTICE 'Tenants activos: %', (SELECT COUNT(*) FROM "Tenants" WHERE "Active" = true);
    RAISE NOTICE 'Admins del sistema: %', (SELECT COUNT(*) FROM "SphereAdmins" WHERE "Active" = true);
    RAISE NOTICE 'Configuraciones: %', (SELECT COUNT(*) FROM "SystemConfigs");
    RAISE NOTICE '==============================================';
    RAISE NOTICE 'Credenciales por defecto:';
    RAISE NOTICE 'Email: admin@spheretimecontrol.com';
    RAISE NOTICE 'Password: admin123';
    RAISE NOTICE '==============================================';
END
$;