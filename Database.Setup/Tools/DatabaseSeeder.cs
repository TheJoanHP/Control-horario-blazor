// Métodos adicionales para DatabaseSeeder.cs (agregar al archivo existente)

/// <summary>
/// Crea la estructura de tablas del tenant
/// </summary>
private async Task CreateTenantStructureAsync(string databaseName)
{
    Console.WriteLine($"🏗️  Creando estructura de tablas para {databaseName}...");
    
    var tenantConnectionString = GetTenantConnectionString(_connectionString, databaseName);
    
    var scriptPath = Path.Combine(_scriptsPath, "02-CreateTenantTemplate.sql");
    if (!File.Exists(scriptPath))
    {
        throw new FileNotFoundException($"Script no encontrado: {scriptPath}");
    }

    var script = await File.ReadAllTextAsync(scriptPath);
    
    using var connection = new NpgsqlConnection(tenantConnectionString);
    await connection.OpenAsync();
    
    using var command = new NpgsqlCommand(script, connection);
    command.CommandTimeout = 300;
    
    await command.ExecuteNonQueryAsync();
    
    Console.WriteLine($"✅ Estructura creada para {databaseName}");
}

/// <summary>
/// Inserta datos iniciales del tenant
/// </summary>
private async Task SeedTenantDataAsync(string databaseName)
{
    Console.WriteLine($"🌱 Insertando datos iniciales para {databaseName}...");
    
    var tenantConnectionString = GetTenantConnectionString(_connectionString, databaseName);
    
    var scriptPath = Path.Combine(_scriptsPath, "03-SeedTenantData.sql");
    if (!File.Exists(scriptPath))
    {
        throw new FileNotFoundException($"Script no encontrado: {scriptPath}");
    }

    var script = await File.ReadAllTextAsync(scriptPath);
    
    using var connection = new NpgsqlConnection(tenantConnectionString);
    await connection.OpenAsync();
    
    using var command = new NpgsqlCommand(script, connection);
    command.CommandTimeout = 300;
    
    await command.ExecuteNonQueryAsync();
    
    Console.WriteLine($"✅ Datos iniciales insertados para {databaseName}");
}

/// <summary>
/// Obtiene la cadena de conexión master (postgres)
/// </summary>
private string GetMasterConnectionString(string connectionString)
{
    var builder = new NpgsqlConnectionStringBuilder(connectionString)
    {
        Database = "postgres"
    };
    return builder.ToString();
}

/// <summary>
/// Obtiene la cadena de conexión para un tenant específico
/// </summary>
private string GetTenantConnectionString(string connectionString, string databaseName)
{
    var builder = new NpgsqlConnectionStringBuilder(connectionString)
    {
        Database = databaseName
    };
    return builder.ToString();
}

