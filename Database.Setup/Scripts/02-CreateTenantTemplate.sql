-- =============================================
-- Script: Crear Plantilla de Base de Datos para Tenants
-- Descripción: Crea la estructura de base de datos que usará cada tenant/empresa
-- Uso: Este script se ejecuta para cada nuevo tenant que se registre
-- Autor: Sistema Sphere Time Control
-- Fecha: 2025
-- =============================================

-- NOTA: Este script usa {TENANT_ID} como placeholder que será reemplazado
-- por el ID real del tenant al crear la base de datos

-- Crear base de datos del tenant
-- CREATE DATABASE "SphereTimeControl_{TENANT_ID}"
--     WITH 
--     OWNER = postgres
--     ENCODING = 'UTF8'
--     LC_COLLATE = 'en_US.utf8'
--     LC_CTYPE = 'en_US.utf8'
--     TABLESPACE = pg_default
--     CONNECTION LIMIT = -1;

-- El comando CREATE DATABASE debe ejecutarse desde código C#
-- porque no se puede hacer en un script transaccional

-- =============================================
-- CONECTAR A LA BASE DE DATOS DEL TENANT
-- =============================================
-- \c "SphereTimeControl_{TENANT_ID}";

-- Crear extensiones necesarias
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =============================================
-- TABLA: Companies (Empresa del Tenant)
-- =============================================
CREATE TABLE IF NOT EXISTS "Companies" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "TaxId" VARCHAR(20),
    "Address" VARCHAR(255),
    "Phone" VARCHAR(20),
    "Email" VARCHAR(255),
    "Active" BOOLEAN NOT NULL DEFAULT true,
    "WorkStartTime" TIME NOT NULL DEFAULT '09:00:00',
    "WorkEndTime" TIME NOT NULL DEFAULT '17:00:00',
    "ToleranceMinutes" INTEGER NOT NULL DEFAULT 15,
    "VacationDaysPerYear" INTEGER NOT NULL DEFAULT 22,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Índices
CREATE INDEX IF NOT EXISTS "IX_Companies_Active" ON "Companies" ("Active");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Companies_TaxId" ON "Companies" ("TaxId") WHERE "TaxId" IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Companies_Email" ON "Companies" ("Email") WHERE "Email" IS NOT NULL;

-- =============================================
-- TABLA: Departments (Departamentos)
-- =============================================
CREATE TABLE IF NOT EXISTS "Departments" (
    "Id" SERIAL PRIMARY KEY,
    "CompanyId" INTEGER NOT NULL,
    "Name" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500),
    "Active" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_Departments_Companies" FOREIGN KEY ("CompanyId") 
        REFERENCES "Companies" ("Id") ON DELETE RESTRICT
);

-- Índices
CREATE INDEX IF NOT EXISTS "IX_Departments_CompanyId" ON "Departments" ("CompanyId");
CREATE INDEX IF NOT EXISTS "IX_Departments_Active" ON "Departments" ("Active");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Departments_CompanyId_Name" ON "Departments" ("CompanyId", "Name");

-- =============================================
-- TABLA: Employees (Empleados)
-- =============================================
CREATE TABLE IF NOT EXISTS "Employees" (
    "Id" SERIAL PRIMARY KEY,
    "CompanyId" INTEGER NOT NULL,
    "DepartmentId" INTEGER,
    "FirstName" VARCHAR(50) NOT NULL,
    "LastName" VARCHAR(50) NOT NULL,
    "Email" VARCHAR(255) NOT NULL UNIQUE,
    "Phone" VARCHAR(20),
    "EmployeeCode" VARCHAR(50) NOT NULL,
    "Role" INTEGER NOT NULL DEFAULT 3, -- 0=SphereAdmin, 1=CompanyAdmin, 2=Supervisor, 3=Employee
    "PasswordHash" TEXT NOT NULL,
    "Active" BOOLEAN NOT NULL DEFAULT true,
    "LastLoginAt" TIMESTAMP,
    "CustomWorkStartTime" TIME,
    "CustomWorkEndTime" TIME,
    "HiredAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_Employees_Companies" FOREIGN KEY ("CompanyId") 
        REFERENCES "Companies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Employees_Departments" FOREIGN KEY ("DepartmentId") 
        REFERENCES "Departments" ("Id") ON DELETE SET NULL
);

