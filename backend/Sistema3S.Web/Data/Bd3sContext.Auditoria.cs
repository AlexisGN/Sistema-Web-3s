using Microsoft.EntityFrameworkCore;

namespace Sistema3S.Web.Data;

public partial class Bd3sContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // SQL Server no admite OUTPUT sin INTO en tablas con triggers habilitados.
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            entity.UseSqlOutputClause(false);
    }
}
