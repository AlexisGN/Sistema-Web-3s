namespace Sistema3S.Web.DTOs.Bi;

public sealed class BiFiltro
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public DateOnly? CompararDesde { get; set; }
    public DateOnly? CompararHasta { get; set; }
    public string? ModoComparacion { get; set; }
    public string ComparacionSeleccionada => ModoComparacion ?? (CompararDesde.HasValue || CompararHasta.HasValue ? "personalizado" : "ninguna");
    public bool ComparacionActiva => ComparacionSeleccionada != "ninguna";
    public string Agrupacion { get; set; } = "mes";
    public string Procedencia { get; set; } = "operativo";
    public int? Categoria { get; set; }
    public int? Elemento { get; set; }
    public int? CompararElemento { get; set; }
    public int? Cliente { get; set; }
    public int? Proveedor { get; set; }
    public string? Estado { get; set; }
    public int Pagina { get; set; } = 1;
    public int Tamanio { get; set; } = 20;

    public void Validar(DateOnly hoy)
    {
        if (Desde.Year < 1900 || Hasta.Year > 9998 || Hasta < Desde || Hasta > hoy || Hasta.DayNumber-Desde.DayNumber > 1826)
            throw new ArgumentException("Selecciona un periodo válido de hasta cinco años que no termine en el futuro.");
        if (Agrupacion is not ("dia" or "semana" or "mes" or "anio")) throw new ArgumentException("Selecciona una agrupación válida.");
        if (Agrupacion=="dia" && Hasta.DayNumber-Desde.DayNumber>366) throw new ArgumentException("Para más de un año utiliza semanas, meses o años.");
        if (Procedencia is not ("operativo" or "referencia")) throw new ArgumentException("Selecciona una procedencia válida.");
        if (Pagina < 1 || Pagina>100000 || Tamanio<1 || Tamanio>100) throw new ArgumentException("La página debe ser válida y contener hasta 100 registros.");
        if (ComparacionSeleccionada is not ("ninguna" or "anterior" or "anioAnterior" or "personalizado"))
            throw new ArgumentException("Selecciona un modo de comparación válido.");
        if (ComparacionSeleccionada=="personalizado" && (!CompararDesde.HasValue || !CompararHasta.HasValue || CompararHasta > hoy || CompararHasta < CompararDesde))
            throw new ArgumentException("Completa un periodo de comparación válido.");
        if (ComparacionSeleccionada=="personalizado" && CompararHasta!.Value.DayNumber-CompararDesde!.Value.DayNumber != Hasta.DayNumber-Desde.DayNumber)
            throw new ArgumentException("La comparación debe tener el mismo número de días que el periodo seleccionado.");
        if (ComparacionActiva && (Comparacion().Desde.Year<1900 || Comparacion().Hasta>hoy)) throw new ArgumentException("Selecciona una comparación entre 1900 y la fecha actual.");
        if (CompararElemento.HasValue && !Elemento.HasValue) throw new ArgumentException("Selecciona el primer producto antes de compararlo con otro.");
        if (CompararElemento.HasValue && CompararElemento==Elemento) throw new ArgumentException("Selecciona dos productos o servicios diferentes.");
        if (Estado is not (null or "" or "Confirmada" or "Registrada" or "Anulada" or "Pendiente" or "Respondida" or "Aprobada" or "Cancelada" or "Convertida en venta" or "Convertida a venta"))
            throw new ArgumentException("Selecciona un estado comercial válido.");
    }
    public (DateOnly Desde, DateOnly Hasta) Comparacion() => ComparacionSeleccionada=="anioAnterior"
        ? (Desde.AddYears(-1),Desde.AddYears(-1).AddDays(Hasta.DayNumber-Desde.DayNumber))
        : ComparacionSeleccionada=="personalizado" && CompararDesde.HasValue
        ? (CompararDesde.Value, CompararHasta!.Value)
        : (Desde.AddDays(-(Hasta.DayNumber-Desde.DayNumber+1)), Desde.AddDays(-1));
}
