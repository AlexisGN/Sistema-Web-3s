using System.Text.Json;
using Microsoft.Data.SqlClient;
namespace Sistema3S.Tests.Fixtures;

public static class BiReferencia
{
    public static async Task PrepararAsync(QaDatabase db)
    {
        // Exclusivamente datos de la base desechable de QA; no usa configuración de BD_3S.
        if(!db.Name.StartsWith("Sistema3S_QA_"))throw new InvalidOperationException("Se requiere la base exclusiva de QA.");
        await db.Exec("DELETE Inventario; DELETE Producto; DELETE ElementoCatalogo; DELETE Categoria;");
        var config=Path.Combine(AppContext.BaseDirectory,"Fixtures/bi-catalogo.json");
        using var doc=JsonDocument.Parse(await File.ReadAllTextAsync(config));
        await using var conn=new SqlConnection(db.Connection);await conn.OpenAsync();
        using var categorias=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory,"Fixtures/bi-categorias.json")));
        await new SqlCommand("SET IDENTITY_INSERT Categoria ON",conn).ExecuteNonQueryAsync();
        foreach(var cat in categorias.RootElement.EnumerateArray()){await using var cmd=new SqlCommand("INSERT Categoria(IdCategoria,Nombre,Estado) VALUES(@id,@nombre,1)",conn);cmd.Parameters.AddWithValue("@id",cat.GetProperty("IdCategoria").GetInt32());cmd.Parameters.AddWithValue("@nombre",cat.GetProperty("Nombre").GetString());await cmd.ExecuteNonQueryAsync();}
        await new SqlCommand("SET IDENTITY_INSERT Categoria OFF",conn).ExecuteNonQueryAsync();
        await new SqlCommand("SET IDENTITY_INSERT ElementoCatalogo ON;",conn).ExecuteNonQueryAsync();
        foreach(var row in doc.RootElement.GetProperty("filas").EnumerateArray())
        {
            await using var cmd=new SqlCommand("INSERT ElementoCatalogo(IdElementoCatalogo,IdTipoElemento,Nombre,PrecioReferencial,FechaRegistro) VALUES(@id,1,@nombre,@precio,'20260101')",conn);
            cmd.Parameters.AddWithValue("@id",row.GetProperty("IdElementoCatalogo").GetInt32());cmd.Parameters.AddWithValue("@nombre",row.GetProperty("Nombre").GetString()!);cmd.Parameters.AddWithValue("@precio",row.GetProperty("Precio").GetDecimal());await cmd.ExecuteNonQueryAsync();
        }
        await new SqlCommand("SET IDENTITY_INSERT ElementoCatalogo OFF; SET IDENTITY_INSERT Producto ON;",conn).ExecuteNonQueryAsync();
        foreach(var row in doc.RootElement.GetProperty("filas").EnumerateArray())
        {
            await using var cmd=new SqlCommand("INSERT Producto(IdProducto,IdElementoCatalogo,IdCategoria,IdUnidadMedida,CodigoProducto) VALUES(@id,@elemento,@categoria,1,@codigo)",conn);
            cmd.Parameters.AddWithValue("@categoria",row.GetProperty("IdCategoria").GetInt32());
            cmd.Parameters.AddWithValue("@id",row.GetProperty("IdProducto").GetInt32());cmd.Parameters.AddWithValue("@elemento",row.GetProperty("IdElementoCatalogo").GetInt32());cmd.Parameters.AddWithValue("@codigo","BIQA-"+row.GetProperty("IdProducto").GetInt32());await cmd.ExecuteNonQueryAsync();
        }
        await new SqlCommand("SET IDENTITY_INSERT Producto OFF; INSERT Inventario(IdProducto,StockActual,StockMinimo) SELECT IdProducto,0,5 FROM Producto;",conn).ExecuteNonQueryAsync();
        await db.Exec(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory,"Fixtures/bi-referencia-qa.sql")));
    }
}
