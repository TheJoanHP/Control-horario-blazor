-- =============================================
-- Script: Sembrar Datos Iniciales para Tenant
-- Descripción: Inserta datos de ejemplo en la base de datos del tenant
-- Uso: Se ejecuta después de crear la estructura del tenant
-- Autor: Sistema Sphere Time Control
-- Fecha: 2025
-- =============================================

-- NOTA: Este script asume que ya está conectado a la base de datos del tenant

-- =============================================
-- INSERTAR DATOS DE LA EMPRESA
-- =============================================

-- =============================================
-- Script: Sembrar Datos Iniciales para Tenant
-- Descripción: Inserta datos de ejemplo en la base de datos del tenant
-- Uso: Se ejecuta después de crear la estructura del tenant
-- Autor: Sistema Sphere Time Control
-- Fecha: 2025
-- =============================================

-- NOTA: Este script asume que ya está conectado a la base de datos del tenant

-- =============================================
-- INSERTAR DATOS DE LA EMPRESA
-- =============================================

INSERT INTO "Companies" (
    "Name", 
    "TaxId", 
    "Address", 
    "Phone", 
    "Email",
    "WorkStartTime",
    "WorkEndTime",
    "ToleranceMinutes",
    "VacationDaysPerYear"
) VALUES (
    'Empresa Demo', 
    'B12345678', 
    'Calle Principal 123, 28001 Madrid, España', 
    '+34 911 234 567', 
    'info@empresademo.com',
    '09:00:00',
    '18:00:00',
    15,
    22
) ON CONFLICT DO NOTHING;

-- =============================================
-- INSERTAR DEPARTAMENTOS
-- =============================================

INSERT INTO "Departments" ("CompanyId", "Name", "Description") VALUES 
(1, 'Administración', 'Departamento de administración y recursos humanos'),
(1, 'Desarrollo', 'Departamento de desarrollo de software'),
(1, 'Marketing', 'Departamento de marketing y comunicación'),
(1, 'Ventas', 'Departamento comercial y de ventas'),
(1, 'Soporte', 'Departamento de soporte técnico')
ON CONFLICT DO NOTHING;

-- =============================================
-- INSERTAR EMPLEADOS
-- =============================================

-- Admin de la empresa
INSERT INTO "Employees" (
    "CompanyId", "DepartmentId", "FirstName", "LastName", "Email", 
    "Phone", "EmployeeCode", "Role", "PasswordHash"
) VALUES (
    1, 1, 'Ana', 'García', 'admin@empresademo.com',
    '+34 666 111 222', 'EMP001', 1, -- CompanyAdmin
    '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj7/N8.D8eo.' -- admin123
) ON CONFLICT (Email) DO NOTHING;

-- Supervisor de desarrollo
INSERT INTO "Employees" (
    "CompanyId", "DepartmentId", "FirstName", "LastName", "Email", 
    "Phone", "EmployeeCode", "Role", "PasswordHash"
) VALUES (
    1, 2, 'Carlos', 'Martínez', 'supervisor@empresademo.com',
    '+34 666 333 444', 'EMP002', 2, -- Supervisor
    '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj7/N8.D8eo.' -- admin123
) ON CONFLICT (Email) DO NOTHING;

-- Empleados regulares
INSERT INTO "Employees" (
    "CompanyId", "DepartmentId", "FirstName", "LastName", "Email", 
    "Phone", "EmployeeCode", "Role", "PasswordHash"
) VALUES 
(1, 2, 'María', 'López', 'maria.lopez@empresademo.com', '+34 666 555 666', 'EMP003', 3, '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj7/N8.D8eo.'),
(1, 2, 'Juan', 'Rodríguez', 'juan.rodriguez@empresademo.com', '+34 666 777 888', 'EMP004', 3, '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj7/N8.D8eo.'),
(1, 3, 'Laura', 'Sánchez', 'laura.sanchez@empresademo.com', '+34 666 999 000', 'EMP005', 3, '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj7/N8.D8eo.'),
(1, 4, 'Pedro', 'Fernández', 'pedro.fernandez@empresademo.com', '+34 666 111 333', 'EMP006', 3, '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj7/N8.D8eo.'),
(1, 5, 'Elena', 'González', 'elena.gonzalez@empresademo.com', '+34 666 444 555', 'EMP007', 3, '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj7/N8.D8eo.'),
(1, 1, 'Roberto', 'Jiménez', 'roberto.jimenez@empresademo.com', '+34 666 666 777', 'EMP008', 3, '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj7/N8.D8eo.')
ON CONFLICT (Email) DO NOTHING;

-- =============================================
-- INSERTAR POLÍTICAS DE VACACIONES
-- =============================================

INSERT INTO "VacationPolicies" (
    "CompanyId", "Name", "AnnualDays", "MaxConsecutiveDays", 
    "MinAdvanceNoticeDays", "RequireApproval", "CarryOverEnabled", "MaxCarryOverDays"
) VALUES (
    1, 'Política General 2025', 22, 15, 15, true, true, 5
) ON CONFLICT DO NOTHING;