/// <summary>
/// Limpia todas las bases de datos del sistema
/// </summary>
public async Task CleanupAllDatabasesAsync()
{
    Console.WriteLine("🧹 Limpiando todas las bases de datos...");
    
    var masterConnectionString = GetMasterConnectionString(_connectionString);
    
    using var connection = new NpgsqlConnection(masterConnectionString);
    await connection.OpenAsync();
    
    // Obtener todas las bases de datos del sistema
    var getDatabasesCommand = new NpgsqlCommand(@"
        SELECT datname 
        FROM pg_database 
        WHERE datname LIKE 'SphereTimeControl%'
        OR datname = 'SphereTimeControl'", connection);
    
    var databases = new List<string>();
    using var reader = await getDatabasesCommand.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        databases.Add(reader.GetString("datname"));
    }
    reader.Close();
    
    // Eliminar cada base de datos
    foreach (var dbName in databases)
    {
        Console.WriteLine($"🗑️  Eliminando base de datos: {dbName}");
        
        // Terminar conexiones activas
        var killConnectionsCommand = new NpgsqlCommand($@"
            SELECT pg_terminate_backend(pid)
            FROM pg_stat_activity
            WHERE datname = '{dbName}'
            AND pid <> pg_backend_pid()", connection);
        
        await killConnectionsCommand.ExecuteNonQueryAsync();
        
        // Eliminar la base de datos
        var dropCommand = new NpgsqlCommand($@"DROP DATABASE IF EXISTS ""{dbName}""", connection);
        await dropCommand.ExecuteNonQueryAsync();
        
        Console.WriteLine($"✅ {dbName} eliminada");
    }
    
    Console.WriteLine("✅ Limpieza completada");
}

/// <summary>
/// Muestra el estado actual de las bases de datos
/// </summary>
public async Task ShowDatabaseStatusAsync()
{
    var masterConnectionString = GetMasterConnectionString(_connectionString);
    
    using var connection = new NpgsqlConnection(masterConnectionString);
    await connection.OpenAsync();
    
    // Verificar base de datos central
    Console.WriteLine("📊 Estado de la Base de Datos Central:");
    Console.WriteLine("=====================================");
    
    var centralExists = await CheckDatabaseExistsAsync(connection, "SphereTimeControl");
    Console.WriteLine($"SphereTimeControl (Central): {(centralExists ? "✅ Existe" : "❌ No existe")}");
    
    if (centralExists)
    {
        var centralStats = await GetDatabaseStatsAsync("SphereTimeControl");
        Console.WriteLine($"  - Tenants registrados: {centralStats.TenantsCount}");
        Console.WriteLine($"  - Admins del sistema: {centralStats.AdminsCount}");
    }
    
    Console.WriteLine();
    
    // Verificar tenants
    Console.WriteLine("🏢 Estado de los Tenants:");
    Console.WriteLine("========================");
    
    var getTenantDbsCommand = new NpgsqlCommand(@"
        SELECT datname 
        FROM pg_database 
        WHERE datname LIKE 'SphereTimeControl_%'
        ORDER BY datname", connection);
    
    var tenantDatabases = new List<string>();
    using var reader = await getTenantDbsCommand.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        tenantDatabases.Add(reader.GetString("datname"));
    }
    reader.Close();
    
    if (tenantDatabases.Count == 0)
    {
        Console.WriteLine("❌ No hay tenants configurados");
    }
    else
    {
        foreach (var dbName in tenantDatabases)
        {
            var tenantId = dbName.Replace("SphereTimeControl_", "");
            Console.WriteLine($"  {tenantId}: ✅ Configurado");
            
            try
            {
                var tenantStats = await GetTenantStatsAsync(dbName);
                Console.WriteLine($"    - Empleados: {tenantStats.EmployeesCount}");
                Console.WriteLine($"    - Departamentos: {tenantStats.DepartmentsCount}");
                Console.WriteLine($"    - Registros hoy: {tenantStats.TodayRecordsCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    - ⚠️  Error al obtener estadísticas: {ex.Message}");
            }
        }
    }
    
    Console.WriteLine();
    Console.WriteLine("🔗 Información de Conexión:");
    Console.WriteLine("==========================");
    
    var builder = new NpgsqlConnectionStringBuilder(_connectionString);
    Console.WriteLine($"Servidor: {builder.Host}:{builder.Port}");
    Console.WriteLine($"Usuario: {builder.Username}");
    Console.WriteLine($"Base de datos por defecto: {builder.Database}");
}

/// <summary>
/// Verifica si una base de datos existe
/// </summary>
private async Task<bool> CheckDatabaseExistsAsync(NpgsqlConnection connection, string databaseName)
{
    var command = new NpgsqlCommand(
        "SELECT 1 FROM pg_database WHERE datname = @dbname", 
        connection);
    command.Parameters.AddWithValue("dbname", databaseName);
    
    var result = await command.ExecuteScalarAsync();
    return result != null;
}

/// <summary>
/// Obtiene estadísticas de la base de datos central
/// </summary>
private async Task<CentralDatabaseStats> GetDatabaseStatsAsync(string databaseName)
{
    var centralConnectionString = GetTenantConnectionString(_connectionString, databaseName);
    
    using var connection = new NpgsqlConnection(centralConnectionString);
    await connection.OpenAsync();
    
    var stats = new CentralDatabaseStats();
    
    // Contar tenants
    try
    {
        var tenantsCommand = new NpgsqlCommand("SELECT COUNT(*) FROM Tenants", connection);
        stats.TenantsCount = Convert.ToInt32(await tenantsCommand.ExecuteScalarAsync());
    }
    catch
    {
        stats.TenantsCount = 0;
    }
    
    // Contar admins
    try
    {
        var adminsCommand = new NpgsqlCommand("SELECT COUNT(*) FROM SphereAdmins", connection);
        stats.AdminsCount = Convert.ToInt32(await adminsCommand.ExecuteScalarAsync());
    }
    catch
    {
        stats.AdminsCount = 0;
    }
    
    return stats;
}

/// <summary>
/// Obtiene estadísticas de un tenant
/// </summary>
private async Task<TenantDatabaseStats> GetTenantStatsAsync(string databaseName)
{
    var tenantConnectionString = GetTenantConnectionString(_connectionString, databaseName);
    
    using var connection = new NpgsqlConnection(tenantConnectionString);
    await connection.OpenAsync();
    
    var stats = new TenantDatabaseStats();
    
    // Contar empleados
    try
    {
        var employeesCommand = new NpgsqlCommand("SELECT COUNT(*) FROM Employees", connection);
        stats.EmployeesCount = Convert.ToInt32(await employeesCommand.ExecuteScalarAsync());
    }
    catch
    {
        stats.EmployeesCount = 0;
    }
    
    // Contar departamentos
    try
    {
        var departmentsCommand = new NpgsqlCommand("SELECT COUNT(*) FROM Departments", connection);
        stats.DepartmentsCount = Convert.ToInt32(await departmentsCommand.ExecuteScalarAsync());
    }
    catch
    {
        stats.DepartmentsCount = 0;
    }
    
    // Contar registros de hoy
    try
    {
        var todayCommand = new NpgsqlCommand(@"
            SELECT COUNT(*) 
            FROM TimeRecords 
            WHERE DATE(CreatedAt) = CURRENT_DATE", connection);
        stats.TodayRecordsCount = Convert.ToInt32(await todayCommand.ExecuteScalarAsync());
    }
    catch
    {
        stats.TodayRecordsCount = 0;
    }
    
    return stats;
}

/// <summary>
/// Verifica la conectividad con la base de datos
/// </summary>
public async Task<bool> TestConnectionAsync()
{
    try
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var command = new NpgsqlCommand("SELECT 1", connection);
        await command.ExecuteScalarAsync();
        
        return true;
    }
    catch
    {
        return false;
    }
}

// Clases auxiliares para estadísticas
private class CentralDatabaseStats
{
    public int TenantsCount { get; set; }
    public int AdminsCount { get; set; }
}

private class TenantDatabaseStats
{
    public int EmployeesCount { get; set; }
    public int DepartmentsCount { get; set; }
    public int TodayRecordsCount { get; set; }
}