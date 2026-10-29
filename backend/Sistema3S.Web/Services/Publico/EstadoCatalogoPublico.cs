namespace Sistema3S.Web.Services.Publico;

// Una nueva generación impide reutilizar respuestas anteriores incluso si una lectura seguía en curso al editar.
public sealed class EstadoCatalogoPublico
{
    public const string Politica = "catalogo-publico";
    public const string Etiqueta = "catalogo-publico";
    private long version;
    public long Version => Volatile.Read(ref version);
    public void Cambiar() => Interlocked.Increment(ref version);
}