-- =============================================
-- INSERTAR SALDOS DE VACACIONES PARA 2025
-- =============================================

INSERT INTO "VacationBalances" ("EmployeeId", "Year", "TotalDays") 
SELECT "Id", 2025, 22 
FROM "Employees" 
WHERE "CompanyId" = 1
ON CONFLICT ("EmployeeId", "Year") DO NOTHING;

-- =============================================
-- INSERTAR HORARIOS DE TRABAJO POR DEFECTO
-- =============================================

INSERT INTO "WorkSchedules" (
    "EmployeeId", "Name",
    "MondayEnabled", "MondayStart", "MondayEnd",
    "TuesdayEnabled", "TuesdayStart", "TuesdayEnd",
    "WednesdayEnabled", "WednesdayStart", "WednesdayEnd",
    "ThursdayEnabled", "ThursdayStart", "ThursdayEnd",
    "FridayEnabled", "FridayStart", "FridayEnd",
    "SaturdayEnabled", "SundayEnabled"
)
SELECT 
    "Id", 'Horario Estándar',
    true, '09:00:00', '18:00:00',
    true, '09:00:00', '18:00:00',
    true, '09:00:00', '18:00:00',
    true, '09:00:00', '18:00:00',
    true, '09:00:00', '15:00:00', -- Viernes jornada reducida
    false, false
FROM "Employees" 
WHERE "CompanyId" = 1;

-- =============================================
-- INSERTAR REGISTROS DE TIEMPO DE EJEMPLO (ÚLTIMOS 7 DÍAS)
-- =============================================

-- Función auxiliar para generar registros de tiempo
DO $
DECLARE
    emp_record RECORD;
    work_date DATE;
    check_in_time TIMESTAMP;
    check_out_time TIMESTAMP;
    break_start_time TIMESTAMP;
    break_end_time TIMESTAMP;
    day_counter INTEGER;
BEGIN
    -- Para cada empleado activo
    FOR emp_record IN SELECT "Id" FROM "Employees" WHERE "Active" = true AND "Role" = 3 LOOP
        day_counter := 0;
        
        -- Generar registros para los últimos 7 días laborables
        FOR work_date IN 
            SELECT generate_series(
                CURRENT_DATE - INTERVAL '10 days', 
                CURRENT_DATE - INTERVAL '1 day', 
                '1 day'::interval
            )::date as date_val
            WHERE EXTRACT(DOW FROM generate_series(
                CURRENT_DATE - INTERVAL '10 days', 
                CURRENT_DATE - INTERVAL '1 day', 
                '1 day'::interval
            )) BETWEEN 1 AND 5 -- Solo días laborables
            LIMIT 7
        LOOP
            day_counter := day_counter + 1;
            
            -- Solo generar registros para algunos días (simular ausencias ocasionales)
            IF day_counter <= 5 OR RANDOM() > 0.2 THEN
                -- Hora de entrada (entre 8:45 y 9:15)
                check_in_time := work_date + TIME '09:00:00' + 
                    (RANDOM() * INTERVAL '30 minutes') - INTERVAL '15 minutes';
                
                -- Hora de salida (entre 17:45 y 18:15)
                check_out_time := work_date + TIME '18:00:00' + 
                    (RANDOM() * INTERVAL '30 minutes') - INTERVAL '15 minutes';
                
                -- Insertar entrada
                INSERT INTO "TimeRecords" ("EmployeeId", "Type", "Timestamp", "Notes") 
                VALUES (emp_record."Id", 0, check_in_time, 'Entrada automática');
                
                -- Insertar descanso (50% de probabilidad)
                IF RANDOM() > 0.5 THEN
                    break_start_time := work_date + TIME '14:00:00' + 
                        (RANDOM() * INTERVAL '60 minutes');
                    break_end_time := break_start_time + 
                        (INTERVAL '15 minutes' + (RANDOM() * INTERVAL '45 minutes'));
                    
                    INSERT INTO "TimeRecords" ("EmployeeId", "Type", "Timestamp", "Notes") 
                    VALUES (emp_record."Id", 2, break_start_time, 'Inicio descanso');
                    
                    INSERT INTO "TimeRecords" ("EmployeeId", "Type", "Timestamp", "Notes") 
                    VALUES (emp_record."Id", 3, break_end_time, 'Fin descanso');
                END IF;
                
                -- Insertar salida
                INSERT INTO "TimeRecords" ("EmployeeId", "Type", "Timestamp", "Notes") 
                VALUES (emp_record."Id", 1, check_out_time, 'Salida automática');
            END IF;
        END LOOP;
    END LOOP;
END
$;

-- =============================================
-- INSERTAR SOLICITUDES DE VACACIONES DE EJEMPLO
-- =============================================

