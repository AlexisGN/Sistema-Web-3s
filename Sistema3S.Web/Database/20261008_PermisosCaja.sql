-- Sistema3S: permisos existentes para Caja. No modifica tablas, datos, triggers ni reglas financieras.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
/* =========================================================
   7. PROCEDIMIENTO: ABRIR CAJA
   ========================================================= */

CREATE OR ALTER PROCEDURE dbo.sp_AbrirCaja
    @IdUsuarioApertura INT,
    @SaldoInicial DECIMAL(18,2),
    @ObservacionApertura NVARCHAR(1000) = NULL
AS
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @BloqueoCaja INT;
    EXEC @BloqueoCaja=sys.sp_getapplock @Resource=N'Sistema3S:operaciones-caja', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
    IF @BloqueoCaja<0 THROW 72002, 'Caja está ocupada. Intenta nuevamente.', 1;
    SET NOCOUNT ON;

    DECLARE @IdEstadoAbierta INT;
    DECLARE @IdCaja INT;

    SET @SaldoInicial = ROUND(ISNULL(@SaldoInicial, 0), 2);
    SET @ObservacionApertura = NULLIF(LTRIM(RTRIM(ISNULL(@ObservacionApertura, ''))), '');

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Usuario u
        INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
        WHERE u.IdUsuario = @IdUsuarioApertura AND u.Estado = 1 AND r.Estado = 1
          AND (UPPER(LTRIM(RTRIM(r.Nombre))) IN ('ADMINISTRADOR', 'ADMIN')
               OR EXISTS (SELECT 1 FROM dbo.RolPermiso rp INNER JOIN dbo.Permiso p ON p.IdPermiso = rp.IdPermiso
                          WHERE rp.IdRol = u.IdRol AND p.Estado = 1 AND p.Nombre = N'CAJA_ABRIR'))
    )
        THROW 70001, 'Acceso denegado. No tienes permiso para esta operacion de caja.', 1;

    IF @SaldoInicial < 0
        THROW 70002, 'El saldo inicial no puede ser negativo.', 1;

    SELECT @IdEstadoAbierta = IdEstadoCaja
    FROM dbo.EstadoCaja
    WHERE Nombre = 'Abierta'
      AND Estado = 1;

    IF @IdEstadoAbierta IS NULL
        THROW 70003, 'No existe el estado de caja Abierta.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.Caja c
        INNER JOIN dbo.EstadoCaja ec ON ec.IdEstadoCaja = c.IdEstadoCaja
        WHERE ec.Nombre = 'Abierta'
          AND ec.Estado = 1
    )
    BEGIN
        THROW 70004, 'Ya existe una caja abierta. Debes cerrarla antes de abrir una nueva.', 1;
    END;

    INSERT INTO dbo.Caja (
        IdUsuarioApertura,
        IdUsuarioCierre,
        IdEstadoCaja,
        FechaApertura,
        FechaCierre,
        SaldoInicial,
        SaldoFinal,
        SaldoSistema,
        TotalIngresos,
        TotalEgresos,
        SaldoContado,
        Diferencia,
        ObservacionApertura,
        ObservacionCierre
    )
    VALUES (
        @IdUsuarioApertura,
        NULL,
        @IdEstadoAbierta,
        GETDATE(),
        NULL,
        @SaldoInicial,
        NULL,
        @SaldoInicial,
        0,
        0,
        NULL,
        NULL,
        @ObservacionApertura,
        NULL
    );

    SET @IdCaja = SCOPE_IDENTITY();

    SELECT
        c.IdCaja,
        c.IdUsuarioApertura,
        c.IdUsuarioCierre,
        c.IdEstadoCaja,
        ec.Nombre AS EstadoCaja,
        c.FechaApertura,
        c.FechaCierre,
        c.SaldoInicial,
        c.TotalIngresos,
        c.TotalEgresos,
        c.SaldoSistema,
        c.SaldoFinal,
        c.SaldoContado,
        c.Diferencia,
        c.ObservacionApertura,
        c.ObservacionCierre,
        'Caja abierta correctamente.' AS Mensaje
    FROM dbo.Caja c
    INNER JOIN dbo.EstadoCaja ec ON ec.IdEstadoCaja = c.IdEstadoCaja
    WHERE c.IdCaja = @IdCaja;
    COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/* =========================================================
   13. PROCEDIMIENTO: CERRAR CAJA
   ========================================================= */

