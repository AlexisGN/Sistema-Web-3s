using System.Data;
using Microsoft.Data.SqlClient;

namespace Sistema3S.Web.Services.Bi;

public sealed class BiConexion(IConfiguration config)
{
    public async Task<SqlConnection> AbrirAsync(CancellationToken ct)
    {
        var c=new SqlConnection(config.GetConnectionString("BD_3S"));
        try { await c.OpenAsync(ct); return c; } catch { await c.DisposeAsync(); throw; }
    }
    public static async Task<Dictionary<string,List<Dictionary<string,object?>>>> LeerAsync(SqlCommand command, string[] nombres, CancellationToken ct)
    {
        var result=new Dictionary<string,List<Dictionary<string,object?>>>();
        await using var reader=await command.ExecuteReaderAsync(ct);
        foreach(var nombre in nombres)
        {
            var rows=new List<Dictionary<string,object?>>();
            while(await reader.ReadAsync(ct))
            {
                var row=new Dictionary<string,object?>();
                for(int i=0;i<reader.FieldCount;i++) row[reader.GetName(i)]=reader.IsDBNull(i)?null:reader.GetValue(i);
                rows.Add(row);
            }
            result[nombre]=rows;
            if(!await reader.NextResultAsync(ct)) break;
        }
        return result;
    }
    public static void Parametro(SqlCommand cmd,string name,SqlDbType type,object? value,int size=0)
    {
        var p=cmd.Parameters.Add(name,type);if(size>0)p.Size=size;p.Value=value ?? DBNull.Value;
    }
}
