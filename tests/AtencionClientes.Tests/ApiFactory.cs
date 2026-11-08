using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;

namespace AtencionClientes.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    public string ConnectionString => Environment.GetEnvironmentVariable("TURNOS_TEST_CONNECTION")
        ?? throw new InvalidOperationException("Configura TURNOS_TEST_CONNECTION con la base AtencionClientesTests.");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SqlServer", ConnectionString);
    }

    public async Task ResetAsync()
    {
        var settings = new SqlConnectionStringBuilder(ConnectionString);
        if (settings.InitialCatalog != "AtencionClientesTests")
            throw new InvalidOperationException("Las pruebas solo limpian AtencionClientesTests.");

        settings.InitialCatalog = "master";
        await using (var master = new SqlConnection(settings.ConnectionString))
        {
            await master.OpenAsync();
            await using var create = new SqlCommand("IF DB_ID('AtencionClientesTests') IS NULL CREATE DATABASE AtencionClientesTests", master);
            await create.ExecuteNonQueryAsync();
        }
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var clear = new SqlCommand("""
            IF OBJECT_ID('Turnos') IS NOT NULL DELETE FROM Turnos;
            IF OBJECT_ID('Clientes') IS NOT NULL DELETE FROM Clientes;
            IF OBJECT_ID('Empleados') IS NOT NULL DELETE FROM Empleados;
            """, connection);
        await clear.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition("Api", DisableParallelization = true)]
public class ApiCollection : ICollectionFixture<ApiFactory>;