CREATE OR ALTER PROCEDURE dbo.sp_CerrarCaja
    @IdUsuarioCierre INT,
    @IdCaja INT,
    @SaldoContado DECIMAL(18,2),
    @ObservacionCierre NVARCHAR(1000) = NULL
AS
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @BloqueoCaja INT;
    EXEC @BloqueoCaja=sys.sp_getapplock @Resource=N'Sistema3S:operaciones-caja', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
    IF @BloqueoCaja<0 THROW 72002, 'Caja está ocupada. Intenta nuevamente.', 1;
    SET NOCOUNT ON;

    DECLARE @IdEstadoCerrada INT;
    DECLARE @EstadoActual NVARCHAR(100);
    DECLARE @SaldoSistema DECIMAL(18,2);
    DECLARE @TotalIngresos DECIMAL(18,2);
    DECLARE @TotalEgresos DECIMAL(18,2);

    SET @SaldoContado = ROUND(ISNULL(@SaldoContado, 0), 2);
    SET @ObservacionCierre = NULLIF(LTRIM(RTRIM(ISNULL(@ObservacionCierre, ''))), '');

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Usuario u
        INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
        WHERE u.IdUsuario = @IdUsuarioCierre AND u.Estado = 1 AND r.Estado = 1
          AND (UPPER(LTRIM(RTRIM(r.Nombre))) IN ('ADMINISTRADOR', 'ADMIN')
               OR EXISTS (SELECT 1 FROM dbo.RolPermiso rp INNER JOIN dbo.Permiso p ON p.IdPermiso = rp.IdPermiso
                          WHERE rp.IdRol = u.IdRol AND p.Estado = 1 AND p.Nombre = N'CAJA_CERRAR'))
    )
        THROW 70601, 'Acceso denegado. No tienes permiso para esta operacion de caja.', 1;

    IF @IdCaja <= 0
        THROW 70602, 'Selecciona una caja válida.', 1;

    IF @SaldoContado < 0
        THROW 70603, 'El saldo contado no puede ser negativo.', 1;

    SELECT
        @EstadoActual = ec.Nombre
    FROM dbo.Caja c
    INNER JOIN dbo.EstadoCaja ec ON ec.IdEstadoCaja = c.IdEstadoCaja
    WHERE c.IdCaja = @IdCaja;

    IF @EstadoActual IS NULL
        THROW 70604, 'La caja seleccionada no existe.', 1;

    IF @EstadoActual <> 'Abierta'
        THROW 70605, 'Solo una caja abierta puede cerrarse.', 1;

    SELECT @IdEstadoCerrada = IdEstadoCaja
    FROM dbo.EstadoCaja
    WHERE Nombre = 'Cerrada'
      AND Estado = 1;

    IF @IdEstadoCerrada IS NULL
        THROW 70606, 'No existe el estado de caja Cerrada.', 1;

    SELECT
        @TotalIngresos = ISNULL(SUM(CASE
            WHEN tmc.Nombre IN ('Ingreso por venta', 'Ingreso manual', 'Ajuste ingreso')
            THEN mc.Monto ELSE 0 END), 0),
        @TotalEgresos = ISNULL(SUM(CASE
            WHEN tmc.Nombre IN ('Egreso por compra', 'Egreso manual', 'Ajuste egreso')
            THEN mc.Monto ELSE 0 END), 0)
    FROM dbo.MovimientoCaja mc
    INNER JOIN dbo.TipoMovimientoCaja tmc
        ON tmc.IdTipoMovimientoCaja = mc.IdTipoMovimientoCaja
    WHERE mc.IdCaja = @IdCaja
      AND mc.Estado = 1;

    SET @TotalIngresos = ROUND(ISNULL(@TotalIngresos, 0), 2);
    SET @TotalEgresos = ROUND(ISNULL(@TotalEgresos, 0), 2);

    SELECT @SaldoSistema = ROUND(SaldoInicial + @TotalIngresos - @TotalEgresos, 2)
    FROM dbo.Caja
    WHERE IdCaja = @IdCaja;

    UPDATE dbo.Caja
    SET
        IdUsuarioCierre = @IdUsuarioCierre,
        IdEstadoCaja = @IdEstadoCerrada,
        FechaCierre = GETDATE(),
        TotalIngresos = @TotalIngresos,
        TotalEgresos = @TotalEgresos,
        SaldoSistema = @SaldoSistema,
        SaldoFinal = @SaldoSistema,
        SaldoContado = @SaldoContado,
        Diferencia = ROUND(@SaldoContado - @SaldoSistema, 2),
        ObservacionCierre = @ObservacionCierre
    WHERE IdCaja = @IdCaja;

    SELECT
        c.IdCaja,
        c.IdUsuarioApertura,
        c.IdUsuarioCierre,
        c.IdEstadoCaja,
        ec.Nombre AS EstadoCaja,
        c.FechaApertura,
        c.FechaCierre,
        c.SaldoInicial,
        c.TotalIngresos,
        c.TotalEgresos,
        c.SaldoSistema,
        c.SaldoFinal,
        c.SaldoContado,
        c.Diferencia,
        c.ObservacionApertura,
        c.ObservacionCierre,
        'Caja cerrada correctamente.' AS Mensaje
    FROM dbo.Caja c
    INNER JOIN dbo.EstadoCaja ec ON ec.IdEstadoCaja = c.IdEstadoCaja
    WHERE c.IdCaja = @IdCaja;
    COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/* =========================================================
   12. PROCEDIMIENTO: LISTAR MOVIMIENTOS DE CAJA
   ========================================================= */