-- Solicitud aprobada
INSERT INTO "VacationRequests" (
    "EmployeeId", "StartDate", "EndDate", "DaysRequested", 
    "Comments", "Status", "ReviewedById", "ReviewedAt", "ResponseComments"
) VALUES (
    3, CURRENT_DATE + INTERVAL '30 days', CURRENT_DATE + INTERVAL '37 days', 6,
    'Vacaciones de verano en familia', 1, 1, NOW(), 'Aprobado. Que disfrutes las vacaciones.'
);

-- Solicitud pendiente
INSERT INTO "VacationRequests" (
    "EmployeeId", "StartDate", "EndDate", "DaysRequested", "Comments", "Status"
) VALUES (
    4, CURRENT_DATE + INTERVAL '45 days', CURRENT_DATE + INTERVAL '49 days', 3,
    'Fin de semana largo', 0
);

-- Solicitud rechazada
INSERT INTO "VacationRequests" (
    "EmployeeId", "StartDate", "EndDate", "DaysRequested", 
    "Comments", "Status", "ReviewedById", "ReviewedAt", "ResponseComments"
) VALUES (
    5, CURRENT_DATE + INTERVAL '7 days', CURRENT_DATE + INTERVAL '11 days', 3,
    'Urgente - asuntos familiares', 2, 1, NOW(), 'Lo siento, pero hay demasiada carga de trabajo esa semana. Propón otras fechas.'
);

-- =============================================
-- ACTUALIZAR SALDOS DE VACACIONES
-- =============================================

-- Actualizar días usados basado en solicitudes aprobadas
UPDATE "VacationBalances" 
SET "UsedDays" = (
    SELECT COALESCE(SUM("DaysRequested"), 0)
    FROM "VacationRequests" 
    WHERE "VacationRequests"."EmployeeId" = "VacationBalances"."EmployeeId"
    AND "Status" = 1 -- Aprobada
    AND EXTRACT(YEAR FROM "StartDate") = "VacationBalances"."Year"
),
"PendingDays" = (
    SELECT COALESCE(SUM("DaysRequested"), 0)
    FROM "VacationRequests" 
    WHERE "VacationRequests"."EmployeeId" = "VacationBalances"."EmployeeId"
    AND "Status" = 0 -- Pendiente
    AND EXTRACT(YEAR FROM "StartDate") = "VacationBalances"."Year"
)
WHERE "Year" = 2025;

-- =============================================
-- CREAR DATOS ESTADÍSTICOS ÚTILES
-- =============================================

-- Vista materializada para estadísticas (opcional)
CREATE MATERIALIZED VIEW IF NOT EXISTS "DailyAttendanceStats" AS
SELECT 
    DATE("Timestamp") as "Date",
    COUNT(DISTINCT "EmployeeId") as "EmployeesPresent",
    (SELECT COUNT(*) FROM "Employees" WHERE "Active" = true) as "TotalEmployees",
    ROUND(
        COUNT(DISTINCT "EmployeeId")::DECIMAL / 
        (SELECT COUNT(*) FROM "Employees" WHERE "Active" = true) * 100, 2
    ) as "AttendanceRate"
FROM "TimeRecords"
WHERE "Type" = 0 -- Solo entradas
AND "Timestamp" >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY DATE("Timestamp")
ORDER BY "Date" DESC;

-- Índice para la vista materializada
CREATE INDEX IF NOT EXISTS "IX_DailyAttendanceStats_Date" ON "DailyAttendanceStats" ("Date");

-- =============================================
-- COMENTARIOS FINALES
-- =============================================

COMMENT ON MATERIALIZED VIEW "DailyAttendanceStats" IS 'Estadísticas diarias de asistencia de los últimos 30 días';

-- =============================================
-- MENSAJE DE FINALIZACIÓN
-- =============================================

DO $
DECLARE
    emp_count INTEGER;
    dept_count INTEGER;
    record_count INTEGER;
    vacation_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO emp_count FROM "Employees";
    SELECT COUNT(*) INTO dept_count FROM "Departments";
    SELECT COUNT(*) INTO record_count FROM "TimeRecords";
    SELECT COUNT(*) INTO vacation_count FROM "VacationRequests";
    
    RAISE NOTICE '==============================================';
    RAISE NOTICE 'Datos iniciales del tenant insertados exitosamente';
    RAISE NOTICE '==============================================';
    RAISE NOTICE 'Empleados creados: %', emp_count;
    RAISE NOTICE 'Departamentos creados: %', dept_count;
    RAISE NOTICE 'Registros de tiempo: %', record_count;
    RAISE NOTICE 'Solicitudes de vacaciones: %', vacation_count;
    RAISE NOTICE '==============================================';
    RAISE NOTICE 'Credenciales de acceso:';
    RAISE NOTICE '- Admin: admin@empresademo.com / admin123';
    RAISE NOTICE '- Supervisor: supervisor@empresademo.com / admin123';
    RAISE NOTICE '- Empleados: [nombre].[apellido]@empresademo.com / admin123';
    RAISE NOTICE '==============================================';
    RAISE NOTICE 'La base de datos está lista para usar!';
    RAISE NOTICE '==============================================';
END
$;