-- Índices
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Employees_Email" ON "Employees" ("Email");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Employees_CompanyId_EmployeeCode" ON "Employees" ("CompanyId", "EmployeeCode");
CREATE INDEX IF NOT EXISTS "IX_Employees_CompanyId" ON "Employees" ("CompanyId");
CREATE INDEX IF NOT EXISTS "IX_Employees_DepartmentId" ON "Employees" ("DepartmentId");
CREATE INDEX IF NOT EXISTS "IX_Employees_Active" ON "Employees" ("Active");
CREATE INDEX IF NOT EXISTS "IX_Employees_Role" ON "Employees" ("Role");

-- =============================================
-- TABLA: TimeRecords (Registros de Tiempo)
-- =============================================
CREATE TABLE IF NOT EXISTS "TimeRecords" (
    "Id" SERIAL PRIMARY KEY,
    "EmployeeId" INTEGER NOT NULL,
    "Type" INTEGER NOT NULL, -- 0=CheckIn, 1=CheckOut, 2=BreakStart, 3=BreakEnd
    "Timestamp" TIMESTAMP NOT NULL,
    "Notes" VARCHAR(500),
    "Latitude" DOUBLE PRECISION,
    "Longitude" DOUBLE PRECISION,
    "Location" VARCHAR(255),
    "DeviceInfo" VARCHAR(100),
    "IpAddress" VARCHAR(45),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_TimeRecords_Employees" FOREIGN KEY ("EmployeeId") 
        REFERENCES "Employees" ("Id") ON DELETE CASCADE
);

-- Índices optimizados para consultas frecuentes
CREATE INDEX IF NOT EXISTS "IX_TimeRecords_EmployeeId_Timestamp" ON "TimeRecords" ("EmployeeId", "Timestamp");
CREATE INDEX IF NOT EXISTS "IX_TimeRecords_Type_Timestamp" ON "TimeRecords" ("Type", "Timestamp");
CREATE INDEX IF NOT EXISTS "IX_TimeRecords_Timestamp" ON "TimeRecords" ("Timestamp");
CREATE INDEX IF NOT EXISTS "IX_TimeRecords_Date" ON "TimeRecords" (DATE("Timestamp"));

-- =============================================
-- TABLA: WorkSchedules (Horarios de Trabajo)
-- =============================================
CREATE TABLE IF NOT EXISTS "WorkSchedules" (
    "Id" SERIAL PRIMARY KEY,
    "EmployeeId" INTEGER NOT NULL,
    "Name" VARCHAR(100) NOT NULL,
    "MondayEnabled" BOOLEAN NOT NULL DEFAULT true,
    "MondayStart" TIME DEFAULT '09:00:00',
    "MondayEnd" TIME DEFAULT '17:00:00',
    "TuesdayEnabled" BOOLEAN NOT NULL DEFAULT true,
    "TuesdayStart" TIME DEFAULT '09:00:00',
    "TuesdayEnd" TIME DEFAULT '17:00:00',
    "WednesdayEnabled" BOOLEAN NOT NULL DEFAULT true,
    "WednesdayStart" TIME DEFAULT '09:00:00',
    "WednesdayEnd" TIME DEFAULT '17:00:00',
    "ThursdayEnabled" BOOLEAN NOT NULL DEFAULT true,
    "ThursdayStart" TIME DEFAULT '09:00:00',
    "ThursdayEnd" TIME DEFAULT '17:00:00',
    "FridayEnabled" BOOLEAN NOT NULL DEFAULT true,
    "FridayStart" TIME DEFAULT '09:00:00',
    "FridayEnd" TIME DEFAULT '17:00:00',
    "SaturdayEnabled" BOOLEAN NOT NULL DEFAULT false,
    "SaturdayStart" TIME,
    "SaturdayEnd" TIME,
    "SundayEnabled" BOOLEAN NOT NULL DEFAULT false,
    "SundayStart" TIME,
    "SundayEnd" TIME,
    "Active" BOOLEAN NOT NULL DEFAULT true,
    "EffectiveFrom" TIMESTAMP NOT NULL DEFAULT NOW(),
    "EffectiveTo" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_WorkSchedules_Employees" FOREIGN KEY ("EmployeeId") 
        REFERENCES "Employees" ("Id") ON DELETE CASCADE
);