CREATE OR ALTER PROCEDURE dbo.sp_ListarMovimientosCaja
    @IdUsuario INT,
    @IdCaja INT = NULL,
    @FechaInicio DATE = NULL,
    @FechaFin DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Usuario u
        INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
        WHERE u.IdUsuario = @IdUsuario AND u.Estado = 1 AND r.Estado = 1
          AND (UPPER(LTRIM(RTRIM(r.Nombre))) IN ('ADMINISTRADOR', 'ADMIN')
               OR EXISTS (SELECT 1 FROM dbo.RolPermiso rp INNER JOIN dbo.Permiso p ON p.IdPermiso = rp.IdPermiso
                          WHERE rp.IdRol = u.IdRol AND p.Estado = 1 AND p.Nombre = N'CAJA_VER'))
    )
        THROW 70501, 'Acceso denegado. No tienes permiso para esta operacion de caja.', 1;

    IF @IdCaja IS NULL
    BEGIN
        SELECT TOP 1 @IdCaja = IdCaja
        FROM dbo.Caja
        ORDER BY IdCaja DESC;
    END;

    SELECT
        mc.IdMovimientoCaja,
        mc.IdCaja,
        mc.IdTipoMovimientoCaja,
        tmc.Nombre AS TipoMovimiento,
        mc.IdVenta,
        mc.IdCompra,
        mc.IdPagoVenta,
        mc.IdPagoCompra,
        mc.IdUsuarioRegistro,
        u.Correo AS UsuarioRegistro,
        mc.MetodoPago,
        mc.Monto,
        mc.Descripcion,
        mc.FechaMovimiento,
        mc.OrigenMovimiento,
        mc.EsAutomatico,
        mc.Estado,
        CASE
            WHEN tmc.Nombre IN ('Ingreso por venta', 'Ingreso manual', 'Ajuste ingreso')
            THEN mc.Monto ELSE 0
        END AS Ingreso,
        CASE
            WHEN tmc.Nombre IN ('Egreso por compra', 'Egreso manual', 'Ajuste egreso')
            THEN mc.Monto ELSE 0
        END AS Egreso
    FROM dbo.MovimientoCaja mc
    INNER JOIN dbo.TipoMovimientoCaja tmc
        ON tmc.IdTipoMovimientoCaja = mc.IdTipoMovimientoCaja
    INNER JOIN dbo.Usuario u
        ON u.IdUsuario = mc.IdUsuarioRegistro
    WHERE (@IdCaja IS NULL OR mc.IdCaja = @IdCaja)
      AND mc.Estado = 1
      AND (@FechaInicio IS NULL OR CONVERT(DATE, mc.FechaMovimiento) >= @FechaInicio)
      AND (@FechaFin IS NULL OR CONVERT(DATE, mc.FechaMovimiento) <= @FechaFin)
    ORDER BY mc.FechaMovimiento DESC, mc.IdMovimientoCaja DESC;
END;
GO

/* =========================================================
   8. PROCEDIMIENTO: OBTENER CAJA ACTIVA
   ========================================================= */

CREATE OR ALTER PROCEDURE dbo.sp_ObtenerCajaActiva
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Usuario u
        INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
        WHERE u.IdUsuario = @IdUsuario AND u.Estado = 1 AND r.Estado = 1
          AND (UPPER(LTRIM(RTRIM(r.Nombre))) IN ('ADMINISTRADOR', 'ADMIN')
               OR EXISTS (SELECT 1 FROM dbo.RolPermiso rp INNER JOIN dbo.Permiso p ON p.IdPermiso = rp.IdPermiso
                          WHERE rp.IdRol = u.IdRol AND p.Estado = 1 AND p.Nombre = N'CAJA_VER'))
    )
        THROW 70101, 'Acceso denegado. No tienes permiso para esta operacion de caja.', 1;

    SELECT TOP 1
        c.IdCaja,
        c.IdUsuarioApertura,
        ua.Correo AS UsuarioApertura,
        c.IdUsuarioCierre,
        uc.Correo AS UsuarioCierre,
        c.IdEstadoCaja,
        ec.Nombre AS EstadoCaja,
        c.FechaApertura,
        c.FechaCierre,
        c.SaldoInicial,
        c.TotalIngresos,
        c.TotalEgresos,
        c.SaldoSistema,
        c.SaldoFinal,
        c.SaldoContado,
        c.Diferencia,
        c.ObservacionApertura,
        c.ObservacionCierre
    FROM dbo.Caja c
    INNER JOIN dbo.EstadoCaja ec ON ec.IdEstadoCaja = c.IdEstadoCaja
    INNER JOIN dbo.Usuario ua ON ua.IdUsuario = c.IdUsuarioApertura
    LEFT JOIN dbo.Usuario uc ON uc.IdUsuario = c.IdUsuarioCierre
    WHERE ec.Nombre = 'Abierta'
      AND ec.Estado = 1
    ORDER BY c.IdCaja DESC;
END;
GO

/* =========================================================
   11. PROCEDIMIENTO: RESUMEN DE CAJA
   ========================================================= */