-- Índices
CREATE INDEX IF NOT EXISTS "IX_WorkSchedules_EmployeeId_EffectiveFrom" ON "WorkSchedules" ("EmployeeId", "EffectiveFrom", "Active");

-- =============================================
-- TABLA: Breaks (Descansos)
-- =============================================
CREATE TABLE IF NOT EXISTS "Breaks" (
    "Id" SERIAL PRIMARY KEY,
    "EmployeeId" INTEGER NOT NULL,
    "StartTime" TIMESTAMP NOT NULL,
    "EndTime" TIMESTAMP,
    "Notes" VARCHAR(500),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_Breaks_Employees" FOREIGN KEY ("EmployeeId") 
        REFERENCES "Employees" ("Id") ON DELETE CASCADE
);

-- Índices
CREATE INDEX IF NOT EXISTS "IX_Breaks_EmployeeId_StartTime" ON "Breaks" ("EmployeeId", "StartTime");

-- =============================================
-- TABLA: Overtime (Horas Extra)
-- =============================================
CREATE TABLE IF NOT EXISTS "Overtime" (
    "Id" SERIAL PRIMARY KEY,
    "EmployeeId" INTEGER NOT NULL,
    "Date" DATE NOT NULL,
    "Duration" INTERVAL NOT NULL,
    "Reason" VARCHAR(500),
    "Approved" BOOLEAN NOT NULL DEFAULT false,
    "ApprovedById" INTEGER,
    "ApprovedAt" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_Overtime_Employees" FOREIGN KEY ("EmployeeId") 
        REFERENCES "Employees" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Overtime_ApprovedBy" FOREIGN KEY ("ApprovedById") 
        REFERENCES "Employees" ("Id") ON DELETE SET NULL
);

-- Índices
CREATE INDEX IF NOT EXISTS "IX_Overtime_EmployeeId_Date" ON "Overtime" ("EmployeeId", "Date");
CREATE INDEX IF NOT EXISTS "IX_Overtime_Approved" ON "Overtime" ("Approved");

-- =============================================
-- TABLA: VacationRequests (Solicitudes de Vacaciones)
-- =============================================
CREATE TABLE IF NOT EXISTS "VacationRequests" (
    "Id" SERIAL PRIMARY KEY,
    "EmployeeId" INTEGER NOT NULL,
    "StartDate" DATE NOT NULL,
    "EndDate" DATE NOT NULL,
    "DaysRequested" INTEGER NOT NULL,
    "Comments" VARCHAR(1000),
    "Status" INTEGER NOT NULL DEFAULT 0, -- 0=Pending, 1=Approved, 2=Rejected, 3=Cancelled
    "ResponseComments" VARCHAR(1000),
    "ReviewedById" INTEGER,
    "ReviewedAt" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_VacationRequests_Employees" FOREIGN KEY ("EmployeeId") 
        REFERENCES "Employees" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_VacationRequests_ReviewedBy" FOREIGN KEY ("ReviewedById") 
        REFERENCES "Employees" ("Id") ON DELETE SET NULL
);

-- Índices
CREATE INDEX IF NOT EXISTS "IX_VacationRequests_EmployeeId_StartDate" ON "VacationRequests" ("EmployeeId", "StartDate");
CREATE INDEX IF NOT EXISTS "IX_VacationRequests_Status" ON "VacationRequests" ("Status");
CREATE INDEX IF NOT EXISTS "IX_VacationRequests_ReviewedById" ON "VacationRequests" ("ReviewedById");