CREATE OR ALTER PROCEDURE dbo.sp_ObtenerResumenCaja
    @IdUsuario INT,
    @IdCaja INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Usuario u
        INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
        WHERE u.IdUsuario = @IdUsuario AND u.Estado = 1 AND r.Estado = 1
          AND (UPPER(LTRIM(RTRIM(r.Nombre))) IN ('ADMINISTRADOR', 'ADMIN')
               OR EXISTS (SELECT 1 FROM dbo.RolPermiso rp INNER JOIN dbo.Permiso p ON p.IdPermiso = rp.IdPermiso
                          WHERE rp.IdRol = u.IdRol AND p.Estado = 1 AND p.Nombre = N'CAJA_VER'))
    )
        THROW 70401, 'Acceso denegado. No tienes permiso para esta operacion de caja.', 1;

    IF @IdCaja IS NULL
    BEGIN
        SELECT TOP 1 @IdCaja = c.IdCaja
        FROM dbo.Caja c
        ORDER BY c.IdCaja DESC;
    END;

    IF @IdCaja IS NULL
        THROW 70402, 'No existe una caja registrada.', 1;

    ;WITH Movimientos AS (
        SELECT
            mc.IdCaja,
            tmc.Nombre AS TipoMovimiento,
            mc.Monto,
            mc.Estado
        FROM dbo.MovimientoCaja mc
        INNER JOIN dbo.TipoMovimientoCaja tmc
            ON tmc.IdTipoMovimientoCaja = mc.IdTipoMovimientoCaja
        WHERE mc.IdCaja = @IdCaja
          AND mc.Estado = 1
    )
    SELECT
        c.IdCaja,
        c.IdEstadoCaja,
        ec.Nombre AS EstadoCaja,
        c.FechaApertura,
        c.FechaCierre,
        c.SaldoInicial,

        ISNULL(SUM(CASE WHEN m.TipoMovimiento = 'Ingreso por venta' THEN m.Monto ELSE 0 END), 0) AS IngresosPorVenta,
        ISNULL(SUM(CASE WHEN m.TipoMovimiento = 'Ingreso manual' THEN m.Monto ELSE 0 END), 0) AS IngresosManuales,
        ISNULL(SUM(CASE WHEN m.TipoMovimiento = 'Ajuste ingreso' THEN m.Monto ELSE 0 END), 0) AS AjustesIngreso,

        ISNULL(SUM(CASE WHEN m.TipoMovimiento = 'Egreso por compra' THEN m.Monto ELSE 0 END), 0) AS EgresosPorCompra,
        ISNULL(SUM(CASE WHEN m.TipoMovimiento = 'Egreso manual' THEN m.Monto ELSE 0 END), 0) AS EgresosManuales,
        ISNULL(SUM(CASE WHEN m.TipoMovimiento = 'Ajuste egreso' THEN m.Monto ELSE 0 END), 0) AS AjustesEgreso,

        ISNULL(SUM(CASE
            WHEN m.TipoMovimiento IN ('Ingreso por venta', 'Ingreso manual', 'Ajuste ingreso')
            THEN m.Monto ELSE 0 END), 0) AS TotalIngresos,

        ISNULL(SUM(CASE
            WHEN m.TipoMovimiento IN ('Egreso por compra', 'Egreso manual', 'Ajuste egreso')
            THEN m.Monto ELSE 0 END), 0) AS TotalEgresos,

        ROUND(
            c.SaldoInicial
            + ISNULL(SUM(CASE
                WHEN m.TipoMovimiento IN ('Ingreso por venta', 'Ingreso manual', 'Ajuste ingreso')
                THEN m.Monto ELSE 0 END), 0)
            - ISNULL(SUM(CASE
                WHEN m.TipoMovimiento IN ('Egreso por compra', 'Egreso manual', 'Ajuste egreso')
                THEN m.Monto ELSE 0 END), 0),
            2
        ) AS SaldoSistema,

        c.SaldoContado,
        c.Diferencia,
        c.ObservacionApertura,
        c.ObservacionCierre
    FROM dbo.Caja c
    INNER JOIN dbo.EstadoCaja ec ON ec.IdEstadoCaja = c.IdEstadoCaja
    LEFT JOIN Movimientos m ON m.IdCaja = c.IdCaja
    WHERE c.IdCaja = @IdCaja
    GROUP BY
        c.IdCaja,
        c.IdEstadoCaja,
        ec.Nombre,
        c.FechaApertura,
        c.FechaCierre,
        c.SaldoInicial,
        c.SaldoContado,
        c.Diferencia,
        c.ObservacionApertura,
        c.ObservacionCierre;
END;
GO

/* =========================================================
   10. PROCEDIMIENTO: REGISTRAR MOVIMIENTO MANUAL
   ========================================================= */

CREATE OR ALTER PROCEDURE dbo.sp_RegistrarMovimientoCajaManual
    @IdUsuarioRegistro INT,
    @TipoMovimiento NVARCHAR(100),
    @MetodoPago NVARCHAR(100),
    @Monto DECIMAL(18,2),
    @Descripcion NVARCHAR(600)
AS
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @BloqueoCaja INT;
    EXEC @BloqueoCaja=sys.sp_getapplock @Resource=N'Sistema3S:operaciones-caja', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
    IF @BloqueoCaja<0 THROW 72002, 'Caja está ocupada. Intenta nuevamente.', 1;
    SET NOCOUNT ON;

    DECLARE @IdCaja INT;
    DECLARE @IdTipoMovimientoCaja INT;

    SET @TipoMovimiento = LTRIM(RTRIM(ISNULL(@TipoMovimiento, '')));
    SET @MetodoPago = NULLIF(LTRIM(RTRIM(ISNULL(@MetodoPago, ''))), '');
    SET @Descripcion = NULLIF(LTRIM(RTRIM(ISNULL(@Descripcion, ''))), '');
    SET @Monto = ROUND(ISNULL(@Monto, 0), 2);

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Usuario u
        INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
        WHERE u.IdUsuario = @IdUsuarioRegistro AND u.Estado = 1 AND r.Estado = 1
          AND (UPPER(LTRIM(RTRIM(r.Nombre))) IN ('ADMINISTRADOR', 'ADMIN')
               OR EXISTS (SELECT 1 FROM dbo.RolPermiso rp INNER JOIN dbo.Permiso p ON p.IdPermiso = rp.IdPermiso
                          WHERE rp.IdRol = u.IdRol AND p.Estado = 1 AND p.Nombre = N'CAJA_MOVIMIENTO_MANUAL'))
    )
        THROW 70301, 'Acceso denegado. No tienes permiso para esta operacion de caja.', 1;

    IF @TipoMovimiento NOT IN ('Ingreso manual', 'Egreso manual', 'Ajuste ingreso', 'Ajuste egreso')
        THROW 70302, 'Selecciona un tipo de movimiento manual válido.', 1;

    IF @Monto <= 0
        THROW 70303, 'El monto debe ser mayor a 0.', 1;

    IF @Descripcion IS NULL
        THROW 70304, 'Ingresa una descripción para el movimiento manual.', 1;

    SELECT @IdCaja = c.IdCaja
    FROM dbo.Caja c
    INNER JOIN dbo.EstadoCaja ec ON ec.IdEstadoCaja = c.IdEstadoCaja
    WHERE ec.Nombre = 'Abierta'
      AND ec.Estado = 1;

    IF @IdCaja IS NULL
        THROW 70305, 'No existe una caja abierta.', 1;

    SELECT @IdTipoMovimientoCaja = IdTipoMovimientoCaja
    FROM dbo.TipoMovimientoCaja
    WHERE Nombre = @TipoMovimiento
      AND Estado = 1;

    IF @IdTipoMovimientoCaja IS NULL
        THROW 70306, 'No existe el tipo de movimiento seleccionado.', 1;

    INSERT INTO dbo.MovimientoCaja (
        IdCaja,
        IdTipoMovimientoCaja,
        IdVenta,
        IdCompra,
        IdUsuarioRegistro,
        Monto,
        Descripcion,
        FechaMovimiento,
        IdPagoVenta,
        IdPagoCompra,
        MetodoPago,
        OrigenMovimiento,
        EsAutomatico,
        Estado
    )
    VALUES (
        @IdCaja,
        @IdTipoMovimientoCaja,
        NULL,
        NULL,
        @IdUsuarioRegistro,
        @Monto,
        @Descripcion,
        GETDATE(),
        NULL,
        NULL,
        @MetodoPago,
        'Manual',
        0,
        1
    );

    UPDATE c
    SET
        TotalIngresos = ISNULL((
            SELECT SUM(mc.Monto)
            FROM dbo.MovimientoCaja mc
            INNER JOIN dbo.TipoMovimientoCaja tmc
                ON tmc.IdTipoMovimientoCaja = mc.IdTipoMovimientoCaja
            WHERE mc.IdCaja = c.IdCaja
              AND mc.Estado = 1
              AND tmc.Nombre IN ('Ingreso por venta', 'Ingreso manual', 'Ajuste ingreso')
        ), 0),
        TotalEgresos = ISNULL((
            SELECT SUM(mc.Monto)
            FROM dbo.MovimientoCaja mc
            INNER JOIN dbo.TipoMovimientoCaja tmc
                ON tmc.IdTipoMovimientoCaja = mc.IdTipoMovimientoCaja
            WHERE mc.IdCaja = c.IdCaja
              AND mc.Estado = 1
              AND tmc.Nombre IN ('Egreso por compra', 'Egreso manual', 'Ajuste egreso')
        ), 0)
    FROM dbo.Caja c
    WHERE c.IdCaja = @IdCaja;

    UPDATE dbo.Caja
    SET SaldoSistema = ROUND(SaldoInicial + TotalIngresos - TotalEgresos, 2)
    WHERE IdCaja = @IdCaja;

    SELECT
        'Movimiento registrado correctamente.' AS Mensaje,
        @IdCaja AS IdCaja;
    COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/* =========================================================
   14. PROCEDIMIENTO: REPORTE DE CAJA
   ========================================================= */