-- =============================================
-- TABLA: VacationPolicies (Políticas de Vacaciones)
-- =============================================
CREATE TABLE IF NOT EXISTS "VacationPolicies" (
    "Id" SERIAL PRIMARY KEY,
    "CompanyId" INTEGER NOT NULL,
    "Name" VARCHAR(100) NOT NULL,
    "AnnualDays" INTEGER NOT NULL DEFAULT 22,
    "MaxConsecutiveDays" INTEGER NOT NULL DEFAULT 15,
    "MinAdvanceNoticeDays" INTEGER NOT NULL DEFAULT 15,
    "RequireApproval" BOOLEAN NOT NULL DEFAULT true,
    "CarryOverEnabled" BOOLEAN NOT NULL DEFAULT true,
    "MaxCarryOverDays" INTEGER NOT NULL DEFAULT 5,
    "Active" BOOLEAN NOT NULL DEFAULT true,
    "EffectiveFrom" TIMESTAMP NOT NULL DEFAULT NOW(),
    "EffectiveTo" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_VacationPolicies_Companies" FOREIGN KEY ("CompanyId") 
        REFERENCES "Companies" ("Id") ON DELETE CASCADE
);

-- Índices
CREATE INDEX IF NOT EXISTS "IX_VacationPolicies_CompanyId_EffectiveFrom" ON "VacationPolicies" ("CompanyId", "EffectiveFrom", "Active");

-- =============================================
-- TABLA: VacationBalances (Saldos de Vacaciones)
-- =============================================
CREATE TABLE IF NOT EXISTS "VacationBalances" (
    "Id" SERIAL PRIMARY KEY,
    "EmployeeId" INTEGER NOT NULL,
    "Year" INTEGER NOT NULL,
    "TotalDays" INTEGER NOT NULL,
    "UsedDays" INTEGER NOT NULL DEFAULT 0,
    "PendingDays" INTEGER NOT NULL DEFAULT 0,
    "CarriedOverDays" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_VacationBalances_Employees" FOREIGN KEY ("EmployeeId") 
        REFERENCES "Employees" ("Id") ON DELETE CASCADE
);

-- Índices
CREATE UNIQUE INDEX IF NOT EXISTS "IX_VacationBalances_EmployeeId_Year" ON "VacationBalances" ("EmployeeId", "Year");

-- =============================================
-- FUNCIONES Y TRIGGERS
-- =============================================

-- Función para actualizar UpdatedAt
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Triggers para actualizar UpdatedAt
CREATE TRIGGER update_companies_updated_at BEFORE UPDATE ON "Companies" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_employees_updated_at BEFORE UPDATE ON "Employees" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_vacation_requests_updated_at BEFORE UPDATE ON "VacationRequests" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_vacation_balances_updated_at BEFORE UPDATE ON "VacationBalances" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =============================================
-- FUNCIONES ÚTILES PARA EL TENANT
-- =============================================

-- Función para calcular horas trabajadas en un día
CREATE OR REPLACE FUNCTION calculate_worked_hours(employee_id INTEGER, work_date DATE)
RETURNS INTERVAL AS $$
DECLARE
    total_worked INTERVAL := '0 minutes';
    check_in_time TIMESTAMP;
    check_out_time TIMESTAMP;