CREATE OR ALTER PROCEDURE dbo.sp_ReporteCaja
    @IdUsuario INT,
    @FechaInicio DATE = NULL,
    @FechaFin DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Usuario u
        INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
        WHERE u.IdUsuario = @IdUsuario AND u.Estado = 1 AND r.Estado = 1
          AND (UPPER(LTRIM(RTRIM(r.Nombre))) IN ('ADMINISTRADOR', 'ADMIN')
               OR EXISTS (SELECT 1 FROM dbo.RolPermiso rp INNER JOIN dbo.Permiso p ON p.IdPermiso = rp.IdPermiso
                          WHERE rp.IdRol = u.IdRol AND p.Estado = 1 AND p.Nombre = N'CAJA_REPORTE'))
    )
        THROW 70701, 'Acceso denegado. No tienes permiso para esta operacion de caja.', 1;

    SELECT
        c.IdCaja,
        ec.Nombre AS EstadoCaja,
        c.FechaApertura,
        c.FechaCierre,
        c.SaldoInicial,
        c.TotalIngresos,
        c.TotalEgresos,
        c.SaldoSistema,
        c.SaldoFinal,
        c.SaldoContado,
        c.Diferencia,
        ua.Correo AS UsuarioApertura,
        uc.Correo AS UsuarioCierre,
        c.ObservacionApertura,
        c.ObservacionCierre
    FROM dbo.Caja c
    INNER JOIN dbo.EstadoCaja ec ON ec.IdEstadoCaja = c.IdEstadoCaja
    INNER JOIN dbo.Usuario ua ON ua.IdUsuario = c.IdUsuarioApertura
    LEFT JOIN dbo.Usuario uc ON uc.IdUsuario = c.IdUsuarioCierre
    WHERE (@FechaInicio IS NULL OR CONVERT(DATE, c.FechaApertura) >= @FechaInicio)
      AND (@FechaFin IS NULL OR CONVERT(DATE, c.FechaApertura) <= @FechaFin)
    ORDER BY c.IdCaja DESC;
END;
GO