BEGIN
    -- Buscar entrada y salida del día
    SELECT "Timestamp" INTO check_in_time
    FROM "TimeRecords"
    WHERE "EmployeeId" = employee_id 
      AND "Type" = 0 -- CheckIn
      AND DATE("Timestamp") = work_date
    ORDER BY "Timestamp" ASC
    LIMIT 1;
    
    SELECT "Timestamp" INTO check_out_time
    FROM "TimeRecords"
    WHERE "EmployeeId" = employee_id 
      AND "Type" = 1 -- CheckOut
      AND DATE("Timestamp") = work_date
    ORDER BY "Timestamp" DESC
    LIMIT 1;
    
    -- Calcular tiempo trabajado si hay entrada y salida
    IF check_in_time IS NOT NULL AND check_out_time IS NOT NULL THEN
        total_worked := check_out_time - check_in_time;
        
        -- Restar tiempo de descansos
        SELECT COALESCE(SUM("EndTime" - "StartTime"), '0 minutes') INTO total_worked
        FROM "Breaks"
        WHERE "EmployeeId" = employee_id
          AND DATE("StartTime") = work_date
          AND "EndTime" IS NOT NULL;
        
        total_worked := (check_out_time - check_in_time) - total_worked;
    END IF;
    
    RETURN GREATEST(total_worked, '0 minutes');
END;
$$ LANGUAGE plpgsql;

-- =============================================
-- VISTAS ÚTILES
-- =============================================

-- Vista de empleados con información completa
CREATE OR REPLACE VIEW "EmployeesView" AS
SELECT 
    e."Id",
    e."CompanyId",
    e."DepartmentId",
    e."FirstName",
    e."LastName",
    (e."FirstName" || ' ' || e."LastName") as "FullName",
    e."Email",
    e."Phone",
    e."EmployeeCode",
    e."Role",
    e."Active",
    e."LastLoginAt",
    e."HiredAt",
    e."CreatedAt",
    d."Name" as "DepartmentName",
    c."Name" as "CompanyName",
    COALESCE(e."CustomWorkStartTime", c."WorkStartTime") as "WorkStartTime",
    COALESCE(e."CustomWorkEndTime", c."WorkEndTime") as "WorkEndTime"
FROM "Employees" e
LEFT JOIN "Departments" d ON e."DepartmentId" = d."Id"
LEFT JOIN "Companies" c ON e."CompanyId" = c."Id";

-- Vista de registros de tiempo con información del empleado
CREATE OR REPLACE VIEW "TimeRecordsView" AS
SELECT 
    tr."Id",
    tr."EmployeeId",
    tr."Type",
    tr."Timestamp",
    tr."Notes",
    tr."Location",
    tr."CreatedAt",
    e."FullName" as "EmployeeName",
    e."EmployeeCode",
    e."DepartmentName",
    CASE tr."Type"
        WHEN 0 THEN 'Entrada'
        WHEN 1 THEN 'Salida'
        WHEN 2 THEN 'Inicio Descanso'
        WHEN 3 THEN 'Fin Descanso'
        ELSE 'Desconocido'
    END as "TypeDisplay"
FROM "TimeRecords" tr
JOIN "EmployeesView" e ON tr."EmployeeId" = e."Id";

-- =============================================
-- COMENTARIOS
-- =============================================

COMMENT ON TABLE "Companies" IS 'Información de la empresa del tenant';
COMMENT ON TABLE "Departments" IS 'Departamentos de la empresa';
COMMENT ON TABLE "Employees" IS 'Empleados de la empresa';
COMMENT ON TABLE "TimeRecords" IS 'Registros de entrada/salida y descansos';
COMMENT ON TABLE "WorkSchedules" IS 'Horarios de trabajo personalizados';
COMMENT ON TABLE "VacationRequests" IS 'Solicitudes de vacaciones';
COMMENT ON TABLE "VacationBalances" IS 'Saldos de días de vacaciones por empleado y año';

-- =============================================
-- MENSAJE DE FINALIZACIÓN
-- =============================================

DO $$
BEGIN
    RAISE NOTICE '==============================================';
    RAISE NOTICE 'Base de datos del tenant creada exitosamente';
    RAISE NOTICE '==============================================';
    RAISE NOTICE 'Tablas creadas: %', (
        SELECT COUNT(*) 
        FROM information_schema.tables 
        WHERE table_schema = 'public' 
        AND table_type = 'BASE TABLE'
    );
    RAISE NOTICE 'Vistas creadas: %', (
        SELECT COUNT(*) 
        FROM information_schema.views 
        WHERE table_schema = 'public'
    );
    RAISE NOTICE '==============================================';
END
$$;