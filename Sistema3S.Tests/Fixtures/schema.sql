SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON;
GO
CREATE TYPE dbo.[CompraDetalleType] AS TABLE ([IdProducto] int NOT NULL,
[Cantidad] int NOT NULL,
[PrecioCompra] decimal(18,2) NOT NULL);
GO
CREATE TYPE dbo.[CotizacionDetalleType] AS TABLE ([IdElementoCatalogo] int NOT NULL,
[Cantidad] int NOT NULL,
[PrecioUnitario] decimal(18,2) NOT NULL,
[Observacion] nvarchar(600) NULL);
GO
CREATE TYPE dbo.[CotizacionWebDetalleType] AS TABLE ([IdProducto] int NOT NULL,
[Cantidad] int NOT NULL,
[Observacion] nvarchar(1000) NULL);
GO
CREATE TYPE dbo.[VentaDetalleType] AS TABLE ([IdElementoCatalogo] int NOT NULL,
[Cantidad] int NOT NULL,
[PrecioUnitario] decimal(18,2) NOT NULL);
GO
CREATE TABLE dbo.[AceptacionDocumentoLegal] ([IdAceptacionDocumentoLegal] int IDENTITY(1,1) NOT NULL,
[IdUsuario] int NOT NULL,
[IdDocumentoLegal] int NOT NULL,
[FechaAceptacion] datetime NOT NULL DEFAULT (getdate()),
[IpAceptacion] nvarchar(50) NULL,
PRIMARY KEY ([IdAceptacionDocumentoLegal]));
GO
CREATE TABLE dbo.[AlertaStock] ([IdAlertaStock] int IDENTITY(1,1) NOT NULL,
[IdProducto] int NOT NULL,
[IdEstadoAlertaStock] int NOT NULL,
[FechaAlerta] datetime NOT NULL DEFAULT (getdate()),
[Mensaje] nvarchar(300) NOT NULL,
PRIMARY KEY ([IdAlertaStock]));
GO
CREATE TABLE dbo.[AuditoriaLog] ([IdLog] int IDENTITY(1,1) NOT NULL,
[IdUsuario] int NULL,
[Accion] nvarchar(100) NOT NULL,
[TablaAfectada] nvarchar(100) NOT NULL,
[IdRegistro] int NULL,
[Fecha] datetime NOT NULL DEFAULT (getdate()),
[Descripcion] nvarchar(500) NULL,
[DatosAntes] nvarchar(max) NULL,
[DatosDespues] nvarchar(max) NULL,
[Solicitud] nvarchar(100) NULL,
[Origen] nvarchar(128) NULL,
PRIMARY KEY ([IdLog]));
GO
CREATE TABLE dbo.[Caja] ([IdCaja] int IDENTITY(1,1) NOT NULL,
[IdUsuarioApertura] int NOT NULL,
[IdUsuarioCierre] int NULL,
[IdEstadoCaja] int NOT NULL,
[FechaApertura] datetime NOT NULL DEFAULT (getdate()),
[FechaCierre] datetime NULL,
[SaldoInicial] decimal(18,2) NOT NULL DEFAULT ((0)),
[SaldoFinal] decimal(18,2) NULL,
[SaldoSistema] decimal(18,2) NOT NULL DEFAULT ((0)),
[TotalIngresos] decimal(18,2) NOT NULL DEFAULT ((0)),
[TotalEgresos] decimal(18,2) NOT NULL DEFAULT ((0)),
[SaldoContado] decimal(18,2) NULL,
[Diferencia] decimal(18,2) NULL,
[ObservacionApertura] nvarchar(1000) NULL,
[ObservacionCierre] nvarchar(1000) NULL,
PRIMARY KEY ([IdCaja]));
GO
CREATE TABLE dbo.[Categoria] ([IdCategoria] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(100) NOT NULL,
[Descripcion] nvarchar(300) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdCategoria]));
GO
CREATE TABLE dbo.[Cliente] ([IdCliente] int IDENTITY(1,1) NOT NULL,
[IdUsuario] int NULL,
[IdTipoCliente] int NOT NULL,
[IdTipoDocumento] int NOT NULL,
[IdUbigeo] int NULL,
[NumeroDocumento] nvarchar(20) NOT NULL,
[Correo] nvarchar(150) NULL,
[Telefono] nvarchar(20) NULL,
[Direccion] nvarchar(250) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
[FechaRegistro] datetime NOT NULL DEFAULT (getdate()),
PRIMARY KEY ([IdCliente]));
GO
CREATE TABLE dbo.[ClienteEmpresa] ([IdCliente] int NOT NULL,
[RazonSocial] nvarchar(200) NOT NULL,
[NombreComercial] nvarchar(200) NULL,
PRIMARY KEY ([IdCliente]));
GO
CREATE TABLE dbo.[ClientePersonaNatural] ([IdCliente] int NOT NULL,
[Nombres] nvarchar(100) NOT NULL,
[ApellidoPaterno] nvarchar(100) NOT NULL,
[ApellidoMaterno] nvarchar(100) NULL,
PRIMARY KEY ([IdCliente]));
GO
CREATE TABLE dbo.[ClienteRecuperacion] ([TokenHash] varchar(64) NOT NULL,
[IdUsuario] int NOT NULL,
[VersionClave] varchar(64) NOT NULL,
[CreadoUtc] datetime2 NOT NULL,
[ExpiraUtc] datetime2 NOT NULL,
PRIMARY KEY ([TokenHash]));
GO
CREATE TABLE dbo.[Compra] ([IdCompra] int IDENTITY(1,1) NOT NULL,
[IdProveedor] int NOT NULL,
[IdUsuarioRegistro] int NOT NULL,
[IdEstadoCompra] int NOT NULL,
[FechaCompra] datetime NOT NULL DEFAULT (getdate()),
[Total] decimal(18,2) NOT NULL DEFAULT ((0)),
[TipoComprobanteProveedor] nvarchar(50) NULL,
[SerieComprobante] nvarchar(20) NULL,
[NumeroComprobante] nvarchar(30) NULL,
[FechaEmisionComprobante] date NULL,
[Subtotal] decimal(18,2) NOT NULL DEFAULT ((0)),
[Igv] decimal(18,2) NOT NULL DEFAULT ((0)),
[TotalPagado] decimal(18,2) NOT NULL DEFAULT ((0)),
[SaldoPendiente] decimal(18,2) NOT NULL DEFAULT ((0)),
[IdEstadoPagoCompra] int NULL,
[Observacion] nvarchar(500) NULL,
[FechaRegistro] datetime NOT NULL DEFAULT (getdate()),
[ObservacionCompra] nvarchar(500) NULL,
PRIMARY KEY ([IdCompra]));
GO
CREATE TABLE dbo.[Comprobante] ([IdComprobante] int IDENTITY(1,1) NOT NULL,
[IdVenta] int NOT NULL,
[IdTipoComprobante] int NOT NULL,
[IdEstadoComprobante] int NOT NULL,
[Serie] nvarchar(10) NOT NULL,
[Numero] nvarchar(20) NOT NULL,
[FechaEmision] datetime NOT NULL DEFAULT (getdate()),
[ArchivoPdf] nvarchar(300) NULL,
[ArchivoXml] nvarchar(300) NULL,
[IdSerieComprobante] int NULL,
PRIMARY KEY ([IdComprobante]));
GO
CREATE TABLE dbo.[ConsultaExterna] ([IdConsultaExterna] int IDENTITY(1,1) NOT NULL,
[IdTipoServicioExterno] int NOT NULL,
[IdUsuarioRegistro] int NULL,
[IdCliente] int NULL,
[NumeroConsultado] nvarchar(30) NOT NULL,
[FechaConsulta] datetime NOT NULL DEFAULT (getdate()),
[Exitoso] bit NOT NULL DEFAULT ((0)),
[ResultadoJson] nvarchar(max) NULL,
[MensajeError] nvarchar(500) NULL,
PRIMARY KEY ([IdConsultaExterna]));
GO
CREATE TABLE dbo.[ContactoCliente] ([IdContactoCliente] int IDENTITY(1,1) NOT NULL,
[IdCliente] int NOT NULL,
[Nombres] nvarchar(100) NOT NULL,
[ApellidoPaterno] nvarchar(100) NOT NULL,
[ApellidoMaterno] nvarchar(100) NULL,
[Cargo] nvarchar(100) NULL,
[Correo] nvarchar(150) NULL,
[Telefono] nvarchar(20) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdContactoCliente]));
GO
CREATE TABLE dbo.[ContactoProveedor] ([IdContactoProveedor] int IDENTITY(1,1) NOT NULL,
[IdProveedor] int NOT NULL,
[Nombres] nvarchar(100) NOT NULL,
[ApellidoPaterno] nvarchar(100) NOT NULL,
[ApellidoMaterno] nvarchar(100) NULL,
[Cargo] nvarchar(100) NULL,
[Correo] nvarchar(150) NULL,
[Telefono] nvarchar(20) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdContactoProveedor]));
GO
CREATE TABLE dbo.[Cotizacion] ([IdCotizacion] int IDENTITY(1,1) NOT NULL,
[IdCliente] int NOT NULL,
[IdUsuarioRegistro] int NULL,
[IdUsuarioAtencion] int NULL,
[IdEstadoCotizacion] int NOT NULL,
[FechaCotizacion] datetime NOT NULL DEFAULT (getdate()),
[TotalReferencial] decimal(18,2) NOT NULL DEFAULT ((0)),
[Observacion] nvarchar(500) NULL,
[ArchivoPdf] nvarchar(300) NULL,
[CorreoEnviado] bit NOT NULL DEFAULT ((0)),
[OrigenCotizacion] nvarchar(30) NOT NULL DEFAULT ('Manual'),
[Subtotal] decimal(18,2) NOT NULL DEFAULT ((0)),
[Descuento] decimal(18,2) NOT NULL DEFAULT ((0)),
[Igv] decimal(18,2) NOT NULL DEFAULT ((0)),
[Total] decimal(18,2) NOT NULL DEFAULT ((0)),
[WhatsappEnviado] bit NOT NULL DEFAULT ((0)),
[FechaRespuesta] datetime NULL,
[CanalRespuesta] nvarchar(30) NULL,
[FechaActualizacion] datetime NULL,
[IdVentaGenerada] int NULL,
PRIMARY KEY ([IdCotizacion]));
GO
CREATE TABLE dbo.[CuotaCompra] ([IdCuotaCompra] int IDENTITY(1,1) NOT NULL,
[IdCompra] int NOT NULL,
[NumeroCuota] int NOT NULL,
[FechaVencimiento] date NOT NULL,
[MontoCuota] decimal(18,2) NOT NULL,
[MontoPagado] decimal(18,2) NOT NULL DEFAULT ((0)),
[EstadoCuota] nvarchar(30) NOT NULL DEFAULT ('Pendiente'),
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdCuotaCompra]));
GO
CREATE TABLE dbo.[CuotaVenta] ([IdCuotaVenta] int IDENTITY(1,1) NOT NULL,
[IdVenta] int NOT NULL,
[NumeroCuota] int NOT NULL,
[FechaVencimiento] date NOT NULL,
[MontoCuota] decimal(18,2) NOT NULL,
[MontoPagado] decimal(18,2) NOT NULL DEFAULT ((0)),
[EstadoCuota] nvarchar(30) NOT NULL DEFAULT ('Pendiente'),
PRIMARY KEY ([IdCuotaVenta]));
GO
CREATE TABLE dbo.[DetalleCompra] ([IdDetalleCompra] int IDENTITY(1,1) NOT NULL,
[IdCompra] int NOT NULL,
[IdProducto] int NOT NULL,
[Cantidad] int NOT NULL,
[PrecioCompra] decimal(18,2) NOT NULL,
[Subtotal] decimal(18,2) NOT NULL,
PRIMARY KEY ([IdDetalleCompra]));
GO
CREATE TABLE dbo.[DetalleCotizacion] ([IdDetalleCotizacion] int IDENTITY(1,1) NOT NULL,
[IdCotizacion] int NOT NULL,
[IdElementoCatalogo] int NOT NULL,
[Cantidad] int NOT NULL,
[PrecioUnitario] decimal(18,2) NOT NULL DEFAULT ((0)),
[Subtotal] decimal(18,2) NOT NULL DEFAULT ((0)),
[Observacion] nvarchar(300) NULL,
PRIMARY KEY ([IdDetalleCotizacion]));
GO
CREATE TABLE dbo.[DetalleVenta] ([IdDetalleVenta] int IDENTITY(1,1) NOT NULL,
[IdVenta] int NOT NULL,
[IdElementoCatalogo] int NOT NULL,
[Cantidad] int NOT NULL,
[PrecioUnitario] decimal(18,2) NOT NULL,
[Subtotal] decimal(18,2) NOT NULL,
PRIMARY KEY ([IdDetalleVenta]));
GO
CREATE TABLE dbo.[DocumentoLegal] ([IdDocumentoLegal] int IDENTITY(1,1) NOT NULL,
[IdTipoDocumentoLegal] int NOT NULL,
[Titulo] nvarchar(150) NOT NULL,
[Contenido] nvarchar(max) NOT NULL,
[Version] nvarchar(20) NOT NULL,
[FechaPublicacion] datetime NOT NULL DEFAULT (getdate()),
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdDocumentoLegal]));
GO
CREATE TABLE dbo.[ElementoCatalogo] ([IdElementoCatalogo] int IDENTITY(1,1) NOT NULL,
[IdTipoElemento] int NOT NULL,
[Nombre] nvarchar(150) NOT NULL,
[Descripcion] nvarchar(max) NULL,
[PrecioReferencial] decimal(18,2) NULL,
[ImagenUrl] nvarchar(300) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
[FechaRegistro] datetime NOT NULL DEFAULT (getdate()),
PRIMARY KEY ([IdElementoCatalogo]));
GO
CREATE TABLE dbo.[ElementoCatalogoSector] ([IdElementoCatalogo] int NOT NULL,
[IdSectorIndustrial] int NOT NULL,
PRIMARY KEY ([IdElementoCatalogo],[IdSectorIndustrial]));
GO
CREATE TABLE dbo.[Empresa] ([IdEmpresa] int IDENTITY(1,1) NOT NULL,
[Ruc] nvarchar(20) NULL,
[RazonSocial] nvarchar(200) NOT NULL,
[NombreComercial] nvarchar(200) NOT NULL,
[Rubro] nvarchar(200) NULL,
[ActividadPrincipal] nvarchar(max) NULL,
[Mision] nvarchar(max) NULL,
[Vision] nvarchar(max) NULL,
[Correo] nvarchar(150) NULL,
[Telefono] nvarchar(20) NULL,
[Direccion] nvarchar(300) NULL,
[SitioWeb] nvarchar(200) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdEmpresa]));
GO
CREATE TABLE dbo.[EnvioCorreo] ([IdEnvioCorreo] int IDENTITY(1,1) NOT NULL,
[IdCotizacion] int NULL,
[IdComprobante] int NULL,
[IdUsuarioRegistro] int NULL,
[Destinatario] nvarchar(150) NOT NULL,
[Asunto] nvarchar(200) NOT NULL,
[Cuerpo] nvarchar(max) NULL,
[FechaEnvio] datetime NOT NULL DEFAULT (getdate()),
[Exitoso] bit NOT NULL DEFAULT ((0)),
[MensajeError] nvarchar(500) NULL,
PRIMARY KEY ([IdEnvioCorreo]));
GO
CREATE TABLE dbo.[EstadoAlertaStock] ([IdEstadoAlertaStock] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(50) NOT NULL,
[Descripcion] nvarchar(250) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdEstadoAlertaStock]));
GO
CREATE TABLE dbo.[EstadoCaja] ([IdEstadoCaja] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(50) NOT NULL,
[Descripcion] nvarchar(250) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdEstadoCaja]));
GO
CREATE TABLE dbo.[EstadoCompra] ([IdEstadoCompra] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(50) NOT NULL,
[Descripcion] nvarchar(250) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdEstadoCompra]));
GO
CREATE TABLE dbo.[EstadoComprobante] ([IdEstadoComprobante] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(50) NOT NULL,
[Descripcion] nvarchar(250) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdEstadoComprobante]));
GO
CREATE TABLE dbo.[EstadoCotizacion] ([IdEstadoCotizacion] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(50) NOT NULL,
[Descripcion] nvarchar(250) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdEstadoCotizacion]));
GO
CREATE TABLE dbo.[EstadoPagoCompra] ([IdEstadoPagoCompra] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(50) NOT NULL,
[Descripcion] nvarchar(250) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdEstadoPagoCompra]));
GO
CREATE TABLE dbo.[EstadoVenta] ([IdEstadoVenta] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(50) NOT NULL,
[Descripcion] nvarchar(250) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdEstadoVenta]));
GO
CREATE TABLE dbo.[GuiaRemisionCompra] ([IdGuiaRemisionCompra] int IDENTITY(1,1) NOT NULL,
[IdCompra] int NOT NULL,
[NumeroGuia] nvarchar(50) NOT NULL,
[FechaEmision] date NOT NULL,
[FechaTraslado] date NOT NULL,
[PuntoPartida] nvarchar(250) NOT NULL,
[PuntoLlegada] nvarchar(250) NOT NULL,
[Transportista] nvarchar(200) NULL,
[RucTransportista] nvarchar(20) NULL,
[PlacaVehiculo] nvarchar(20) NULL,
[Observacion] nvarchar(500) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdGuiaRemisionCompra]));
GO
CREATE TABLE dbo.[ImagenElementoCatalogo] ([IdImagenElementoCatalogo] int IDENTITY(1,1) NOT NULL,
[IdElementoCatalogo] int NOT NULL,
[UrlImagen] nvarchar(300) NOT NULL,
[TextoAlternativo] nvarchar(150) NULL,
[EsPrincipal] bit NOT NULL DEFAULT ((0)),
[Estado] bit NOT NULL DEFAULT ((1)),
[FechaRegistro] datetime NOT NULL DEFAULT (getdate()),
PRIMARY KEY ([IdImagenElementoCatalogo]));
GO
CREATE TABLE dbo.[Inventario] ([IdInventario] int IDENTITY(1,1) NOT NULL,
[IdProducto] int NOT NULL,
[StockActual] int NOT NULL DEFAULT ((0)),
[StockMinimo] int NOT NULL DEFAULT ((0)),
[FechaActualizacion] datetime NOT NULL DEFAULT (getdate()),
PRIMARY KEY ([IdInventario]));
GO
CREATE TABLE dbo.[Marca] ([IdMarca] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(100) NOT NULL,
[LogoUrl] nvarchar(300) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdMarca]));
GO
CREATE TABLE dbo.[MovimientoCaja] ([IdMovimientoCaja] int IDENTITY(1,1) NOT NULL,
[IdCaja] int NOT NULL,
[IdTipoMovimientoCaja] int NOT NULL,
[IdVenta] int NULL,
[IdCompra] int NULL,
[IdUsuarioRegistro] int NOT NULL,
[Monto] decimal(18,2) NOT NULL,
[Descripcion] nvarchar(300) NULL,
[FechaMovimiento] datetime NOT NULL DEFAULT (getdate()),
[IdPagoVenta] int NULL,
[IdPagoCompra] int NULL,
[MetodoPago] nvarchar(100) NULL,
[OrigenMovimiento] nvarchar(100) NOT NULL DEFAULT ('Manual'),
[EsAutomatico] bit NOT NULL DEFAULT ((0)),
[Estado] bit NOT NULL DEFAULT ((1)),
[IdMovimientoRevertido] int NULL,
PRIMARY KEY ([IdMovimientoCaja]));
GO
CREATE TABLE dbo.[MovimientoStock] ([IdMovimientoStock] int IDENTITY(1,1) NOT NULL,
[IdProducto] int NOT NULL,
[IdUsuarioRegistro] int NOT NULL,
[IdTipoMovimientoStock] int NOT NULL,
[IdVenta] int NULL,
[IdCompra] int NULL,
[Cantidad] int NOT NULL,
[FechaMovimiento] datetime NOT NULL DEFAULT (getdate()),
[Motivo] nvarchar(300) NULL,
PRIMARY KEY ([IdMovimientoStock]));
GO
CREATE TABLE dbo.[PagoCompra] ([IdPagoCompra] int IDENTITY(1,1) NOT NULL,
[IdCompra] int NOT NULL,
[IdUsuarioRegistro] int NOT NULL,
[MetodoPago] nvarchar(50) NOT NULL,
[MontoPagado] decimal(18,2) NOT NULL,
[FechaPago] datetime NOT NULL DEFAULT (getdate()),
[Observacion] nvarchar(500) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdPagoCompra]));
GO
CREATE TABLE dbo.[PagoCompraCuota] ([IdPagoCompraCuota] int IDENTITY(1,1) NOT NULL,
[IdPagoCompra] int NOT NULL,
[IdCuotaCompra] int NOT NULL,
[MontoAplicado] decimal(18,2) NOT NULL,
[FechaRegistro] datetime NOT NULL DEFAULT (getdate()),
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdPagoCompraCuota]));
GO
CREATE TABLE dbo.[PagoVenta] ([IdPagoVenta] int IDENTITY(1,1) NOT NULL,
[IdVenta] int NOT NULL,
[IdUsuarioRegistro] int NOT NULL,
[MetodoPago] nvarchar(50) NOT NULL,
[MontoPagado] decimal(18,2) NOT NULL,
[FechaPago] datetime NOT NULL DEFAULT (getdate()),
[Observacion] nvarchar(300) NULL,
PRIMARY KEY ([IdPagoVenta]));
GO
CREATE TABLE dbo.[PagoVentaCuota] ([IdPagoVentaCuota] int IDENTITY(1,1) NOT NULL,
[IdPagoVenta] int NOT NULL,
[IdCuotaVenta] int NOT NULL,
[MontoAplicado] decimal(18,2) NOT NULL,
[FechaRegistro] datetime NOT NULL DEFAULT (getdate()),
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdPagoVentaCuota]));
GO
CREATE TABLE dbo.[Permiso] ([IdPermiso] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(100) NOT NULL,
[Descripcion] nvarchar(250) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdPermiso]));
GO
CREATE TABLE dbo.[PersonalInterno] ([IdPersonalInterno] int IDENTITY(1,1) NOT NULL,
[IdUsuario] int NOT NULL,
[Nombres] nvarchar(100) NOT NULL,
[ApellidoPaterno] nvarchar(100) NOT NULL,
[ApellidoMaterno] nvarchar(100) NULL,
[Telefono] nvarchar(20) NULL,
[Cargo] nvarchar(100) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdPersonalInterno]));
GO
CREATE TABLE dbo.[Producto] ([IdProducto] int IDENTITY(1,1) NOT NULL,
[IdElementoCatalogo] int NOT NULL,
[IdCategoria] int NOT NULL,
[IdMarca] int NULL,
[IdUnidadMedida] int NULL,
[CodigoProducto] nvarchar(50) NOT NULL,
[FichaTecnicaPdf] nvarchar(300) NULL,
[AplicaInventario] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdProducto]));
GO
CREATE TABLE dbo.[Proveedor] ([IdProveedor] int IDENTITY(1,1) NOT NULL,
[IdUbigeo] int NULL,
[Ruc] nvarchar(20) NOT NULL,
[RazonSocial] nvarchar(200) NOT NULL,
[NombreComercial] nvarchar(200) NULL,
[Correo] nvarchar(150) NULL,
[Telefono] nvarchar(20) NULL,
[Direccion] nvarchar(250) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdProveedor]));
GO
CREATE TABLE dbo.[Rol] ([IdRol] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(50) NOT NULL,
[Descripcion] nvarchar(250) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdRol]));
GO
CREATE TABLE dbo.[RolPermiso] ([IdRol] int NOT NULL,
[IdPermiso] int NOT NULL,
PRIMARY KEY ([IdRol],[IdPermiso]));
GO
CREATE TABLE dbo.[SectorIndustrial] ([IdSectorIndustrial] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(100) NOT NULL,
[Descripcion] nvarchar(300) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdSectorIndustrial]));
GO
CREATE TABLE dbo.[SerieComprobante] ([IdSerieComprobante] int IDENTITY(1,1) NOT NULL,
[IdTipoComprobante] int NOT NULL,
[Serie] nvarchar(10) NOT NULL,
[NumeroActual] int NOT NULL DEFAULT ((0)),
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdSerieComprobante]));
GO
CREATE TABLE dbo.[Servicio] ([IdServicio] int IDENTITY(1,1) NOT NULL,
[IdElementoCatalogo] int NOT NULL,
[SectorAplicacion] nvarchar(200) NULL,
[MensajeWhatsApp] nvarchar(500) NULL,
[RequiereVisitaTecnica] bit NOT NULL DEFAULT ((0)),
PRIMARY KEY ([IdServicio]));
GO
CREATE TABLE dbo.[TipoCliente] ([IdTipoCliente] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(50) NOT NULL,
[Descripcion] nvarchar(250) NULL,
[RequiereRuc] bit NOT NULL DEFAULT ((0)),
[RequiereFactura] bit NOT NULL DEFAULT ((0)),
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdTipoCliente]));
GO
CREATE TABLE dbo.[TipoComprobante] ([IdTipoComprobante] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(50) NOT NULL,
[Descripcion] nvarchar(250) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdTipoComprobante]));
GO
CREATE TABLE dbo.[TipoDocumento] ([IdTipoDocumento] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(30) NOT NULL,
[Longitud] int NOT NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdTipoDocumento]));
GO
CREATE TABLE dbo.[TipoDocumentoLegal] ([IdTipoDocumentoLegal] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(100) NOT NULL,
[Descripcion] nvarchar(250) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdTipoDocumentoLegal]));
GO
CREATE TABLE dbo.[TipoElemento] ([IdTipoElemento] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(50) NOT NULL,
[Descripcion] nvarchar(250) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdTipoElemento]));
GO
CREATE TABLE dbo.[TipoMovimientoCaja] ([IdTipoMovimientoCaja] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(50) NOT NULL,
[Descripcion] nvarchar(250) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdTipoMovimientoCaja]));
GO
CREATE TABLE dbo.[TipoMovimientoStock] ([IdTipoMovimientoStock] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(50) NOT NULL,
[Descripcion] nvarchar(250) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdTipoMovimientoStock]));
GO
CREATE TABLE dbo.[TipoServicioExterno] ([IdTipoServicioExterno] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(100) NOT NULL,
[Descripcion] nvarchar(250) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdTipoServicioExterno]));
GO
CREATE TABLE dbo.[Ubigeo] ([IdUbigeo] int IDENTITY(1,1) NOT NULL,
[Departamento] nvarchar(100) NOT NULL,
[Provincia] nvarchar(100) NOT NULL,
[Distrito] nvarchar(100) NOT NULL,
[CodigoUbigeo] nvarchar(6) NULL,
PRIMARY KEY ([IdUbigeo]));
GO
CREATE TABLE dbo.[UnidadMedida] ([IdUnidadMedida] int IDENTITY(1,1) NOT NULL,
[Nombre] nvarchar(80) NOT NULL,
[Abreviatura] nvarchar(20) NOT NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdUnidadMedida]));
GO
CREATE TABLE dbo.[Usuario] ([IdUsuario] int IDENTITY(1,1) NOT NULL,
[IdRol] int NOT NULL,
[Correo] nvarchar(150) NOT NULL,
[ContrasenaHash] nvarchar(255) NOT NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
[FechaRegistro] datetime NOT NULL DEFAULT (getdate()),
PRIMARY KEY ([IdUsuario]));
GO
CREATE TABLE dbo.[ValorCorporativo] ([IdValor] int IDENTITY(1,1) NOT NULL,
[IdEmpresa] int NOT NULL,
[Nombre] nvarchar(100) NOT NULL,
[Descripcion] nvarchar(300) NULL,
[Estado] bit NOT NULL DEFAULT ((1)),
PRIMARY KEY ([IdValor]));
GO
CREATE TABLE dbo.[Venta] ([IdVenta] int IDENTITY(1,1) NOT NULL,
[IdCliente] int NOT NULL,
[IdCotizacion] int NULL,
[IdUsuarioRegistro] int NOT NULL,
[IdEstadoVenta] int NOT NULL,
[FechaVenta] datetime NOT NULL DEFAULT (getdate()),
[Subtotal] decimal(18,2) NOT NULL DEFAULT ((0)),
[Igv] decimal(18,2) NOT NULL DEFAULT ((0)),
[Total] decimal(18,2) NOT NULL DEFAULT ((0)),
[Observacion] nvarchar(500) NULL,
PRIMARY KEY ([IdVenta]));
GO
ALTER TABLE dbo.[AceptacionDocumentoLegal] ADD CONSTRAINT [FK_AceptacionDocumentoLegal_DocumentoLegal] FOREIGN KEY ([IdDocumentoLegal]) REFERENCES dbo.[DocumentoLegal] ([IdDocumentoLegal]);
ALTER TABLE dbo.[AceptacionDocumentoLegal] ADD CONSTRAINT [FK_AceptacionDocumentoLegal_Usuario] FOREIGN KEY ([IdUsuario]) REFERENCES dbo.[Usuario] ([IdUsuario]);
ALTER TABLE dbo.[AlertaStock] ADD CONSTRAINT [FK_AlertaStock_EstadoAlertaStock] FOREIGN KEY ([IdEstadoAlertaStock]) REFERENCES dbo.[EstadoAlertaStock] ([IdEstadoAlertaStock]);
ALTER TABLE dbo.[AlertaStock] ADD CONSTRAINT [FK_AlertaStock_Producto] FOREIGN KEY ([IdProducto]) REFERENCES dbo.[Producto] ([IdProducto]);
ALTER TABLE dbo.[AuditoriaLog] ADD CONSTRAINT [FK_AuditoriaLog_Usuario] FOREIGN KEY ([IdUsuario]) REFERENCES dbo.[Usuario] ([IdUsuario]);
ALTER TABLE dbo.[Caja] ADD CONSTRAINT [FK_Caja_EstadoCaja] FOREIGN KEY ([IdEstadoCaja]) REFERENCES dbo.[EstadoCaja] ([IdEstadoCaja]);
ALTER TABLE dbo.[Caja] ADD CONSTRAINT [FK_Caja_UsuarioApertura] FOREIGN KEY ([IdUsuarioApertura]) REFERENCES dbo.[Usuario] ([IdUsuario]);
ALTER TABLE dbo.[Caja] ADD CONSTRAINT [FK_Caja_UsuarioCierre] FOREIGN KEY ([IdUsuarioCierre]) REFERENCES dbo.[Usuario] ([IdUsuario]);
ALTER TABLE dbo.[Cliente] ADD CONSTRAINT [FK_Cliente_TipoCliente] FOREIGN KEY ([IdTipoCliente]) REFERENCES dbo.[TipoCliente] ([IdTipoCliente]);
ALTER TABLE dbo.[Cliente] ADD CONSTRAINT [FK_Cliente_TipoDocumento] FOREIGN KEY ([IdTipoDocumento]) REFERENCES dbo.[TipoDocumento] ([IdTipoDocumento]);
ALTER TABLE dbo.[Cliente] ADD CONSTRAINT [FK_Cliente_Ubigeo] FOREIGN KEY ([IdUbigeo]) REFERENCES dbo.[Ubigeo] ([IdUbigeo]);
ALTER TABLE dbo.[Cliente] ADD CONSTRAINT [FK_Cliente_Usuario] FOREIGN KEY ([IdUsuario]) REFERENCES dbo.[Usuario] ([IdUsuario]);
ALTER TABLE dbo.[ClienteEmpresa] ADD CONSTRAINT [FK_ClienteEmpresa_Cliente] FOREIGN KEY ([IdCliente]) REFERENCES dbo.[Cliente] ([IdCliente]);
ALTER TABLE dbo.[ClientePersonaNatural] ADD CONSTRAINT [FK_ClientePersonaNatural_Cliente] FOREIGN KEY ([IdCliente]) REFERENCES dbo.[Cliente] ([IdCliente]);
ALTER TABLE dbo.[ClienteRecuperacion] ADD CONSTRAINT [FK_ClienteRecuperacion_Usuario] FOREIGN KEY ([IdUsuario]) REFERENCES dbo.[Usuario] ([IdUsuario]);
ALTER TABLE dbo.[Compra] ADD CONSTRAINT [FK_Compra_EstadoCompra] FOREIGN KEY ([IdEstadoCompra]) REFERENCES dbo.[EstadoCompra] ([IdEstadoCompra]);
ALTER TABLE dbo.[Compra] ADD CONSTRAINT [FK_Compra_EstadoPagoCompra] FOREIGN KEY ([IdEstadoPagoCompra]) REFERENCES dbo.[EstadoPagoCompra] ([IdEstadoPagoCompra]);
ALTER TABLE dbo.[Compra] ADD CONSTRAINT [FK_Compra_Proveedor] FOREIGN KEY ([IdProveedor]) REFERENCES dbo.[Proveedor] ([IdProveedor]);
ALTER TABLE dbo.[Compra] ADD CONSTRAINT [FK_Compra_UsuarioRegistro] FOREIGN KEY ([IdUsuarioRegistro]) REFERENCES dbo.[Usuario] ([IdUsuario]);
ALTER TABLE dbo.[Comprobante] ADD CONSTRAINT [FK_Comprobante_EstadoComprobante] FOREIGN KEY ([IdEstadoComprobante]) REFERENCES dbo.[EstadoComprobante] ([IdEstadoComprobante]);
ALTER TABLE dbo.[Comprobante] ADD CONSTRAINT [FK_Comprobante_SerieComprobante] FOREIGN KEY ([IdSerieComprobante]) REFERENCES dbo.[SerieComprobante] ([IdSerieComprobante]);
ALTER TABLE dbo.[Comprobante] ADD CONSTRAINT [FK_Comprobante_TipoComprobante] FOREIGN KEY ([IdTipoComprobante]) REFERENCES dbo.[TipoComprobante] ([IdTipoComprobante]);
ALTER TABLE dbo.[Comprobante] ADD CONSTRAINT [FK_Comprobante_Venta] FOREIGN KEY ([IdVenta]) REFERENCES dbo.[Venta] ([IdVenta]);
ALTER TABLE dbo.[ConsultaExterna] ADD CONSTRAINT [FK_ConsultaExterna_Cliente] FOREIGN KEY ([IdCliente]) REFERENCES dbo.[Cliente] ([IdCliente]);
ALTER TABLE dbo.[ConsultaExterna] ADD CONSTRAINT [FK_ConsultaExterna_TipoServicioExterno] FOREIGN KEY ([IdTipoServicioExterno]) REFERENCES dbo.[TipoServicioExterno] ([IdTipoServicioExterno]);
ALTER TABLE dbo.[ConsultaExterna] ADD CONSTRAINT [FK_ConsultaExterna_UsuarioRegistro] FOREIGN KEY ([IdUsuarioRegistro]) REFERENCES dbo.[Usuario] ([IdUsuario]);
ALTER TABLE dbo.[ContactoCliente] ADD CONSTRAINT [FK_ContactoCliente_Cliente] FOREIGN KEY ([IdCliente]) REFERENCES dbo.[Cliente] ([IdCliente]);
ALTER TABLE dbo.[ContactoProveedor] ADD CONSTRAINT [FK_ContactoProveedor_Proveedor] FOREIGN KEY ([IdProveedor]) REFERENCES dbo.[Proveedor] ([IdProveedor]);
ALTER TABLE dbo.[Cotizacion] ADD CONSTRAINT [FK_Cotizacion_Cliente] FOREIGN KEY ([IdCliente]) REFERENCES dbo.[Cliente] ([IdCliente]);
ALTER TABLE dbo.[Cotizacion] ADD CONSTRAINT [FK_Cotizacion_EstadoCotizacion] FOREIGN KEY ([IdEstadoCotizacion]) REFERENCES dbo.[EstadoCotizacion] ([IdEstadoCotizacion]);
ALTER TABLE dbo.[Cotizacion] ADD CONSTRAINT [FK_Cotizacion_UsuarioAtencion] FOREIGN KEY ([IdUsuarioAtencion]) REFERENCES dbo.[Usuario] ([IdUsuario]);
ALTER TABLE dbo.[Cotizacion] ADD CONSTRAINT [FK_Cotizacion_UsuarioRegistro] FOREIGN KEY ([IdUsuarioRegistro]) REFERENCES dbo.[Usuario] ([IdUsuario]);
ALTER TABLE dbo.[CuotaCompra] ADD CONSTRAINT [FK_CuotaCompra_Compra] FOREIGN KEY ([IdCompra]) REFERENCES dbo.[Compra] ([IdCompra]);
ALTER TABLE dbo.[CuotaVenta] ADD CONSTRAINT [FK_CuotaVenta_Venta] FOREIGN KEY ([IdVenta]) REFERENCES dbo.[Venta] ([IdVenta]);
ALTER TABLE dbo.[DetalleCompra] ADD CONSTRAINT [FK_DetalleCompra_Compra] FOREIGN KEY ([IdCompra]) REFERENCES dbo.[Compra] ([IdCompra]);
ALTER TABLE dbo.[DetalleCompra] ADD CONSTRAINT [FK_DetalleCompra_Producto] FOREIGN KEY ([IdProducto]) REFERENCES dbo.[Producto] ([IdProducto]);
ALTER TABLE dbo.[DetalleCotizacion] ADD CONSTRAINT [FK_DetalleCotizacion_Cotizacion] FOREIGN KEY ([IdCotizacion]) REFERENCES dbo.[Cotizacion] ([IdCotizacion]);
ALTER TABLE dbo.[DetalleCotizacion] ADD CONSTRAINT [FK_DetalleCotizacion_ElementoCatalogo] FOREIGN KEY ([IdElementoCatalogo]) REFERENCES dbo.[ElementoCatalogo] ([IdElementoCatalogo]);
ALTER TABLE dbo.[DetalleVenta] ADD CONSTRAINT [FK_DetalleVenta_ElementoCatalogo] FOREIGN KEY ([IdElementoCatalogo]) REFERENCES dbo.[ElementoCatalogo] ([IdElementoCatalogo]);
ALTER TABLE dbo.[DetalleVenta] ADD CONSTRAINT [FK_DetalleVenta_Venta] FOREIGN KEY ([IdVenta]) REFERENCES dbo.[Venta] ([IdVenta]);
ALTER TABLE dbo.[DocumentoLegal] ADD CONSTRAINT [FK_DocumentoLegal_TipoDocumentoLegal] FOREIGN KEY ([IdTipoDocumentoLegal]) REFERENCES dbo.[TipoDocumentoLegal] ([IdTipoDocumentoLegal]);
ALTER TABLE dbo.[ElementoCatalogo] ADD CONSTRAINT [FK_ElementoCatalogo_TipoElemento] FOREIGN KEY ([IdTipoElemento]) REFERENCES dbo.[TipoElemento] ([IdTipoElemento]);
ALTER TABLE dbo.[ElementoCatalogoSector] ADD CONSTRAINT [FK_ElementoCatalogoSector_ElementoCatalogo] FOREIGN KEY ([IdElementoCatalogo]) REFERENCES dbo.[ElementoCatalogo] ([IdElementoCatalogo]);
ALTER TABLE dbo.[ElementoCatalogoSector] ADD CONSTRAINT [FK_ElementoCatalogoSector_SectorIndustrial] FOREIGN KEY ([IdSectorIndustrial]) REFERENCES dbo.[SectorIndustrial] ([IdSectorIndustrial]);
ALTER TABLE dbo.[EnvioCorreo] ADD CONSTRAINT [FK_EnvioCorreo_Comprobante] FOREIGN KEY ([IdComprobante]) REFERENCES dbo.[Comprobante] ([IdComprobante]);
ALTER TABLE dbo.[EnvioCorreo] ADD CONSTRAINT [FK_EnvioCorreo_Cotizacion] FOREIGN KEY ([IdCotizacion]) REFERENCES dbo.[Cotizacion] ([IdCotizacion]);
ALTER TABLE dbo.[EnvioCorreo] ADD CONSTRAINT [FK_EnvioCorreo_UsuarioRegistro] FOREIGN KEY ([IdUsuarioRegistro]) REFERENCES dbo.[Usuario] ([IdUsuario]);
ALTER TABLE dbo.[GuiaRemisionCompra] ADD CONSTRAINT [FK_GuiaRemisionCompra_Compra] FOREIGN KEY ([IdCompra]) REFERENCES dbo.[Compra] ([IdCompra]);
ALTER TABLE dbo.[ImagenElementoCatalogo] ADD CONSTRAINT [FK_ImagenElementoCatalogo_ElementoCatalogo] FOREIGN KEY ([IdElementoCatalogo]) REFERENCES dbo.[ElementoCatalogo] ([IdElementoCatalogo]);
ALTER TABLE dbo.[Inventario] ADD CONSTRAINT [FK_Inventario_Producto] FOREIGN KEY ([IdProducto]) REFERENCES dbo.[Producto] ([IdProducto]);
ALTER TABLE dbo.[MovimientoCaja] ADD CONSTRAINT [FK_MovimientoCaja_Caja] FOREIGN KEY ([IdCaja]) REFERENCES dbo.[Caja] ([IdCaja]);
ALTER TABLE dbo.[MovimientoCaja] ADD CONSTRAINT [FK_MovimientoCaja_Compra] FOREIGN KEY ([IdCompra]) REFERENCES dbo.[Compra] ([IdCompra]);
ALTER TABLE dbo.[MovimientoCaja] ADD CONSTRAINT [FK_MovimientoCaja_PagoCompra] FOREIGN KEY ([IdPagoCompra]) REFERENCES dbo.[PagoCompra] ([IdPagoCompra]);
ALTER TABLE dbo.[MovimientoCaja] ADD CONSTRAINT [FK_MovimientoCaja_PagoVenta] FOREIGN KEY ([IdPagoVenta]) REFERENCES dbo.[PagoVenta] ([IdPagoVenta]);
ALTER TABLE dbo.[MovimientoCaja] ADD CONSTRAINT [FK_MovimientoCaja_Reversion] FOREIGN KEY ([IdMovimientoRevertido]) REFERENCES dbo.[MovimientoCaja] ([IdMovimientoCaja]);
ALTER TABLE dbo.[MovimientoCaja] ADD CONSTRAINT [FK_MovimientoCaja_TipoMovimientoCaja] FOREIGN KEY ([IdTipoMovimientoCaja]) REFERENCES dbo.[TipoMovimientoCaja] ([IdTipoMovimientoCaja]);
ALTER TABLE dbo.[MovimientoCaja] ADD CONSTRAINT [FK_MovimientoCaja_UsuarioRegistro] FOREIGN KEY ([IdUsuarioRegistro]) REFERENCES dbo.[Usuario] ([IdUsuario]);
ALTER TABLE dbo.[MovimientoCaja] ADD CONSTRAINT [FK_MovimientoCaja_Venta] FOREIGN KEY ([IdVenta]) REFERENCES dbo.[Venta] ([IdVenta]);
ALTER TABLE dbo.[MovimientoStock] ADD CONSTRAINT [FK_MovimientoStock_Compra] FOREIGN KEY ([IdCompra]) REFERENCES dbo.[Compra] ([IdCompra]);
ALTER TABLE dbo.[MovimientoStock] ADD CONSTRAINT [FK_MovimientoStock_Producto] FOREIGN KEY ([IdProducto]) REFERENCES dbo.[Producto] ([IdProducto]);
ALTER TABLE dbo.[MovimientoStock] ADD CONSTRAINT [FK_MovimientoStock_TipoMovimientoStock] FOREIGN KEY ([IdTipoMovimientoStock]) REFERENCES dbo.[TipoMovimientoStock] ([IdTipoMovimientoStock]);
ALTER TABLE dbo.[MovimientoStock] ADD CONSTRAINT [FK_MovimientoStock_UsuarioRegistro] FOREIGN KEY ([IdUsuarioRegistro]) REFERENCES dbo.[Usuario] ([IdUsuario]);
ALTER TABLE dbo.[MovimientoStock] ADD CONSTRAINT [FK_MovimientoStock_Venta] FOREIGN KEY ([IdVenta]) REFERENCES dbo.[Venta] ([IdVenta]);
ALTER TABLE dbo.[PagoCompra] ADD CONSTRAINT [FK_PagoCompra_Compra] FOREIGN KEY ([IdCompra]) REFERENCES dbo.[Compra] ([IdCompra]);
ALTER TABLE dbo.[PagoCompra] ADD CONSTRAINT [FK_PagoCompra_Usuario] FOREIGN KEY ([IdUsuarioRegistro]) REFERENCES dbo.[Usuario] ([IdUsuario]);
ALTER TABLE dbo.[PagoCompraCuota] ADD CONSTRAINT [FK_PagoCompraCuota_CuotaCompra] FOREIGN KEY ([IdCuotaCompra]) REFERENCES dbo.[CuotaCompra] ([IdCuotaCompra]);
ALTER TABLE dbo.[PagoCompraCuota] ADD CONSTRAINT [FK_PagoCompraCuota_PagoCompra] FOREIGN KEY ([IdPagoCompra]) REFERENCES dbo.[PagoCompra] ([IdPagoCompra]);
ALTER TABLE dbo.[PagoVenta] ADD CONSTRAINT [FK_PagoVenta_UsuarioRegistro] FOREIGN KEY ([IdUsuarioRegistro]) REFERENCES dbo.[Usuario] ([IdUsuario]);
ALTER TABLE dbo.[PagoVenta] ADD CONSTRAINT [FK_PagoVenta_Venta] FOREIGN KEY ([IdVenta]) REFERENCES dbo.[Venta] ([IdVenta]);
ALTER TABLE dbo.[PagoVentaCuota] ADD CONSTRAINT [FK_PagoVentaCuota_CuotaVenta] FOREIGN KEY ([IdCuotaVenta]) REFERENCES dbo.[CuotaVenta] ([IdCuotaVenta]);
ALTER TABLE dbo.[PagoVentaCuota] ADD CONSTRAINT [FK_PagoVentaCuota_PagoVenta] FOREIGN KEY ([IdPagoVenta]) REFERENCES dbo.[PagoVenta] ([IdPagoVenta]);
ALTER TABLE dbo.[PersonalInterno] ADD CONSTRAINT [FK_PersonalInterno_Usuario] FOREIGN KEY ([IdUsuario]) REFERENCES dbo.[Usuario] ([IdUsuario]);
ALTER TABLE dbo.[Producto] ADD CONSTRAINT [FK_Producto_Categoria] FOREIGN KEY ([IdCategoria]) REFERENCES dbo.[Categoria] ([IdCategoria]);
ALTER TABLE dbo.[Producto] ADD CONSTRAINT [FK_Producto_ElementoCatalogo] FOREIGN KEY ([IdElementoCatalogo]) REFERENCES dbo.[ElementoCatalogo] ([IdElementoCatalogo]);
ALTER TABLE dbo.[Producto] ADD CONSTRAINT [FK_Producto_Marca] FOREIGN KEY ([IdMarca]) REFERENCES dbo.[Marca] ([IdMarca]);
ALTER TABLE dbo.[Producto] ADD CONSTRAINT [FK_Producto_UnidadMedida] FOREIGN KEY ([IdUnidadMedida]) REFERENCES dbo.[UnidadMedida] ([IdUnidadMedida]);
ALTER TABLE dbo.[Proveedor] ADD CONSTRAINT [FK_Proveedor_Ubigeo] FOREIGN KEY ([IdUbigeo]) REFERENCES dbo.[Ubigeo] ([IdUbigeo]);
ALTER TABLE dbo.[RolPermiso] ADD CONSTRAINT [FK_RolPermiso_Permiso] FOREIGN KEY ([IdPermiso]) REFERENCES dbo.[Permiso] ([IdPermiso]);
ALTER TABLE dbo.[RolPermiso] ADD CONSTRAINT [FK_RolPermiso_Rol] FOREIGN KEY ([IdRol]) REFERENCES dbo.[Rol] ([IdRol]);
ALTER TABLE dbo.[SerieComprobante] ADD CONSTRAINT [FK_SerieComprobante_TipoComprobante] FOREIGN KEY ([IdTipoComprobante]) REFERENCES dbo.[TipoComprobante] ([IdTipoComprobante]);
ALTER TABLE dbo.[Servicio] ADD CONSTRAINT [FK_Servicio_ElementoCatalogo] FOREIGN KEY ([IdElementoCatalogo]) REFERENCES dbo.[ElementoCatalogo] ([IdElementoCatalogo]);
ALTER TABLE dbo.[Usuario] ADD CONSTRAINT [FK_Usuario_Rol] FOREIGN KEY ([IdRol]) REFERENCES dbo.[Rol] ([IdRol]);
ALTER TABLE dbo.[ValorCorporativo] ADD CONSTRAINT [FK_ValorCorporativo_Empresa] FOREIGN KEY ([IdEmpresa]) REFERENCES dbo.[Empresa] ([IdEmpresa]);
ALTER TABLE dbo.[Venta] ADD CONSTRAINT [FK_Venta_Cliente] FOREIGN KEY ([IdCliente]) REFERENCES dbo.[Cliente] ([IdCliente]);
ALTER TABLE dbo.[Venta] ADD CONSTRAINT [FK_Venta_Cotizacion] FOREIGN KEY ([IdCotizacion]) REFERENCES dbo.[Cotizacion] ([IdCotizacion]);
ALTER TABLE dbo.[Venta] ADD CONSTRAINT [FK_Venta_EstadoVenta] FOREIGN KEY ([IdEstadoVenta]) REFERENCES dbo.[EstadoVenta] ([IdEstadoVenta]);
ALTER TABLE dbo.[Venta] ADD CONSTRAINT [FK_Venta_UsuarioRegistro] FOREIGN KEY ([IdUsuarioRegistro]) REFERENCES dbo.[Usuario] ([IdUsuario]);
ALTER TABLE dbo.[Cotizacion] ADD CONSTRAINT [CK_Cotizacion_Igv] CHECK ([Igv]>=(0));
ALTER TABLE dbo.[DetalleCompra] ADD CONSTRAINT [CK_DetalleCompra_Subtotal] CHECK ([Subtotal]>=(0));
ALTER TABLE dbo.[Cotizacion] ADD CONSTRAINT [CK_Cotizacion_TotalFinal] CHECK ([Total]>=(0));
ALTER TABLE dbo.[Cotizacion] ADD CONSTRAINT [CK_Cotizacion_Descuento_NoMayor_Subtotal] CHECK ([Descuento]<=[Subtotal]);
ALTER TABLE dbo.[Inventario] ADD CONSTRAINT [CK_Inventario_StockActual] CHECK ([StockActual]>=(0));
ALTER TABLE dbo.[Inventario] ADD CONSTRAINT [CK_Inventario_StockMinimo] CHECK ([StockMinimo]>=(0));
ALTER TABLE dbo.[MovimientoStock] ADD CONSTRAINT [CK_MovimientoStock_Cantidad] CHECK ([Cantidad]>(0));
ALTER TABLE dbo.[ElementoCatalogo] ADD CONSTRAINT [CK_ElementoCatalogo_Precio] CHECK ([PrecioReferencial] IS NULL OR [PrecioReferencial]>=(0));
ALTER TABLE dbo.[Caja] ADD CONSTRAINT [CK_Caja_SaldoInicial] CHECK ([SaldoInicial]>=(0));
ALTER TABLE dbo.[Caja] ADD CONSTRAINT [CK_Caja_SaldoFinal] CHECK ([SaldoFinal] IS NULL OR [SaldoFinal]>=(0));
ALTER TABLE dbo.[PagoCompra] ADD CONSTRAINT [CK_PagoCompra_Monto] CHECK ([MontoPagado]>(0));
ALTER TABLE dbo.[MovimientoCaja] ADD CONSTRAINT [CK_MovimientoCaja_Monto] CHECK ([Monto]>(0));
ALTER TABLE dbo.[Cotizacion] ADD CONSTRAINT [CK_Cotizacion_Total] CHECK ([TotalReferencial]>=(0));
ALTER TABLE dbo.[CuotaCompra] ADD CONSTRAINT [CK_CuotaCompra_Numero] CHECK ([NumeroCuota]>(0));
ALTER TABLE dbo.[CuotaCompra] ADD CONSTRAINT [CK_CuotaCompra_Monto] CHECK ([MontoCuota]>(0));
ALTER TABLE dbo.[DetalleCotizacion] ADD CONSTRAINT [CK_DetalleCotizacion_Cantidad] CHECK ([Cantidad]>(0));
ALTER TABLE dbo.[DetalleCotizacion] ADD CONSTRAINT [CK_DetalleCotizacion_Precio] CHECK ([PrecioUnitario]>=(0));
ALTER TABLE dbo.[DetalleCotizacion] ADD CONSTRAINT [CK_DetalleCotizacion_Subtotal] CHECK ([Subtotal]>=(0));
ALTER TABLE dbo.[PagoVenta] ADD CONSTRAINT [CK_PagoVenta_Monto] CHECK ([MontoPagado]>(0));
ALTER TABLE dbo.[CuotaVenta] ADD CONSTRAINT [CK_CuotaVenta_Numero] CHECK ([NumeroCuota]>(0));
ALTER TABLE dbo.[CuotaVenta] ADD CONSTRAINT [CK_CuotaVenta_Monto] CHECK ([MontoCuota]>(0));
ALTER TABLE dbo.[CuotaVenta] ADD CONSTRAINT [CK_CuotaVenta_MontoPagado] CHECK ([MontoPagado]>=(0));
ALTER TABLE dbo.[Venta] ADD CONSTRAINT [CK_Venta_Subtotal] CHECK ([Subtotal]>=(0));
ALTER TABLE dbo.[Venta] ADD CONSTRAINT [CK_Venta_Igv] CHECK ([Igv]>=(0));
ALTER TABLE dbo.[Venta] ADD CONSTRAINT [CK_Venta_Total] CHECK ([Total]>=(0));
ALTER TABLE dbo.[DetalleVenta] ADD CONSTRAINT [CK_DetalleVenta_Cantidad] CHECK ([Cantidad]>(0));
ALTER TABLE dbo.[DetalleVenta] ADD CONSTRAINT [CK_DetalleVenta_Precio] CHECK ([PrecioUnitario]>=(0));
ALTER TABLE dbo.[DetalleVenta] ADD CONSTRAINT [CK_DetalleVenta_Subtotal] CHECK ([Subtotal]>=(0));
ALTER TABLE dbo.[Compra] ADD CONSTRAINT [CK_Compra_Total] CHECK ([Total]>=(0));
ALTER TABLE dbo.[Cotizacion] ADD CONSTRAINT [CK_Cotizacion_OrigenCotizacion] CHECK ([OrigenCotizacion]='Correo' OR [OrigenCotizacion]='WhatsApp' OR [OrigenCotizacion]='Web' OR [OrigenCotizacion]='Manual');
ALTER TABLE dbo.[Cotizacion] ADD CONSTRAINT [CK_Cotizacion_Subtotal] CHECK ([Subtotal]>=(0));
ALTER TABLE dbo.[DetalleCompra] ADD CONSTRAINT [CK_DetalleCompra_Cantidad] CHECK ([Cantidad]>(0));
ALTER TABLE dbo.[Cotizacion] ADD CONSTRAINT [CK_Cotizacion_Descuento] CHECK ([Descuento]>=(0));
ALTER TABLE dbo.[DetalleCompra] ADD CONSTRAINT [CK_DetalleCompra_Precio] CHECK ([PrecioCompra]>=(0));

GO
CREATE UNIQUE INDEX [UQ_SectorIndustrial_Nombre] ON dbo.[SectorIndustrial] ([Nombre]);
GO
CREATE UNIQUE INDEX [UQ_Inventario_Producto] ON dbo.[Inventario] ([IdProducto]);
GO
CREATE INDEX [IX_Inventario_Producto] ON dbo.[Inventario] ([IdProducto]);
GO
CREATE UNIQUE INDEX [UQ_TipoElemento_Nombre] ON dbo.[TipoElemento] ([Nombre]);
GO
CREATE UNIQUE INDEX [UQ_Categoria_Nombre] ON dbo.[Categoria] ([Nombre]);
GO
CREATE UNIQUE INDEX [UQ_TipoMovimientoStock_Nombre] ON dbo.[TipoMovimientoStock] ([Nombre]);
GO
CREATE UNIQUE INDEX [UQ_Marca_Nombre] ON dbo.[Marca] ([Nombre]);
GO
CREATE INDEX [IX_MovimientoStock_Producto] ON dbo.[MovimientoStock] ([IdProducto]);
GO
CREATE UNIQUE INDEX [UQ_UnidadMedida_Nombre] ON dbo.[UnidadMedida] ([Nombre]);
GO
CREATE UNIQUE INDEX [UQ_EstadoAlertaStock_Nombre] ON dbo.[EstadoAlertaStock] ([Nombre]);
GO
CREATE UNIQUE INDEX [UQ_Producto_Codigo] ON dbo.[Producto] ([CodigoProducto]);
GO
CREATE UNIQUE INDEX [UQ_Producto_ElementoCatalogo] ON dbo.[Producto] ([IdElementoCatalogo]);
GO
CREATE INDEX [IX_Producto_Categoria] ON dbo.[Producto] ([IdCategoria]);
GO
CREATE INDEX [IX_Producto_Marca] ON dbo.[Producto] ([IdMarca]);
GO
CREATE UNIQUE INDEX [UQ_EstadoPagoCompra_Nombre] ON dbo.[EstadoPagoCompra] ([Nombre]);
GO
CREATE UNIQUE INDEX [UQ_EstadoCaja_Nombre] ON dbo.[EstadoCaja] ([Nombre]);
GO
CREATE UNIQUE INDEX [UQ_Servicio_ElementoCatalogo] ON dbo.[Servicio] ([IdElementoCatalogo]);
GO
CREATE UNIQUE INDEX [UQ_EstadoCotizacion_Nombre] ON dbo.[EstadoCotizacion] ([Nombre]);
GO
CREATE UNIQUE INDEX [UQ_TipoMovimientoCaja_Nombre] ON dbo.[TipoMovimientoCaja] ([Nombre]);
GO
CREATE INDEX [IX_Cotizacion_Cliente] ON dbo.[Cotizacion] ([IdCliente]);
GO
CREATE INDEX [IX_Cotizacion_Estado] ON dbo.[Cotizacion] ([IdEstadoCotizacion]);
GO
CREATE INDEX [IX_MovimientoCaja_Caja] ON dbo.[MovimientoCaja] ([IdCaja]);
GO
CREATE UNIQUE INDEX [UX_MovimientoCaja_IdPagoVenta_Activo] ON dbo.[MovimientoCaja] ([IdPagoVenta]) WHERE ([IdPagoVenta] IS NOT NULL AND [Estado]=(1));
GO
CREATE UNIQUE INDEX [UX_MovimientoCaja_IdPagoCompra_Activo] ON dbo.[MovimientoCaja] ([IdPagoCompra]) WHERE ([IdPagoCompra] IS NOT NULL AND [Estado]=(1));
GO
CREATE UNIQUE INDEX [UX_MovimientoCaja_Reversion] ON dbo.[MovimientoCaja] ([IdMovimientoRevertido]) WHERE ([IdMovimientoRevertido] IS NOT NULL);
GO
CREATE UNIQUE INDEX [UQ_TipoDocumentoLegal_Nombre] ON dbo.[TipoDocumentoLegal] ([Nombre]);
GO
CREATE UNIQUE INDEX [UQ_AceptacionDocumentoLegal] ON dbo.[AceptacionDocumentoLegal] ([IdUsuario],[IdDocumentoLegal]);
GO
CREATE UNIQUE INDEX [UQ_EstadoVenta_Nombre] ON dbo.[EstadoVenta] ([Nombre]);
GO
CREATE INDEX [IX_Venta_Cliente] ON dbo.[Venta] ([IdCliente]);
GO
CREATE INDEX [IX_Venta_Estado] ON dbo.[Venta] ([IdEstadoVenta]);
GO
CREATE INDEX [IX_AuditoriaLog_Fecha] ON dbo.[AuditoriaLog] ([Fecha] DESC,[IdLog] DESC);
GO
CREATE INDEX [IX_ClienteRecuperacion_Usuario] ON dbo.[ClienteRecuperacion] ([IdUsuario],[CreadoUtc]);
GO
CREATE UNIQUE INDEX [UQ_Usuario_Correo] ON dbo.[Usuario] ([Correo]);
GO
CREATE INDEX [IX_Usuario_Rol] ON dbo.[Usuario] ([IdRol]);
GO
CREATE INDEX [IX_ImagenElementoCatalogo_Elemento] ON dbo.[ImagenElementoCatalogo] ([IdElementoCatalogo]);
GO
CREATE UNIQUE INDEX [UQ_TipoComprobante_Nombre] ON dbo.[TipoComprobante] ([Nombre]);
GO
CREATE UNIQUE INDEX [UQ_PersonalInterno_Usuario] ON dbo.[PersonalInterno] ([IdUsuario]);
GO
CREATE UNIQUE INDEX [UQ_TipoServicioExterno_Nombre] ON dbo.[TipoServicioExterno] ([Nombre]);
GO
CREATE UNIQUE INDEX [UQ_EstadoComprobante_Nombre] ON dbo.[EstadoComprobante] ([Nombre]);
GO
CREATE UNIQUE INDEX [UQ_TipoDocumento_Nombre] ON dbo.[TipoDocumento] ([Nombre]);
GO
CREATE INDEX [IX_ConsultaExterna_TipoServicio] ON dbo.[ConsultaExterna] ([IdTipoServicioExterno]);
GO
CREATE INDEX [IX_ConsultaExterna_Cliente] ON dbo.[ConsultaExterna] ([IdCliente]);
GO
CREATE UNIQUE INDEX [UQ_Comprobante_SerieNumero] ON dbo.[Comprobante] ([Serie],[Numero]);
GO
CREATE UNIQUE INDEX [UQ_Comprobante_Venta] ON dbo.[Comprobante] ([IdVenta]);
GO
CREATE UNIQUE INDEX [UQ_TipoCliente_Nombre] ON dbo.[TipoCliente] ([Nombre]);
GO
CREATE INDEX [IX_EnvioCorreo_Cotizacion] ON dbo.[EnvioCorreo] ([IdCotizacion]);
GO
CREATE INDEX [IX_EnvioCorreo_Comprobante] ON dbo.[EnvioCorreo] ([IdComprobante]);
GO
CREATE UNIQUE INDEX [UQ_Ubigeo_CodigoUbigeo] ON dbo.[Ubigeo] ([CodigoUbigeo]) WHERE ([CodigoUbigeo] IS NOT NULL);
GO
CREATE INDEX [IX_Ubigeo_Busqueda] ON dbo.[Ubigeo] ([Departamento],[Provincia],[Distrito]);
GO
CREATE UNIQUE INDEX [UQ_Proveedor_Ruc] ON dbo.[Proveedor] ([Ruc]);
GO
CREATE UNIQUE INDEX [UQ_Cliente_Documento] ON dbo.[Cliente] ([IdTipoDocumento],[NumeroDocumento]);
GO
CREATE UNIQUE INDEX [UX_Cliente_IdUsuario] ON dbo.[Cliente] ([IdUsuario]) WHERE ([IdUsuario] IS NOT NULL);
GO
CREATE INDEX [IX_Cliente_TipoCliente] ON dbo.[Cliente] ([IdTipoCliente]);
GO
CREATE INDEX [IX_Cliente_TipoDocumento] ON dbo.[Cliente] ([IdTipoDocumento]);
GO
CREATE UNIQUE INDEX [UQ_SerieComprobante] ON dbo.[SerieComprobante] ([IdTipoComprobante],[Serie]);
GO
CREATE UNIQUE INDEX [UQ_EstadoCompra_Nombre] ON dbo.[EstadoCompra] ([Nombre]);
GO
CREATE INDEX [IX_Compra_Proveedor] ON dbo.[Compra] ([IdProveedor]);
GO
CREATE INDEX [IX_Compra_Estado] ON dbo.[Compra] ([IdEstadoCompra]);
GO
/* =========================================================
   7. FUNCIÓN: CANTIDAD DE ADMINISTRADORES ACTIVOS
   ========================================================= */

CREATE   FUNCTION dbo.fn_CantidadAdministradoresActivos()
RETURNS INT
AS
BEGIN
    DECLARE @Cantidad INT = 0;

    SELECT @Cantidad = COUNT(*)
    FROM dbo.Usuario u
    INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
    WHERE u.Estado = 1
      AND r.Estado = 1
      AND UPPER(LTRIM(RTRIM(r.Nombre))) IN ('ADMINISTRADOR', 'ADMIN');

    RETURN ISNULL(@Cantidad, 0);
END;

GO
/* =========================================================
   6. FUNCIÓN PARA VALIDAR ADMINISTRADOR
   ========================================================= */

CREATE   FUNCTION dbo.fn_EsUsuarioAdministrador
(
    @IdUsuario INT
)
RETURNS BIT
AS
BEGIN
    DECLARE @EsAdministrador BIT = 0;

    IF EXISTS (
        SELECT 1
        FROM dbo.Usuario u
        INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
        WHERE u.IdUsuario = @IdUsuario
          AND u.Estado = 1
          AND r.Estado = 1
          AND (
                UPPER(LTRIM(RTRIM(r.Nombre))) = 'ADMINISTRADOR'
             OR UPPER(LTRIM(RTRIM(r.Nombre))) = 'ADMIN'
          )
    )
    BEGIN
        SET @EsAdministrador = 1;
    END;

    RETURN @EsAdministrador;
END;

GO
/* =========================================================
   7. PROCEDIMIENTO: ABRIR CAJA
   ========================================================= */

CREATE   PROCEDURE dbo.sp_AbrirCaja
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
   7. ACTUALIZAR ARCHIVO PDF DE COTIZACIÓN
   ========================================================= */

CREATE   PROCEDURE dbo.sp_ActualizarPdfCotizacion
    @IdCotizacion INT,
    @ArchivoPdf NVARCHAR(600)
AS
BEGIN
    SET NOCOUNT ON;

    SET @ArchivoPdf = NULLIF(LTRIM(RTRIM(ISNULL(@ArchivoPdf, ''))), '');

    IF @IdCotizacion <= 0
        THROW 63001, 'Selecciona una cotización válida.', 1;

    IF @ArchivoPdf IS NULL
        THROW 63002, 'La ruta del PDF no es válida.', 1;

    IF NOT EXISTS (SELECT 1 FROM Cotizacion WHERE IdCotizacion = @IdCotizacion)
        THROW 63003, 'La cotización seleccionada no existe.', 1;

    UPDATE Cotizacion
    SET ArchivoPdf = @ArchivoPdf,
        FechaActualizacion = GETDATE()
    WHERE IdCotizacion = @IdCotizacion;

    SELECT
        @IdCotizacion AS IdCotizacion,
        @ArchivoPdf AS ArchivoPdf,
        'PDF de cotización actualizado correctamente.' AS Mensaje;
END;

GO
/* =========================================================
   19. PROCEDIMIENTO: ACTUALIZAR ROL
   ========================================================= */

CREATE   PROCEDURE dbo.sp_ActualizarRol
    @IdRol INT,
    @Nombre NVARCHAR(100),
    @Descripcion NVARCHAR(500) = NULL,
    @Estado BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RolActual NVARCHAR(100);

    SET @Nombre = LTRIM(RTRIM(ISNULL(@Nombre, '')));
    SET @Descripcion = NULLIF(LTRIM(RTRIM(ISNULL(@Descripcion, ''))), '');

    IF @IdRol <= 0 OR NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE IdRol = @IdRol)
        THROW 72701, 'El rol seleccionado no existe.', 1;

    SELECT @RolActual = Nombre
    FROM dbo.Rol
    WHERE IdRol = @IdRol;

    IF UPPER(LTRIM(RTRIM(@RolActual))) IN ('ADMINISTRADOR', 'ADMIN')
       AND (
            UPPER(LTRIM(RTRIM(@Nombre))) NOT IN ('ADMINISTRADOR', 'ADMIN')
            OR ISNULL(@Estado, 0) = 0
       )
    BEGIN
        THROW 72702, 'No puedes renombrar ni desactivar el rol Administrador.', 1;
    END;

    IF @Nombre = ''
        THROW 72703, 'Ingresa el nombre del rol.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.Rol
        WHERE UPPER(Nombre) = UPPER(@Nombre)
          AND IdRol <> @IdRol
    )
        THROW 72704, 'Ya existe otro rol registrado con ese nombre.', 1;

    UPDATE dbo.Rol
    SET
        Nombre = @Nombre,
        Descripcion = @Descripcion,
        Estado = ISNULL(@Estado, 1)
    WHERE IdRol = @IdRol;

    EXEC dbo.sp_ListarRoles @SoloActivos = NULL;
END;

GO
/* =========================================================
   14. PROCEDIMIENTO: ACTUALIZAR USUARIO
   ========================================================= */

CREATE   PROCEDURE dbo.sp_ActualizarUsuario
    @IdUsuario INT,
    @IdRol INT,
    @Correo NVARCHAR(300),
    @Estado BIT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdRolActual INT;
    DECLARE @RolActual NVARCHAR(100);
    DECLARE @RolNuevo NVARCHAR(100);
    DECLARE @EstadoActual BIT;

    SET @Correo = LOWER(LTRIM(RTRIM(ISNULL(@Correo, ''))));

    IF @IdUsuario <= 0 OR NOT EXISTS (SELECT 1 FROM dbo.Usuario WHERE IdUsuario = @IdUsuario)
        THROW 72301, 'El usuario seleccionado no existe.', 1;

    IF @IdRol <= 0 OR NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE IdRol = @IdRol AND Estado = 1)
        THROW 72302, 'Selecciona un rol válido.', 1;

    IF @Correo = '' OR @Correo NOT LIKE '%_@_%._%'
        THROW 72303, 'Ingresa un correo válido.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.Usuario
        WHERE LOWER(Correo) = @Correo
          AND IdUsuario <> @IdUsuario
    )
        THROW 72304, 'Ya existe otro usuario registrado con ese correo.', 1;

    SELECT
        @IdRolActual = u.IdRol,
        @RolActual = r.Nombre,
        @EstadoActual = u.Estado
    FROM dbo.Usuario u
    INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
    WHERE u.IdUsuario = @IdUsuario;

    SELECT @RolNuevo = Nombre
    FROM dbo.Rol
    WHERE IdRol = @IdRol;

    IF @EstadoActual = 1
       AND UPPER(LTRIM(RTRIM(@RolActual))) IN ('ADMINISTRADOR', 'ADMIN')
       AND (
            ISNULL(@Estado, 0) = 0
            OR UPPER(LTRIM(RTRIM(@RolNuevo))) NOT IN ('ADMINISTRADOR', 'ADMIN')
       )
       AND dbo.fn_CantidadAdministradoresActivos() <= 1
    BEGIN
        THROW 72305, 'No puedes quitar o desactivar el último administrador activo del sistema.', 1;
    END;

    UPDATE dbo.Usuario
    SET
        IdRol = @IdRol,
        Correo = @Correo,
        Estado = ISNULL(@Estado, 1)
    WHERE IdUsuario = @IdUsuario;

    EXEC dbo.sp_ObtenerUsuarioPorId @IdUsuario = @IdUsuario;
END;

GO
/* =========================================================
   9. ANULAR COMPRA Y REVERTIR STOCK
   ========================================================= */

CREATE   PROCEDURE sp_AnularCompra
    @IdCompra INT,
    @IdUsuarioRegistro INT,
    @Motivo NVARCHAR(300)
AS
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @BloqueoCaja INT;
    EXEC @BloqueoCaja=sys.sp_getapplock @Resource=N'Sistema3S:operaciones-caja', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
    IF @BloqueoCaja<0 THROW 72002, 'Caja está ocupada. Intenta nuevamente.', 1;
    SET NOCOUNT ON;

    EXEC dbo.sp_ExigirAdministradorAuditoria @IdUsuarioRegistro;
    DECLARE @IdEstadoAnulada INT;
    DECLARE @IdTipoMovimientoSalida INT;
    DECLARE @EstadoActual NVARCHAR(50);

    SELECT @EstadoActual = ec.Nombre
    FROM Compra c
    INNER JOIN EstadoCompra ec ON ec.IdEstadoCompra = c.IdEstadoCompra
    WHERE c.IdCompra = @IdCompra;

    IF @EstadoActual IS NULL
        THROW 64001, 'La compra seleccionada no existe.', 1;

    IF @EstadoActual = 'Anulada'
        THROW 64002, 'La compra ya está anulada.', 1;

    IF @IdUsuarioRegistro <= 0 OR NOT EXISTS (SELECT 1 FROM Usuario WHERE IdUsuario = @IdUsuarioRegistro)
        THROW 64003, 'El usuario que anula la compra no es válido.', 1;

    IF LTRIM(RTRIM(ISNULL(@Motivo, ''))) = ''
        THROW 64004, 'Ingresa el motivo de anulación.', 1;

    SELECT @IdEstadoAnulada = IdEstadoCompra
    FROM EstadoCompra
    WHERE Nombre = 'Anulada';

    SELECT @IdTipoMovimientoSalida = IdTipoMovimientoStock
    FROM TipoMovimientoStock
    WHERE Nombre = 'Salida';

    IF @IdEstadoAnulada IS NULL
        THROW 64005, 'No existe el estado de compra Anulada.', 1;

    IF @IdTipoMovimientoSalida IS NULL
        THROW 64006, 'No existe el tipo de movimiento Salida.', 1;

    IF EXISTS (
        SELECT 1
        FROM (SELECT IdCompra,IdProducto,SUM(Cantidad) Cantidad FROM DetalleCompra GROUP BY IdCompra,IdProducto) dc
        LEFT JOIN Inventario i WITH (UPDLOCK,HOLDLOCK) ON i.IdProducto = dc.IdProducto
        WHERE dc.IdCompra = @IdCompra
          AND (i.IdInventario IS NULL OR i.StockActual < dc.Cantidad)
    )
    BEGIN
        THROW 64007, 'No se puede anular la compra porque uno o más productos ya no tienen stock suficiente para revertir la entrada.', 1;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE Compra
        SET IdEstadoCompra = @IdEstadoAnulada
        WHERE IdCompra = @IdCompra;

        UPDATE i
        SET
            i.StockActual = i.StockActual - dc.Cantidad,
            i.FechaActualizacion = GETDATE()
        FROM Inventario i
        INNER JOIN (SELECT IdCompra,IdProducto,SUM(Cantidad) Cantidad FROM DetalleCompra GROUP BY IdCompra,IdProducto) dc ON dc.IdProducto = i.IdProducto
        WHERE dc.IdCompra = @IdCompra;

        INSERT INTO MovimientoStock (
            IdProducto,
            IdUsuarioRegistro,
            IdTipoMovimientoStock,
            IdVenta,
            IdCompra,
            Cantidad,
            FechaMovimiento,
            Motivo
        )
        SELECT
            dc.IdProducto,
            @IdUsuarioRegistro,
            @IdTipoMovimientoSalida,
            NULL,
            @IdCompra,
            dc.Cantidad,
            GETDATE(),
            LEFT(CONCAT('Reversión por anulación de compra. Motivo: ', @Motivo),300)
        FROM (SELECT IdCompra,IdProducto,SUM(Cantidad) Cantidad FROM DetalleCompra GROUP BY IdCompra,IdProducto) dc
        WHERE dc.IdCompra = @IdCompra;

        EXEC dbo.sp_RevertirCajaCompra @IdCompra, @IdUsuarioRegistro, @Motivo;
        COMMIT TRANSACTION;

        SELECT CAST(1 AS BIT) AS Resultado;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
    COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

GO
/* =========================================================
   9. ANULAR VENTA
   ========================================================= */

CREATE   PROCEDURE sp_AnularVenta
    @IdVenta INT,
    @IdUsuarioRegistro INT,
    @Motivo NVARCHAR(300)
AS
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @BloqueoCaja INT;
    EXEC @BloqueoCaja=sys.sp_getapplock @Resource=N'Sistema3S:operaciones-caja', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
    IF @BloqueoCaja<0 THROW 72002, 'Caja está ocupada. Intenta nuevamente.', 1;
    SET NOCOUNT ON;

    EXEC dbo.sp_ExigirAdministradorAuditoria @IdUsuarioRegistro;
    DECLARE @IdEstadoAnulada INT;
    DECLARE @IdEstadoComprobanteAnulado INT;
    DECLARE @IdTipoMovimientoEntrada INT;
    DECLARE @EstadoActual NVARCHAR(50);

    IF @IdVenta <= 0
        THROW 53001, 'Selecciona una venta válida.', 1;

    IF @IdUsuarioRegistro <= 0 OR NOT EXISTS (SELECT 1 FROM Usuario WHERE IdUsuario = @IdUsuarioRegistro)
        THROW 53002, 'El usuario que anula la venta no es válido.', 1;

    IF @Motivo IS NULL OR LEN(LTRIM(RTRIM(@Motivo))) < 5
        THROW 53003, 'Ingresa un motivo de anulación válido.', 1;

    SELECT @IdEstadoAnulada = IdEstadoVenta
    FROM EstadoVenta
    WHERE Nombre = 'Anulada';

    SELECT @IdEstadoComprobanteAnulado = IdEstadoComprobante
    FROM EstadoComprobante
    WHERE Nombre = 'Anulado';

    SELECT @IdTipoMovimientoEntrada = IdTipoMovimientoStock
    FROM TipoMovimientoStock
    WHERE Nombre = 'Entrada';

    SELECT @EstadoActual = ev.Nombre
    FROM Venta v
    INNER JOIN EstadoVenta ev ON ev.IdEstadoVenta = v.IdEstadoVenta
    WHERE v.IdVenta = @IdVenta;

    IF @EstadoActual IS NULL
        THROW 53004, 'La venta seleccionada no existe.', 1;

    IF @EstadoActual = 'Anulada'
        THROW 53005, 'La venta ya se encuentra anulada.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE Venta
        SET
            IdEstadoVenta = @IdEstadoAnulada,
            Observacion = CONCAT(ISNULL(Observacion + ' | ', ''), 'ANULACIÓN: ', @Motivo)
        WHERE IdVenta = @IdVenta;

        UPDATE Comprobante
        SET IdEstadoComprobante = @IdEstadoComprobanteAnulado
        WHERE IdVenta = @IdVenta;

        UPDATE i
        SET
            i.StockActual = i.StockActual + dv.Cantidad,
            i.FechaActualizacion = GETDATE()
        FROM Inventario i
        INNER JOIN Producto p ON p.IdProducto = i.IdProducto
        INNER JOIN DetalleVenta dv ON dv.IdElementoCatalogo = p.IdElementoCatalogo
        WHERE dv.IdVenta = @IdVenta
          AND p.AplicaInventario = 1;

        INSERT INTO MovimientoStock (
            IdProducto,
            IdUsuarioRegistro,
            IdTipoMovimientoStock,
            IdVenta,
            IdCompra,
            Cantidad,
            FechaMovimiento,
            Motivo
        )
        SELECT
            p.IdProducto,
            @IdUsuarioRegistro,
            @IdTipoMovimientoEntrada,
            @IdVenta,
            NULL,
            dv.Cantidad,
            GETDATE(),
            CONCAT('Reversión por anulación de venta. Motivo: ', @Motivo)
        FROM DetalleVenta dv
        INNER JOIN Producto p ON p.IdElementoCatalogo = dv.IdElementoCatalogo
        WHERE dv.IdVenta = @IdVenta
          AND p.AplicaInventario = 1;

        COMMIT TRANSACTION;

        SELECT 'Venta anulada correctamente.' AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
    COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

GO
/* =========================================================
   23. PROCEDIMIENTO: ASIGNAR PERMISOS A ROL
   @PermisosJson admite:
   [1,2,3]
   o
   [{"idPermiso":1},{"idPermiso":2}]
   ========================================================= */

CREATE   PROCEDURE dbo.sp_AsignarPermisosRol
    @IdRol INT,
    @PermisosJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NombreRol NVARCHAR(100);

    DECLARE @PermisosSeleccionados TABLE (
        IdPermiso INT NOT NULL PRIMARY KEY
    );

    SELECT @NombreRol = Nombre
    FROM dbo.Rol
    WHERE IdRol = @IdRol
      AND Estado = 1;

    IF @NombreRol IS NULL
        THROW 73001, 'El rol seleccionado no existe o está inactivo.', 1;

    IF UPPER(LTRIM(RTRIM(@NombreRol))) IN ('ADMINISTRADOR', 'ADMIN')
    BEGIN
        INSERT INTO dbo.RolPermiso (IdRol, IdPermiso)
        SELECT @IdRol, p.IdPermiso
        FROM dbo.Permiso p
        WHERE p.Estado = 1
          AND NOT EXISTS (
              SELECT 1
              FROM dbo.RolPermiso rp
              WHERE rp.IdRol = @IdRol
                AND rp.IdPermiso = p.IdPermiso
          );

        SELECT
            'El rol Administrador mantiene todos los permisos activos.' AS Mensaje,
            @IdRol AS IdRol;

        RETURN;
    END;

    SET @PermisosJson = NULLIF(LTRIM(RTRIM(ISNULL(@PermisosJson, ''))), '');

    IF @PermisosJson IS NULL OR ISJSON(@PermisosJson) <> 1
        THROW 73002, 'La lista de permisos no tiene formato válido.', 1;

    INSERT INTO @PermisosSeleccionados (IdPermiso)
    SELECT DISTINCT
        COALESCE(
            TRY_CONVERT(INT, j.[value]),
            TRY_CONVERT(INT, JSON_VALUE(j.[value], '$.idPermiso')),
            TRY_CONVERT(INT, JSON_VALUE(j.[value], '$.IdPermiso'))
        )
    FROM OPENJSON(@PermisosJson) j
    WHERE COALESCE(
            TRY_CONVERT(INT, j.[value]),
            TRY_CONVERT(INT, JSON_VALUE(j.[value], '$.idPermiso')),
            TRY_CONVERT(INT, JSON_VALUE(j.[value], '$.IdPermiso'))
          ) IS NOT NULL;

    IF EXISTS (
        SELECT 1
        FROM @PermisosSeleccionados ps
        LEFT JOIN dbo.Permiso p ON p.IdPermiso = ps.IdPermiso AND p.Estado = 1
        WHERE p.IdPermiso IS NULL
    )
        THROW 73003, 'Uno o más permisos seleccionados no son válidos.', 1;

    DELETE FROM dbo.RolPermiso
    WHERE IdRol = @IdRol;

    INSERT INTO dbo.RolPermiso (IdRol, IdPermiso)
    SELECT @IdRol, IdPermiso
    FROM @PermisosSeleccionados;

    SELECT
        'Permisos actualizados correctamente.' AS Mensaje,
        @IdRol AS IdRol;
END;

GO
/* =========================================================
   15. PROCEDIMIENTO: CAMBIAR CONTRASEÑA DE USUARIO
   La contraseña llega como hash desde backend.
   ========================================================= */

CREATE   PROCEDURE dbo.sp_CambiarContrasenaUsuario
    @IdUsuario INT,
    @ContrasenaHash NVARCHAR(510)
AS
BEGIN
    SET NOCOUNT ON;

    SET @ContrasenaHash = LTRIM(RTRIM(ISNULL(@ContrasenaHash, '')));

    IF @IdUsuario <= 0 OR NOT EXISTS (SELECT 1 FROM dbo.Usuario WHERE IdUsuario = @IdUsuario)
        THROW 72401, 'El usuario seleccionado no existe.', 1;

    IF LEN(@ContrasenaHash) < 20
        THROW 72402, 'La contraseña no fue procesada correctamente.', 1;

    UPDATE dbo.Usuario
    SET ContrasenaHash = @ContrasenaHash
    WHERE IdUsuario = @IdUsuario;

    SELECT
        'Contraseña actualizada correctamente.' AS Mensaje,
        @IdUsuario AS IdUsuario;
END;

GO
/* =========================================================
   9. CAMBIAR ESTADO DE COTIZACIÓN
   Flujo:
   Pendiente -> Respondida / Cancelada
   Respondida -> Aprobada / Cancelada
   Aprobada -> Cancelada
   Convertida en venta solo desde venta.
   ========================================================= */

CREATE   PROCEDURE dbo.sp_CambiarEstadoCotizacion
    @IdCotizacion INT,
    @NuevoEstado NVARCHAR(100),
    @IdUsuarioAtencion INT = NULL
AS
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @BloqueoCaja INT;
    EXEC @BloqueoCaja=sys.sp_getapplock @Resource=N'Sistema3S:operaciones-caja', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
    IF @BloqueoCaja<0 THROW 72002, 'Caja está ocupada. Intenta nuevamente.', 1;
    SET NOCOUNT ON;

    IF LTRIM(RTRIM(@NuevoEstado)) = 'Cancelada' EXEC dbo.sp_ExigirAdministradorAuditoria @IdUsuarioAtencion;
    DECLARE @EstadoActual NVARCHAR(100);
    DECLARE @IdNuevoEstado INT;

    SET @NuevoEstado = NULLIF(LTRIM(RTRIM(ISNULL(@NuevoEstado, ''))), '');

    IF @IdCotizacion <= 0
        THROW 65001, 'Selecciona una cotización válida.', 1;

    IF @NuevoEstado IS NULL
        THROW 65002, 'Selecciona el nuevo estado de la cotización.', 1;

    IF @IdUsuarioAtencion IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM Usuario WHERE IdUsuario = @IdUsuarioAtencion)
        THROW 65003, 'El usuario que atiende la cotización no existe.', 1;

    SELECT @EstadoActual = ec.Nombre
    FROM Cotizacion c
    INNER JOIN EstadoCotizacion ec ON ec.IdEstadoCotizacion = c.IdEstadoCotizacion
    WHERE c.IdCotizacion = @IdCotizacion;

    IF @EstadoActual IS NULL
        THROW 65004, 'La cotización seleccionada no existe.', 1;

    IF @EstadoActual = @NuevoEstado
    BEGIN
        SELECT
            @IdCotizacion AS IdCotizacion,
            @EstadoActual AS EstadoCotizacion,
            'La cotización ya se encuentra en ese estado.' AS Mensaje;
        COMMIT TRANSACTION; RETURN;
    END;

    IF @EstadoActual IN ('Cancelada', 'Convertida en venta')
        THROW 65005, 'No se puede modificar una cotización cancelada o convertida en venta.', 1;

    IF @NuevoEstado = 'Convertida en venta'
        THROW 65006, 'La conversión a venta debe realizarse desde el registro de venta.', 1;

    IF @EstadoActual = 'Pendiente'
       AND @NuevoEstado NOT IN ('Respondida', 'Cancelada')
        THROW 65007, 'Una cotización pendiente solo puede pasar a Respondida o Cancelada.', 1;

    IF @EstadoActual = 'Respondida'
       AND @NuevoEstado NOT IN ('Aprobada', 'Cancelada')
        THROW 65008, 'Una cotización respondida solo puede pasar a Aprobada o Cancelada.', 1;

    IF @EstadoActual = 'Aprobada'
       AND @NuevoEstado NOT IN ('Cancelada')
        THROW 65009, 'Una cotización aprobada solo puede cancelarse o convertirse desde ventas.', 1;

    SELECT @IdNuevoEstado = IdEstadoCotizacion
    FROM EstadoCotizacion
    WHERE Nombre = @NuevoEstado
      AND Estado = 1;

    IF @IdNuevoEstado IS NULL
        THROW 65010, 'El nuevo estado seleccionado no existe o está inactivo.', 1;

    UPDATE Cotizacion
    SET
        IdEstadoCotizacion = @IdNuevoEstado,
        IdUsuarioAtencion = COALESCE(@IdUsuarioAtencion, IdUsuarioAtencion),
        FechaActualizacion = GETDATE()
    WHERE IdCotizacion = @IdCotizacion;

    SELECT
        @IdCotizacion AS IdCotizacion,
        @NuevoEstado AS EstadoCotizacion,
        CONCAT('Cotización actualizada a estado ', @NuevoEstado, '.') AS Mensaje;
    COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

GO
/* =========================================================
   10. CANCELAR COTIZACIÓN
   ========================================================= */

CREATE   PROCEDURE dbo.sp_CancelarCotizacion
    @IdCotizacion INT,
    @IdUsuarioAtencion INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    EXEC dbo.sp_CambiarEstadoCotizacion
        @IdCotizacion = @IdCotizacion,
        @NuevoEstado = 'Cancelada',
        @IdUsuarioAtencion = @IdUsuarioAtencion;
END;

GO
/* =========================================================
   13. PROCEDIMIENTO: CERRAR CAJA
   ========================================================= */

CREATE   PROCEDURE dbo.sp_CerrarCaja
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
   18. PROCEDIMIENTO: CREAR ROL
   ========================================================= */

CREATE   PROCEDURE dbo.sp_CrearRol
    @Nombre NVARCHAR(100),
    @Descripcion NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdRol INT;

    SET @Nombre = LTRIM(RTRIM(ISNULL(@Nombre, '')));
    SET @Descripcion = NULLIF(LTRIM(RTRIM(ISNULL(@Descripcion, ''))), '');

    IF @Nombre = ''
        THROW 72601, 'Ingresa el nombre del rol.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Rol WHERE UPPER(Nombre) = UPPER(@Nombre))
        THROW 72602, 'Ya existe un rol registrado con ese nombre.', 1;

    INSERT INTO dbo.Rol (Nombre, Descripcion, Estado)
    VALUES (@Nombre, @Descripcion, 1);

    SET @IdRol = SCOPE_IDENTITY();

    EXEC dbo.sp_ListarRoles @SoloActivos = NULL;
END;

GO
/* =========================================================
   13. PROCEDIMIENTO: CREAR USUARIO
   La contraseña llega como hash desde backend.
   ========================================================= */

CREATE   PROCEDURE dbo.sp_CrearUsuario
    @IdRol INT,
    @Correo NVARCHAR(300),
    @ContrasenaHash NVARCHAR(510),
    @Estado BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdUsuario INT;

    SET @Correo = LOWER(LTRIM(RTRIM(ISNULL(@Correo, ''))));
    SET @ContrasenaHash = LTRIM(RTRIM(ISNULL(@ContrasenaHash, '')));

    IF @IdRol <= 0 OR NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE IdRol = @IdRol AND Estado = 1)
        THROW 72201, 'Selecciona un rol válido.', 1;

    IF @Correo = '' OR @Correo NOT LIKE '%_@_%._%'
        THROW 72202, 'Ingresa un correo válido.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Usuario WHERE LOWER(Correo) = @Correo)
        THROW 72203, 'Ya existe un usuario registrado con ese correo.', 1;

    IF LEN(@ContrasenaHash) < 20
        THROW 72204, 'La contraseña no fue procesada correctamente.', 1;

    INSERT INTO dbo.Usuario (
        IdRol,
        Correo,
        ContrasenaHash,
        Estado,
        FechaRegistro
    )
    VALUES (
        @IdRol,
        @Correo,
        @ContrasenaHash,
        ISNULL(@Estado, 1),
        GETDATE()
    );

    SET @IdUsuario = SCOPE_IDENTITY();

    EXEC dbo.sp_ObtenerUsuarioPorId @IdUsuario = @IdUsuario;
END;

GO
/* =========================================================
   20. PROCEDIMIENTO: DESACTIVAR ROL
   ========================================================= */

CREATE   PROCEDURE dbo.sp_DesactivarRol
    @IdRol INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Rol NVARCHAR(100);

    IF @IdRol <= 0 OR NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE IdRol = @IdRol)
        THROW 72801, 'El rol seleccionado no existe.', 1;

    SELECT @Rol = Nombre
    FROM dbo.Rol
    WHERE IdRol = @IdRol;

    IF UPPER(LTRIM(RTRIM(@Rol))) IN ('ADMINISTRADOR', 'ADMIN')
        THROW 72802, 'No puedes desactivar el rol Administrador.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.Usuario
        WHERE IdRol = @IdRol
          AND Estado = 1
    )
        THROW 72803, 'No puedes desactivar un rol con usuarios activos asignados.', 1;

    UPDATE dbo.Rol
    SET Estado = 0
    WHERE IdRol = @IdRol;

    SELECT
        'Rol desactivado correctamente.' AS Mensaje,
        @IdRol AS IdRol;
END;

GO
/* =========================================================
   16. PROCEDIMIENTO: DESACTIVAR USUARIO
   ========================================================= */

CREATE   PROCEDURE dbo.sp_DesactivarUsuario
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Rol NVARCHAR(100);
    DECLARE @Estado BIT;

    IF @IdUsuario <= 0 OR NOT EXISTS (SELECT 1 FROM dbo.Usuario WHERE IdUsuario = @IdUsuario)
        THROW 72501, 'El usuario seleccionado no existe.', 1;

    SELECT
        @Rol = r.Nombre,
        @Estado = u.Estado
    FROM dbo.Usuario u
    INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
    WHERE u.IdUsuario = @IdUsuario;

    IF @Estado = 1
       AND UPPER(LTRIM(RTRIM(@Rol))) IN ('ADMINISTRADOR', 'ADMIN')
       AND dbo.fn_CantidadAdministradoresActivos() <= 1
    BEGIN
        THROW 72502, 'No puedes desactivar el último administrador activo del sistema.', 1;
    END;

    UPDATE dbo.Usuario
    SET Estado = 0
    WHERE IdUsuario = @IdUsuario;

    SELECT
        'Usuario desactivado correctamente.' AS Mensaje,
        @IdUsuario AS IdUsuario;
END;

GO
CREATE   PROCEDURE dbo.sp_ExigirAdministradorAuditoria @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    IF ISNULL(TRY_CONVERT(INT, SESSION_CONTEXT(N'IdUsuarioAuditoria')), 0) <> ISNULL(@IdUsuario, -1)
       OR NOT EXISTS (SELECT 1 FROM dbo.Usuario u JOIN dbo.Rol r ON r.IdRol=u.IdRol
           WHERE u.IdUsuario=@IdUsuario AND u.Estado=1 AND r.Estado=1 AND LOWER(LTRIM(RTRIM(r.Nombre))) IN ('administrador','admin'))
        THROW 72001, 'Sólo el administrador autenticado puede anular operaciones.', 1;
END;

GO
/* =========================================================
   6. LISTAR COMPRAS
   ========================================================= */

CREATE   PROCEDURE sp_ListarCompras
    @Buscar NVARCHAR(200) = NULL,
    @EstadoPago NVARCHAR(30) = NULL,
    @FechaInicio DATE = NULL,
    @FechaFin DATE = NULL,
    @Pagina INT = 1,
    @TamanioPagina INT = 8
AS
BEGIN
    SET NOCOUNT ON;

    IF @Pagina <= 0 SET @Pagina = 1;
    IF @TamanioPagina <= 0 SET @TamanioPagina = 8;

    ;WITH Base AS (
        SELECT
            c.IdCompra,
            c.IdProveedor,
            c.FechaCompra,
            ISNULL(c.TipoComprobanteProveedor, '-') AS TipoComprobanteProveedor,
            ISNULL(c.SerieComprobante, '-') AS SerieComprobante,
            ISNULL(c.NumeroComprobante, '-') AS NumeroComprobante,
            CONCAT(ISNULL(c.SerieComprobante, '-'), '-', ISNULL(c.NumeroComprobante, '-')) AS DocumentoCompleto,
            p.Ruc AS RucProveedor,
            p.RazonSocial AS RazonSocialProveedor,
            c.Subtotal,
            c.Igv,
            c.Total,
            ISNULL((
                SELECT SUM(pc.MontoPagado)
                FROM PagoCompra pc
                WHERE pc.IdCompra = c.IdCompra
            ), 0) AS TotalPagado,
            c.Total - ISNULL((
                SELECT SUM(pc.MontoPagado)
                FROM PagoCompra pc
                WHERE pc.IdCompra = c.IdCompra
            ), 0) AS SaldoPendiente,
            ec.Nombre AS EstadoCompra,
            CASE
                WHEN ec.Nombre = 'Anulada' THEN 'Anulada'
                WHEN c.Total - ISNULL((SELECT SUM(pc.MontoPagado) FROM PagoCompra pc WHERE pc.IdCompra = c.IdCompra), 0) <= 0 THEN 'Pagada'
                WHEN EXISTS (SELECT 1 FROM CuotaCompra cc WHERE cc.IdCompra = c.IdCompra) THEN 'En cuotas'
                WHEN ISNULL((SELECT SUM(pc.MontoPagado) FROM PagoCompra pc WHERE pc.IdCompra = c.IdCompra), 0) > 0 THEN 'Parcial'
                ELSE 'Pendiente'
            END AS EstadoPago,
            CAST(CASE WHEN EXISTS (SELECT 1 FROM GuiaRemisionCompra g WHERE g.IdCompra = c.IdCompra) THEN 1 ELSE 0 END AS BIT) AS TieneGuia
        FROM Compra c
        INNER JOIN Proveedor p ON p.IdProveedor = c.IdProveedor
        INNER JOIN EstadoCompra ec ON ec.IdEstadoCompra = c.IdEstadoCompra
        WHERE
            (
                @Buscar IS NULL OR @Buscar = '' OR
                p.Ruc LIKE '%' + @Buscar + '%' OR
                p.RazonSocial LIKE '%' + @Buscar + '%' OR
                c.SerieComprobante LIKE '%' + @Buscar + '%' OR
                c.NumeroComprobante LIKE '%' + @Buscar + '%'
            )
            AND (@FechaInicio IS NULL OR CAST(c.FechaCompra AS DATE) >= @FechaInicio)
            AND (@FechaFin IS NULL OR CAST(c.FechaCompra AS DATE) <= @FechaFin)
    ),
    Filtrado AS (
        SELECT *
        FROM Base
        WHERE @EstadoPago IS NULL OR @EstadoPago = '' OR EstadoPago = @EstadoPago
    )
    SELECT
        *,
        COUNT(*) OVER() AS TotalRegistros
    FROM Filtrado
    ORDER BY IdCompra DESC
    OFFSET (@Pagina - 1) * @TamanioPagina ROWS
    FETCH NEXT @TamanioPagina ROWS ONLY;
END

GO
/* =========================================================
   4. LISTAR COTIZACIONES
   ========================================================= */

CREATE   PROCEDURE dbo.sp_ListarCotizaciones
    @Buscar NVARCHAR(150) = NULL,
    @Estado NVARCHAR(50) = NULL,
    @Origen NVARCHAR(60) = NULL,
    @Pagina INT = 1,
    @TamanioPagina INT = 8
AS
BEGIN
    SET NOCOUNT ON;

    SET @Buscar = NULLIF(LTRIM(RTRIM(ISNULL(@Buscar, ''))), '');
    SET @Estado = NULLIF(LTRIM(RTRIM(ISNULL(@Estado, ''))), '');
    SET @Origen = NULLIF(LTRIM(RTRIM(ISNULL(@Origen, ''))), '');

    IF @Pagina <= 0 SET @Pagina = 1;
    IF @TamanioPagina <= 0 SET @TamanioPagina = 8;

    DECLARE @Offset INT = (@Pagina - 1) * @TamanioPagina;

    ;WITH Detalles AS (
        SELECT 
            IdCotizacion,
            COUNT(*) AS CantidadDetalles
        FROM DetalleCotizacion
        GROUP BY IdCotizacion
    )
    SELECT
        COUNT(*) OVER() AS TotalRegistros,

        c.IdCotizacion,
        CONCAT('COT-', RIGHT(REPLICATE('0', 5) + CAST(c.IdCotizacion AS NVARCHAR(20)), 5)) AS CodigoCotizacion,

        c.IdCliente,
        td.Nombre AS TipoDocumentoCliente,
        cli.NumeroDocumento AS DocumentoCliente,

        COALESCE(
            ce.RazonSocial,
            LTRIM(RTRIM(CONCAT(pn.Nombres, ' ', pn.ApellidoPaterno, ' ', ISNULL(pn.ApellidoMaterno, '')))),
            'Cliente sin nombre'
        ) AS Cliente,

        cli.Correo AS CorreoCliente,
        cli.Telefono AS TelefonoCliente,
        cli.Direccion AS DireccionCliente,

        c.IdUsuarioRegistro,
        c.IdUsuarioAtencion,

        c.IdEstadoCotizacion,
        ec.Nombre AS EstadoCotizacion,

        c.FechaCotizacion,
        c.TotalReferencial,
        c.Subtotal,
        c.Descuento,
        c.Igv,
        c.Total,

        c.Observacion,
        c.ArchivoPdf,
        c.CorreoEnviado,
        ISNULL(c.WhatsappEnviado, 0) AS WhatsappEnviado,
        c.OrigenCotizacion,
        c.FechaRespuesta,
        c.CanalRespuesta,
        c.IdVentaGenerada,
        venta.IdVenta AS IdVentaAsociada,

        ISNULL(d.CantidadDetalles, 0) AS CantidadDetalles,

        CASE
            WHEN c.ArchivoPdf IS NOT NULL AND LTRIM(RTRIM(c.ArchivoPdf)) <> '' THEN 1
            ELSE 0
        END AS PdfGenerado,

        CASE
            WHEN ec.Nombre = 'Aprobada'
                 AND venta.IdVenta IS NULL
                 AND c.IdVentaGenerada IS NULL
            THEN 1
            ELSE 0
        END AS PuedeConvertirVenta,

        CASE
            WHEN ec.Nombre IN ('Cancelada', 'Convertida en venta') THEN 0
            ELSE 1
        END AS PuedeGestionar
    FROM Cotizacion c
    INNER JOIN EstadoCotizacion ec ON ec.IdEstadoCotizacion = c.IdEstadoCotizacion
    INNER JOIN Cliente cli ON cli.IdCliente = c.IdCliente
    INNER JOIN TipoDocumento td ON td.IdTipoDocumento = cli.IdTipoDocumento
    LEFT JOIN ClientePersonaNatural pn ON pn.IdCliente = cli.IdCliente
    LEFT JOIN ClienteEmpresa ce ON ce.IdCliente = cli.IdCliente
    LEFT JOIN Detalles d ON d.IdCotizacion = c.IdCotizacion
    OUTER APPLY (
        SELECT TOP 1 v.IdVenta
        FROM Venta v
        WHERE v.IdCotizacion = c.IdCotizacion
        ORDER BY v.IdVenta DESC
    ) venta
    WHERE
        (@Estado IS NULL OR ec.Nombre = @Estado)
        AND (@Origen IS NULL OR c.OrigenCotizacion = @Origen)
        AND (
            @Buscar IS NULL
            OR CONCAT('COT-', RIGHT(REPLICATE('0', 5) + CAST(c.IdCotizacion AS NVARCHAR(20)), 5)) LIKE '%' + @Buscar + '%'
            OR cli.NumeroDocumento LIKE '%' + @Buscar + '%'
            OR COALESCE(ce.RazonSocial, pn.Nombres, '') LIKE '%' + @Buscar + '%'
            OR COALESCE(ce.NombreComercial, pn.ApellidoPaterno, '') LIKE '%' + @Buscar + '%'
            OR ec.Nombre LIKE '%' + @Buscar + '%'
            OR c.OrigenCotizacion LIKE '%' + @Buscar + '%'
            OR ISNULL(c.Observacion, '') LIKE '%' + @Buscar + '%'
        )
    ORDER BY c.FechaCotizacion DESC, c.IdCotizacion DESC
    OFFSET @Offset ROWS FETCH NEXT @TamanioPagina ROWS ONLY;
END;

GO
/* =========================================================
   12. PROCEDIMIENTO: LISTAR MOVIMIENTOS DE CAJA
   ========================================================= */

CREATE   PROCEDURE dbo.sp_ListarMovimientosCaja
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
   21. PROCEDIMIENTO: LISTAR PERMISOS
   ========================================================= */

CREATE   PROCEDURE dbo.sp_ListarPermisos
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        IdPermiso,
        Nombre,
        Descripcion,
        Estado
    FROM dbo.Permiso
    WHERE Estado = 1
    ORDER BY Nombre;
END;

GO
CREATE   PROCEDURE dbo.sp_ListarRoles
    @SoloActivos BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.IdRol,
        r.Nombre,
        r.Descripcion,
        r.Estado,
        COUNT(DISTINCT rp.IdPermiso) AS TotalPermisos,
        COUNT(DISTINCT u.IdUsuario) AS TotalUsuarios
    FROM dbo.Rol r
    LEFT JOIN dbo.RolPermiso rp
        ON rp.IdRol = r.IdRol
    LEFT JOIN dbo.Usuario u
        ON u.IdRol = r.IdRol
       AND u.Estado = 1
    WHERE
        @SoloActivos IS NULL
        OR r.Estado = @SoloActivos
    GROUP BY
        r.IdRol,
        r.Nombre,
        r.Descripcion,
        r.Estado
    ORDER BY
        CASE
            WHEN r.Nombre = 'Administrador' THEN 0
            ELSE 1
        END,
        r.Nombre;
END;

GO
/* =========================================================
   11. PROCEDIMIENTO: LISTAR USUARIOS
   ========================================================= */

CREATE   PROCEDURE dbo.sp_ListarUsuarios
    @Buscar NVARCHAR(300) = NULL,
    @SoloActivos BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Buscar = NULLIF(LTRIM(RTRIM(ISNULL(@Buscar, ''))), '');

    SELECT
        u.IdUsuario,
        u.IdRol,
        r.Nombre AS Rol,
        u.Correo,
        u.Estado,
        u.FechaRegistro,
        ISNULL(permisos.TotalPermisos, 0) AS TotalPermisos
    FROM dbo.Usuario u
    INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
    OUTER APPLY (
        SELECT COUNT(*) AS TotalPermisos
        FROM dbo.RolPermiso rp
        INNER JOIN dbo.Permiso p ON p.IdPermiso = rp.IdPermiso
        WHERE rp.IdRol = u.IdRol
          AND p.Estado = 1
    ) permisos
    WHERE (@SoloActivos IS NULL OR u.Estado = @SoloActivos)
      AND (
            @Buscar IS NULL
         OR u.Correo LIKE '%' + @Buscar + '%'
         OR r.Nombre LIKE '%' + @Buscar + '%'
      )
    ORDER BY u.IdUsuario DESC;
END;

GO
/* =========================================================
   6. LISTAR VENTAS
   ========================================================= */

CREATE PROCEDURE sp_ListarVentas
    @Buscar NVARCHAR(150) = NULL,
    @EstadoPago NVARCHAR(30) = NULL,
    @TipoComprobante NVARCHAR(50) = NULL,
    @OrigenVenta NVARCHAR(30) = NULL,
    @FechaInicio DATE = NULL,
    @FechaFin DATE = NULL,
    @Pagina INT = 1,
    @TamanioPagina INT = 8
AS
BEGIN
    SET NOCOUNT ON;

    IF @Pagina <= 0 SET @Pagina = 1;
    IF @TamanioPagina <= 0 SET @TamanioPagina = 8;

    DECLARE @Offset INT = (@Pagina - 1) * @TamanioPagina;

    ;WITH Base AS (
        SELECT
            v.IdVenta,
            v.IdCliente,
            v.IdCotizacion,
            v.FechaVenta,
            v.Subtotal,
            v.Igv,
            v.Total,
            ev.Nombre AS EstadoVenta,
            tc.Nombre AS TipoComprobante,
            c.Serie,
            c.Numero,
            CONCAT(c.Serie, '-', c.Numero) AS DocumentoCompleto,
            cl.NumeroDocumento AS DocumentoCliente,
            COALESCE(
                ce.RazonSocial,
                CONCAT(cpn.Nombres, ' ', cpn.ApellidoPaterno, ' ', ISNULL(cpn.ApellidoMaterno, ''))
            ) AS Cliente,
            CASE WHEN v.IdCotizacion IS NULL THEN 'Directa' ELSE 'Cotización' END AS OrigenVenta,
            ISNULL(pagos.TotalPagado, 0) AS TotalPagado,
            ROUND(v.Total - ISNULL(pagos.TotalPagado, 0), 2) AS SaldoPendiente,
            CASE
                WHEN ev.Nombre = 'Anulada' THEN 'Anulada'
                WHEN ROUND(v.Total - ISNULL(pagos.TotalPagado, 0), 2) <= 0 THEN 'Pagada'
                WHEN EXISTS (SELECT 1 FROM CuotaVenta cv WHERE cv.IdVenta = v.IdVenta) THEN 'En cuotas'
                WHEN ISNULL(pagos.TotalPagado, 0) > 0 THEN 'Parcial'
                ELSE 'Pendiente'
            END AS EstadoPago
        FROM Venta v
        INNER JOIN EstadoVenta ev ON ev.IdEstadoVenta = v.IdEstadoVenta
        INNER JOIN Cliente cl ON cl.IdCliente = v.IdCliente
        LEFT JOIN ClienteEmpresa ce ON ce.IdCliente = cl.IdCliente
        LEFT JOIN ClientePersonaNatural cpn ON cpn.IdCliente = cl.IdCliente
        INNER JOIN Comprobante c ON c.IdVenta = v.IdVenta
        INNER JOIN TipoComprobante tc ON tc.IdTipoComprobante = c.IdTipoComprobante
        OUTER APPLY (
            SELECT SUM(pv.MontoPagado) AS TotalPagado
            FROM PagoVenta pv
            WHERE pv.IdVenta = v.IdVenta
        ) pagos
        WHERE
            (@Buscar IS NULL OR @Buscar = ''
                OR COALESCE(ce.RazonSocial, CONCAT(cpn.Nombres, ' ', cpn.ApellidoPaterno, ' ', ISNULL(cpn.ApellidoMaterno, ''))) LIKE '%' + @Buscar + '%'
                OR cl.NumeroDocumento LIKE '%' + @Buscar + '%'
                OR c.Serie LIKE '%' + @Buscar + '%'
                OR c.Numero LIKE '%' + @Buscar + '%'
                OR CONCAT(c.Serie, '-', c.Numero) LIKE '%' + @Buscar + '%')
            AND (@TipoComprobante IS NULL OR @TipoComprobante = '' OR tc.Nombre = @TipoComprobante)
            AND (@OrigenVenta IS NULL OR @OrigenVenta = '' OR CASE WHEN v.IdCotizacion IS NULL THEN 'Directa' ELSE 'Cotización' END = @OrigenVenta)
            AND (@FechaInicio IS NULL OR CAST(v.FechaVenta AS DATE) >= @FechaInicio)
            AND (@FechaFin IS NULL OR CAST(v.FechaVenta AS DATE) <= @FechaFin)
    ),
    Filtrada AS (
        SELECT *
        FROM Base
        WHERE (@EstadoPago IS NULL OR @EstadoPago = '' OR EstadoPago = @EstadoPago)
    )
    SELECT
        IdVenta,
        IdCliente,
        IdCotizacion,
        FechaVenta,
        TipoComprobante,
        Serie,
        Numero,
        DocumentoCompleto,
        DocumentoCliente,
        Cliente,
        OrigenVenta,
        Subtotal,
        Igv,
        Total,
        TotalPagado,
        SaldoPendiente,
        EstadoVenta,
        EstadoPago,
        COUNT(*) OVER() AS TotalRegistros
    FROM Filtrada
    ORDER BY FechaVenta DESC
    OFFSET @Offset ROWS FETCH NEXT @TamanioPagina ROWS ONLY;
END

GO
/* =========================================================
   8. PROCEDIMIENTO: LOGIN
   Nota: este SP NO valida contraseña.
   El backend validará la contraseña contra ContrasenaHash.
   ========================================================= */

CREATE   PROCEDURE dbo.sp_LoginUsuario
    @Correo NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;

    SET @Correo = LOWER(LTRIM(RTRIM(ISNULL(@Correo, ''))));

    IF @Correo = ''
        THROW 72001, 'Credenciales incorrectas.', 1;

    SELECT TOP 1
        u.IdUsuario,
        u.IdRol,
        r.Nombre AS Rol,
        u.Correo,
        u.ContrasenaHash,
        u.Estado AS EstadoUsuario,
        r.Estado AS EstadoRol,
        u.FechaRegistro
    FROM dbo.Usuario u
    INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
    WHERE LOWER(u.Correo) = @Correo
      AND u.Estado = 1
      AND r.Estado = 1;

    IF @@ROWCOUNT = 0
        THROW 72002, 'Credenciales incorrectas.', 1;
END;

GO
/* =========================================================
   12. MARCAR COTIZACIÓN COMO CONVERTIDA EN VENTA
   Este SP se llamará después de registrar la venta.
   ========================================================= */

CREATE   PROCEDURE dbo.sp_MarcarCotizacionConvertidaVenta
    @IdCotizacion INT,
    @IdVenta INT,
    @IdUsuarioAtencion INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @EstadoActual NVARCHAR(100);
    DECLARE @IdEstadoConvertida INT;

    IF @IdCotizacion <= 0
        THROW 67001, 'Selecciona una cotización válida.', 1;

    IF @IdVenta <= 0
        THROW 67002, 'La venta generada no es válida.', 1;

    IF @IdUsuarioAtencion IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM Usuario WHERE IdUsuario = @IdUsuarioAtencion)
        THROW 67003, 'El usuario que atiende la cotización no existe.', 1;

    SELECT @EstadoActual = ec.Nombre
    FROM Cotizacion c
    INNER JOIN EstadoCotizacion ec ON ec.IdEstadoCotizacion = c.IdEstadoCotizacion
    WHERE c.IdCotizacion = @IdCotizacion;

    IF @EstadoActual IS NULL
        THROW 67004, 'La cotización seleccionada no existe.', 1;

    IF @EstadoActual <> 'Aprobada'
        THROW 67005, 'Solo una cotización aprobada puede marcarse como convertida en venta.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM Venta
        WHERE IdVenta = @IdVenta
          AND IdCotizacion = @IdCotizacion
    )
    BEGIN
        THROW 67006, 'La venta indicada no pertenece a la cotización seleccionada.', 1;
    END;

    IF EXISTS (
        SELECT 1
        FROM Venta
        WHERE IdCotizacion = @IdCotizacion
          AND IdVenta <> @IdVenta
    )
    BEGIN
        THROW 67007, 'Esta cotización ya tiene otra venta asociada.', 1;
    END;

    SELECT @IdEstadoConvertida = IdEstadoCotizacion
    FROM EstadoCotizacion
    WHERE Nombre = 'Convertida en venta'
      AND Estado = 1;

    IF @IdEstadoConvertida IS NULL
        THROW 67008, 'No existe el estado Convertida en venta para cotizaciones.', 1;

    UPDATE Cotizacion
    SET
        IdEstadoCotizacion = @IdEstadoConvertida,
        IdUsuarioAtencion = COALESCE(@IdUsuarioAtencion, IdUsuarioAtencion),
        IdVentaGenerada = @IdVenta,
        FechaActualizacion = GETDATE()
    WHERE IdCotizacion = @IdCotizacion;

    SELECT
        @IdCotizacion AS IdCotizacion,
        @IdVenta AS IdVenta,
        'Cotización marcada como convertida en venta correctamente.' AS Mensaje;
END;

GO
/* =========================================================
   8. MARCAR COTIZACIÓN COMO RESPONDIDA
   Se usa después de enviar por correo o WhatsApp.
   ========================================================= */

CREATE   PROCEDURE dbo.sp_MarcarCotizacionRespondida
    @IdCotizacion INT,
    @IdUsuarioAtencion INT = NULL,
    @CanalEnvio NVARCHAR(30),
    @ArchivoPdf NVARCHAR(600) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdEstadoActual INT;
    DECLARE @EstadoActual NVARCHAR(100);
    DECLARE @IdEstadoRespondida INT;
    DECLARE @ArchivoFinal NVARCHAR(600);
    DECLARE @CorreoCliente NVARCHAR(150);
    DECLARE @TelefonoCliente NVARCHAR(20);

    SET @CanalEnvio = NULLIF(LTRIM(RTRIM(ISNULL(@CanalEnvio, ''))), '');
    SET @ArchivoPdf = NULLIF(LTRIM(RTRIM(ISNULL(@ArchivoPdf, ''))), '');

    IF @IdCotizacion <= 0
        THROW 64001, 'Selecciona una cotización válida.', 1;

    IF @CanalEnvio NOT IN ('Correo', 'WhatsApp')
        THROW 64002, 'Canal de envío no válido.', 1;

    IF @IdUsuarioAtencion IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM Usuario WHERE IdUsuario = @IdUsuarioAtencion)
        THROW 64003, 'El usuario que atiende la cotización no existe.', 1;

    SELECT
        @IdEstadoActual = c.IdEstadoCotizacion,
        @EstadoActual = ec.Nombre,
        @ArchivoFinal = COALESCE(@ArchivoPdf, c.ArchivoPdf),
        @CorreoCliente = cli.Correo,
        @TelefonoCliente = cli.Telefono
    FROM Cotizacion c
    INNER JOIN EstadoCotizacion ec ON ec.IdEstadoCotizacion = c.IdEstadoCotizacion
    INNER JOIN Cliente cli ON cli.IdCliente = c.IdCliente
    WHERE c.IdCotizacion = @IdCotizacion;

    IF @IdEstadoActual IS NULL
        THROW 64004, 'La cotización seleccionada no existe.', 1;

    IF @EstadoActual IN ('Cancelada', 'Convertida en venta')
        THROW 64005, 'No se puede responder una cotización cancelada o convertida en venta.', 1;

    IF @ArchivoFinal IS NULL
        THROW 64006, 'Primero genera el PDF de la cotización antes de enviarla.', 1;

    IF @CanalEnvio = 'Correo' AND NULLIF(LTRIM(RTRIM(ISNULL(@CorreoCliente, ''))), '') IS NULL
        THROW 64007, 'El cliente no tiene correo registrado. Agrega un correo antes de enviar la cotización.', 1;

    IF @CanalEnvio = 'WhatsApp' AND NULLIF(LTRIM(RTRIM(ISNULL(@TelefonoCliente, ''))), '') IS NULL
        THROW 64008, 'El cliente no tiene teléfono registrado. Agrega un número antes de enviar por WhatsApp.', 1;

    SELECT @IdEstadoRespondida = IdEstadoCotizacion
    FROM EstadoCotizacion
    WHERE Nombre = 'Respondida'
      AND Estado = 1;

    IF @IdEstadoRespondida IS NULL
        THROW 64009, 'No existe el estado Respondida para cotizaciones.', 1;

    UPDATE Cotizacion
    SET
        IdEstadoCotizacion =
            CASE
                WHEN @EstadoActual = 'Pendiente' THEN @IdEstadoRespondida
                ELSE IdEstadoCotizacion
            END,
        IdUsuarioAtencion = COALESCE(@IdUsuarioAtencion, IdUsuarioAtencion),
        ArchivoPdf = @ArchivoFinal,
        CorreoEnviado =
            CASE
                WHEN @CanalEnvio = 'Correo' THEN 1
                ELSE CorreoEnviado
            END,
        WhatsappEnviado =
            CASE
                WHEN @CanalEnvio = 'WhatsApp' THEN 1
                ELSE WhatsappEnviado
            END,
        FechaRespuesta = GETDATE(),
        CanalRespuesta = @CanalEnvio,
        FechaActualizacion = GETDATE()
    WHERE IdCotizacion = @IdCotizacion;

    SELECT
        @IdCotizacion AS IdCotizacion,
        @CanalEnvio AS CanalEnvio,
        'Cotización marcada como respondida correctamente.' AS Mensaje;
END;

GO
/* =========================================================
   8. PROCEDIMIENTO: OBTENER CAJA ACTIVA
   ========================================================= */

CREATE   PROCEDURE dbo.sp_ObtenerCajaActiva
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
   7. DETALLE COMPLETO DE COMPRA
   ========================================================= */

CREATE   PROCEDURE sp_ObtenerCompraDetalle
    @IdCompra INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.IdCompra,
        c.IdProveedor,
        c.FechaCompra,
        ISNULL(c.TipoComprobanteProveedor, '-') AS TipoComprobanteProveedor,
        ISNULL(c.SerieComprobante, '-') AS SerieComprobante,
        ISNULL(c.NumeroComprobante, '-') AS NumeroComprobante,
        CONCAT(ISNULL(c.SerieComprobante, '-'), '-', ISNULL(c.NumeroComprobante, '-')) AS DocumentoCompleto,
        c.FechaEmisionComprobante,
        p.Ruc AS RucProveedor,
        p.RazonSocial AS RazonSocialProveedor,
        p.NombreComercial,
        p.Correo AS CorreoProveedor,
        p.Telefono AS TelefonoProveedor,
        p.Direccion AS DireccionProveedor,
        c.Subtotal,
        c.Igv,
        c.Total,
        ISNULL((SELECT SUM(pc.MontoPagado) FROM PagoCompra pc WHERE pc.IdCompra = c.IdCompra), 0) AS TotalPagado,
        c.Total - ISNULL((SELECT SUM(pc.MontoPagado) FROM PagoCompra pc WHERE pc.IdCompra = c.IdCompra), 0) AS SaldoPendiente,
        ec.Nombre AS EstadoCompra,
        CASE
            WHEN ec.Nombre = 'Anulada' THEN 'Anulada'
            WHEN c.Total - ISNULL((SELECT SUM(pc.MontoPagado) FROM PagoCompra pc WHERE pc.IdCompra = c.IdCompra), 0) <= 0 THEN 'Pagada'
            WHEN EXISTS (SELECT 1 FROM CuotaCompra cc WHERE cc.IdCompra = c.IdCompra) THEN 'En cuotas'
            WHEN ISNULL((SELECT SUM(pc.MontoPagado) FROM PagoCompra pc WHERE pc.IdCompra = c.IdCompra), 0) > 0 THEN 'Parcial'
            ELSE 'Pendiente'
        END AS EstadoPago,
        c.ObservacionCompra
    FROM Compra c
    INNER JOIN Proveedor p ON p.IdProveedor = c.IdProveedor
    INNER JOIN EstadoCompra ec ON ec.IdEstadoCompra = c.IdEstadoCompra
    WHERE c.IdCompra = @IdCompra;

    SELECT
        dc.IdDetalleCompra,
        dc.IdCompra,
        dc.IdProducto,
        pr.IdElementoCatalogo,
        pr.CodigoProducto,
        ec.Nombre AS Producto,
        cat.Nombre AS Categoria,
        mar.Nombre AS Marca,
        um.Nombre AS UnidadMedida,
        dc.Cantidad,
        dc.PrecioCompra,
        dc.Subtotal
    FROM DetalleCompra dc
    INNER JOIN Producto pr ON pr.IdProducto = dc.IdProducto
    INNER JOIN ElementoCatalogo ec ON ec.IdElementoCatalogo = pr.IdElementoCatalogo
    INNER JOIN Categoria cat ON cat.IdCategoria = pr.IdCategoria
    LEFT JOIN Marca mar ON mar.IdMarca = pr.IdMarca
    LEFT JOIN UnidadMedida um ON um.IdUnidadMedida = pr.IdUnidadMedida
    WHERE dc.IdCompra = @IdCompra;

    SELECT
        IdPagoCompra,
        IdCompra,
        MetodoPago,
        MontoPagado,
        FechaPago,
        Observacion
    FROM PagoCompra
    WHERE IdCompra = @IdCompra
    ORDER BY FechaPago;

    SELECT
        IdCuotaCompra,
        IdCompra,
        NumeroCuota,
        FechaVencimiento,
        MontoCuota,
        MontoPagado,
        EstadoCuota
    FROM CuotaCompra
    WHERE IdCompra = @IdCompra
    ORDER BY NumeroCuota;

    SELECT
        IdGuiaRemisionCompra,
        IdCompra,
        NumeroGuia,
        FechaEmision,
        FechaTraslado,
        PuntoPartida,
        PuntoLlegada,
        Transportista,
        RucTransportista,
        PlacaVehiculo,
        Observacion
    FROM GuiaRemisionCompra
    WHERE IdCompra = @IdCompra;
END

GO
/* =========================================================
   6. OBTENER DETALLE DE COTIZACIÓN
   Resultado 1: cabecera
   Resultado 2: detalles
   ========================================================= */

CREATE   PROCEDURE dbo.sp_ObtenerCotizacionDetalle
    @IdCotizacion INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @IdCotizacion <= 0
        THROW 62001, 'Selecciona una cotización válida.', 1;

    IF NOT EXISTS (SELECT 1 FROM Cotizacion WHERE IdCotizacion = @IdCotizacion)
        THROW 62002, 'La cotización seleccionada no existe.', 1;

    SELECT
        c.IdCotizacion,
        CONCAT('COT-', RIGHT(REPLICATE('0', 5) + CAST(c.IdCotizacion AS NVARCHAR(20)), 5)) AS CodigoCotizacion,

        c.IdCliente,
        td.Nombre AS TipoDocumentoCliente,
        cli.NumeroDocumento AS DocumentoCliente,

        COALESCE(
            ce.RazonSocial,
            LTRIM(RTRIM(CONCAT(pn.Nombres, ' ', pn.ApellidoPaterno, ' ', ISNULL(pn.ApellidoMaterno, '')))),
            'Cliente sin nombre'
        ) AS Cliente,

        ce.RazonSocial,
        ce.NombreComercial,
        pn.Nombres,
        pn.ApellidoPaterno,
        pn.ApellidoMaterno,

        cli.Correo AS CorreoCliente,
        cli.Telefono AS TelefonoCliente,
        cli.Direccion AS DireccionCliente,

        c.IdUsuarioRegistro,
        c.IdUsuarioAtencion,

        c.IdEstadoCotizacion,
        ec.Nombre AS EstadoCotizacion,

        c.FechaCotizacion,
        c.TotalReferencial,
        c.Subtotal,
        c.Descuento,
        c.Igv,
        c.Total,

        c.Observacion,
        c.ArchivoPdf,
        c.CorreoEnviado,
        ISNULL(c.WhatsappEnviado, 0) AS WhatsappEnviado,
        c.OrigenCotizacion,
        c.FechaRespuesta,
        c.CanalRespuesta,
        c.IdVentaGenerada,
        venta.IdVenta AS IdVentaAsociada,

        CASE
            WHEN c.ArchivoPdf IS NOT NULL AND LTRIM(RTRIM(c.ArchivoPdf)) <> '' THEN 1
            ELSE 0
        END AS PdfGenerado,

        CASE
            WHEN ec.Nombre = 'Aprobada'
                 AND venta.IdVenta IS NULL
                 AND c.IdVentaGenerada IS NULL
            THEN 1
            ELSE 0
        END AS PuedeConvertirVenta
    FROM Cotizacion c
    INNER JOIN EstadoCotizacion ec ON ec.IdEstadoCotizacion = c.IdEstadoCotizacion
    INNER JOIN Cliente cli ON cli.IdCliente = c.IdCliente
    INNER JOIN TipoDocumento td ON td.IdTipoDocumento = cli.IdTipoDocumento
    LEFT JOIN ClientePersonaNatural pn ON pn.IdCliente = cli.IdCliente
    LEFT JOIN ClienteEmpresa ce ON ce.IdCliente = cli.IdCliente
    OUTER APPLY (
        SELECT TOP 1 v.IdVenta
        FROM Venta v
        WHERE v.IdCotizacion = c.IdCotizacion
        ORDER BY v.IdVenta DESC
    ) venta
    WHERE c.IdCotizacion = @IdCotizacion;

    SELECT
        dc.IdDetalleCotizacion,
        dc.IdCotizacion,
        dc.IdElementoCatalogo,

        e.Nombre AS Elemento,
        te.Nombre AS TipoElemento,

        p.IdProducto,
        p.CodigoProducto,

        s.IdServicio,

        COALESCE(p.CodigoProducto, CONCAT('SV-', s.IdServicio), '-') AS Codigo,

        dc.Cantidad,
        dc.PrecioUnitario,
        dc.Subtotal,
        dc.Observacion
    FROM DetalleCotizacion dc
    INNER JOIN ElementoCatalogo e ON e.IdElementoCatalogo = dc.IdElementoCatalogo
    INNER JOIN TipoElemento te ON te.IdTipoElemento = e.IdTipoElemento
    LEFT JOIN Producto p ON p.IdElementoCatalogo = e.IdElementoCatalogo
    LEFT JOIN Servicio s ON s.IdElementoCatalogo = e.IdElementoCatalogo
    WHERE dc.IdCotizacion = @IdCotizacion
    ORDER BY dc.IdDetalleCotizacion;
END;

GO
/* =========================================================
   22. PROCEDIMIENTO: OBTENER PERMISOS POR ROL
   ========================================================= */

CREATE   PROCEDURE dbo.sp_ObtenerPermisosPorRol
    @IdRol INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NombreRol NVARCHAR(100);

    SELECT @NombreRol = Nombre
    FROM dbo.Rol
    WHERE IdRol = @IdRol
      AND Estado = 1;

    IF @NombreRol IS NULL
        THROW 72901, 'El rol seleccionado no existe o está inactivo.', 1;

    IF UPPER(LTRIM(RTRIM(@NombreRol))) IN ('ADMINISTRADOR', 'ADMIN')
    BEGIN
        SELECT
            p.IdPermiso,
            p.Nombre,
            p.Descripcion,
            CAST(1 AS BIT) AS Asignado
        FROM dbo.Permiso p
        WHERE p.Estado = 1
        ORDER BY p.Nombre;

        RETURN;
    END;

    SELECT
        p.IdPermiso,
        p.Nombre,
        p.Descripcion,
        CASE
            WHEN rp.IdPermiso IS NULL THEN CAST(0 AS BIT)
            ELSE CAST(1 AS BIT)
        END AS Asignado
    FROM dbo.Permiso p
    LEFT JOIN dbo.RolPermiso rp
        ON rp.IdPermiso = p.IdPermiso
       AND rp.IdRol = @IdRol
    WHERE p.Estado = 1
    ORDER BY p.Nombre;
END;

GO
/* =========================================================
   9. PROCEDIMIENTO: OBTENER PERMISOS DEL USUARIO
   Administrador devuelve todos los permisos activos.
   ========================================================= */

CREATE   PROCEDURE dbo.sp_ObtenerPermisosUsuario
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdRol INT;
    DECLARE @NombreRol NVARCHAR(100);

    SELECT
        @IdRol = u.IdRol,
        @NombreRol = r.Nombre
    FROM dbo.Usuario u
    INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
    WHERE u.IdUsuario = @IdUsuario
      AND u.Estado = 1
      AND r.Estado = 1;

    IF @IdRol IS NULL
        THROW 72101, 'Usuario no válido o inactivo.', 1;

    IF UPPER(LTRIM(RTRIM(@NombreRol))) IN ('ADMINISTRADOR', 'ADMIN')
    BEGIN
        SELECT
            p.IdPermiso,
            p.Nombre,
            p.Descripcion,
            CAST(1 AS BIT) AS Asignado
        FROM dbo.Permiso p
        WHERE p.Estado = 1
        ORDER BY p.Nombre;

        RETURN;
    END;

    SELECT
        p.IdPermiso,
        p.Nombre,
        p.Descripcion,
        CAST(1 AS BIT) AS Asignado
    FROM dbo.RolPermiso rp
    INNER JOIN dbo.Permiso p ON p.IdPermiso = rp.IdPermiso
    WHERE rp.IdRol = @IdRol
      AND p.Estado = 1
    ORDER BY p.Nombre;
END;

GO
/* =========================================================
   11. PROCEDIMIENTO: RESUMEN DE CAJA
   ========================================================= */

CREATE   PROCEDURE dbo.sp_ObtenerResumenCaja
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
   1. PROCEDURE PARA PREVISUALIZAR SIGUIENTE COMPROBANTE
   ========================================================= */

CREATE   PROCEDURE sp_ObtenerSiguienteNumeroComprobante
    @TipoComprobante NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Serie NVARCHAR(10);
    DECLARE @UltimoNumero INT;
    DECLARE @SiguienteNumero INT;
    DECLARE @Numero NVARCHAR(20);

    SET @TipoComprobante = LTRIM(RTRIM(@TipoComprobante));

    IF @TipoComprobante = 'Factura'
        SET @Serie = 'F001';
    ELSE IF @TipoComprobante = 'Boleta'
        SET @Serie = 'B001';
    ELSE IF @TipoComprobante = 'Nota de venta'
        SET @Serie = 'NV01';
    ELSE
        THROW 54001, 'Tipo de comprobante no válido.', 1;

    SELECT @UltimoNumero = ISNULL(MAX(TRY_CONVERT(INT, Numero)), 0)
    FROM Comprobante
    WHERE Serie = @Serie;

    SET @SiguienteNumero = @UltimoNumero + 1;
    SET @Numero = RIGHT(REPLICATE('0', 6) + CAST(@SiguienteNumero AS NVARCHAR(20)), 6);

    SELECT
        @Serie AS Serie,
        @Numero AS Numero,
        CONCAT(@Serie, '-', @Numero) AS DocumentoCompleto;
END

GO
/* =========================================================
   12. PROCEDIMIENTO: OBTENER USUARIO POR ID
   ========================================================= */

CREATE   PROCEDURE dbo.sp_ObtenerUsuarioPorId
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        u.IdUsuario,
        u.IdRol,
        r.Nombre AS Rol,
        u.Correo,
        u.Estado,
        u.FechaRegistro
    FROM dbo.Usuario u
    INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
    WHERE u.IdUsuario = @IdUsuario;
END;

GO
/* =========================================================
   7. OBTENER DETALLE DE VENTA
   ========================================================= */

CREATE PROCEDURE sp_ObtenerVentaDetalle
    @IdVenta INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        v.IdVenta,
        v.IdCliente,
        v.IdCotizacion,
        v.FechaVenta,
        v.Subtotal,
        v.Igv,
        v.Total,
        v.Observacion,
        ev.Nombre AS EstadoVenta,
        tc.Nombre AS TipoComprobante,
        c.Serie,
        c.Numero,
        CONCAT(c.Serie, '-', c.Numero) AS DocumentoCompleto,
        c.FechaEmision,
        cl.NumeroDocumento AS DocumentoCliente,
        td.Nombre AS TipoDocumentoCliente,
        COALESCE(
            ce.RazonSocial,
            CONCAT(cpn.Nombres, ' ', cpn.ApellidoPaterno, ' ', ISNULL(cpn.ApellidoMaterno, ''))
        ) AS Cliente,
        cl.Correo AS CorreoCliente,
        cl.Telefono AS TelefonoCliente,
        cl.Direccion AS DireccionCliente,
        ISNULL(pagos.TotalPagado, 0) AS TotalPagado,
        ROUND(v.Total - ISNULL(pagos.TotalPagado, 0), 2) AS SaldoPendiente,
        CASE
            WHEN ev.Nombre = 'Anulada' THEN 'Anulada'
            WHEN ROUND(v.Total - ISNULL(pagos.TotalPagado, 0), 2) <= 0 THEN 'Pagada'
            WHEN EXISTS (SELECT 1 FROM CuotaVenta cv WHERE cv.IdVenta = v.IdVenta) THEN 'En cuotas'
            WHEN ISNULL(pagos.TotalPagado, 0) > 0 THEN 'Parcial'
            ELSE 'Pendiente'
        END AS EstadoPago,
        CASE WHEN v.IdCotizacion IS NULL THEN 'Directa' ELSE 'Cotización' END AS OrigenVenta
    FROM Venta v
    INNER JOIN EstadoVenta ev ON ev.IdEstadoVenta = v.IdEstadoVenta
    INNER JOIN Cliente cl ON cl.IdCliente = v.IdCliente
    INNER JOIN TipoDocumento td ON td.IdTipoDocumento = cl.IdTipoDocumento
    LEFT JOIN ClienteEmpresa ce ON ce.IdCliente = cl.IdCliente
    LEFT JOIN ClientePersonaNatural cpn ON cpn.IdCliente = cl.IdCliente
    INNER JOIN Comprobante c ON c.IdVenta = v.IdVenta
    INNER JOIN TipoComprobante tc ON tc.IdTipoComprobante = c.IdTipoComprobante
    OUTER APPLY (
        SELECT SUM(pv.MontoPagado) AS TotalPagado
        FROM PagoVenta pv
        WHERE pv.IdVenta = v.IdVenta
    ) pagos
    WHERE v.IdVenta = @IdVenta;

    SELECT
        dv.IdDetalleVenta,
        dv.IdElementoCatalogo,
        ec.Nombre AS Elemento,
        te.Nombre AS TipoElemento,
        p.IdProducto,
        p.CodigoProducto,
        s.IdServicio,
        dv.Cantidad,
        dv.PrecioUnitario,
        dv.Subtotal
    FROM DetalleVenta dv
    INNER JOIN ElementoCatalogo ec ON ec.IdElementoCatalogo = dv.IdElementoCatalogo
    INNER JOIN TipoElemento te ON te.IdTipoElemento = ec.IdTipoElemento
    LEFT JOIN Producto p ON p.IdElementoCatalogo = ec.IdElementoCatalogo
    LEFT JOIN Servicio s ON s.IdElementoCatalogo = ec.IdElementoCatalogo
    WHERE dv.IdVenta = @IdVenta
    ORDER BY dv.IdDetalleVenta;

    SELECT
        IdPagoVenta,
        IdVenta,
        MetodoPago,
        MontoPagado,
        FechaPago,
        Observacion
    FROM PagoVenta
    WHERE IdVenta = @IdVenta
    ORDER BY FechaPago;

    SELECT
        IdCuotaVenta,
        IdVenta,
        NumeroCuota,
        FechaVencimiento,
        MontoCuota,
        MontoPagado,
        EstadoCuota
    FROM CuotaVenta
    WHERE IdVenta = @IdVenta
    ORDER BY NumeroCuota;
END

GO
/* =========================================================
   11. PREPARAR COTIZACIÓN PARA VENTA
   Solo si está Aprobada y no fue convertida.
   Resultado 1: cabecera
   Resultado 2: detalles
   ========================================================= */

CREATE   PROCEDURE dbo.sp_PrepararCotizacionParaVenta
    @IdCotizacion INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @EstadoActual NVARCHAR(100);
    DECLARE @IdVentaExistente INT;

    IF @IdCotizacion <= 0
        THROW 66001, 'Selecciona una cotización válida.', 1;

    SELECT
        @EstadoActual = ec.Nombre,
        @IdVentaExistente = venta.IdVenta
    FROM Cotizacion c
    INNER JOIN EstadoCotizacion ec ON ec.IdEstadoCotizacion = c.IdEstadoCotizacion
    OUTER APPLY (
        SELECT TOP 1 v.IdVenta
        FROM Venta v
        WHERE v.IdCotizacion = c.IdCotizacion
        ORDER BY v.IdVenta DESC
    ) venta
    WHERE c.IdCotizacion = @IdCotizacion;

    IF @EstadoActual IS NULL
        THROW 66002, 'La cotización seleccionada no existe.', 1;

    IF @EstadoActual <> 'Aprobada'
        THROW 66003, 'Solo una cotización aprobada puede convertirse en venta.', 1;

    IF @IdVentaExistente IS NOT NULL
        THROW 66004, 'Esta cotización ya fue convertida en venta.', 1;

    EXEC dbo.sp_ObtenerCotizacionDetalle @IdCotizacion = @IdCotizacion;
END;

GO
CREATE   PROCEDURE dbo.sp_RecalcularCajaAuditoria @IdCaja INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE c SET TotalIngresos=s.ingresos, TotalEgresos=s.egresos,
        SaldoSistema=c.SaldoInicial+s.ingresos-s.egresos
    FROM dbo.Caja c CROSS APPLY (
        SELECT ISNULL(SUM(CASE WHEN t.Nombre IN ('Ingreso por venta','Ingreso manual','Ajuste ingreso') THEN m.Monto ELSE 0 END),0) ingresos,
            ISNULL(SUM(CASE WHEN t.Nombre IN ('Egreso por compra','Egreso manual','Ajuste egreso') THEN m.Monto ELSE 0 END),0) egresos
        FROM dbo.MovimientoCaja m JOIN dbo.TipoMovimientoCaja t ON t.IdTipoMovimientoCaja=m.IdTipoMovimientoCaja WHERE m.IdCaja=c.IdCaja AND m.Estado=1
    ) s WHERE c.IdCaja=@IdCaja;
END;

GO
CREATE   PROCEDURE dbo.sp_RegistrarClienteWeb
    @TipoDocumento NVARCHAR(30),
    @NumeroDocumento NVARCHAR(20),
    @Correo NVARCHAR(150),
    @Telefono NVARCHAR(20) = NULL,
    @Direccion NVARCHAR(250) = NULL,
    @ContrasenaHash NVARCHAR(255),

    @Nombres NVARCHAR(100) = NULL,
    @ApellidoPaterno NVARCHAR(100) = NULL,
    @ApellidoMaterno NVARCHAR(100) = NULL,

    @RazonSocial NVARCHAR(200) = NULL,
    @NombreComercial NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdTipoDocumento INT;
    DECLARE @IdTipoCliente INT;
    DECLARE @IdRolCliente INT;
    DECLARE @IdCliente INT;
    DECLARE @IdUsuario INT;
    DECLARE @EsEmpresa BIT = 0;
    DECLARE @NombreCliente NVARCHAR(250);
    DECLARE @Mensaje NVARCHAR(500);

    SET @TipoDocumento = UPPER(LTRIM(RTRIM(ISNULL(@TipoDocumento, ''))));
    SET @NumeroDocumento = LTRIM(RTRIM(ISNULL(@NumeroDocumento, '')));
    SET @Correo = LOWER(LTRIM(RTRIM(ISNULL(@Correo, ''))));
    SET @Telefono = NULLIF(LTRIM(RTRIM(ISNULL(@Telefono, ''))), '');
    SET @Direccion = NULLIF(LTRIM(RTRIM(ISNULL(@Direccion, ''))), '');
    SET @ContrasenaHash = LTRIM(RTRIM(ISNULL(@ContrasenaHash, '')));

    SET @Nombres = NULLIF(LTRIM(RTRIM(ISNULL(@Nombres, ''))), '');
    SET @ApellidoPaterno = NULLIF(LTRIM(RTRIM(ISNULL(@ApellidoPaterno, ''))), '');
    SET @ApellidoMaterno = NULLIF(LTRIM(RTRIM(ISNULL(@ApellidoMaterno, ''))), '');

    SET @RazonSocial = NULLIF(LTRIM(RTRIM(ISNULL(@RazonSocial, ''))), '');
    SET @NombreComercial = NULLIF(LTRIM(RTRIM(ISNULL(@NombreComercial, ''))), '');

    IF @TipoDocumento NOT IN ('DNI', 'RUC')
        THROW 81001, 'Selecciona un tipo de documento válido.', 1;

    IF @TipoDocumento = 'DNI' AND LEN(@NumeroDocumento) <> 8
        THROW 81002, 'El DNI debe tener 8 dígitos.', 1;

    IF @TipoDocumento = 'RUC' AND LEN(@NumeroDocumento) <> 11
        THROW 81003, 'El RUC debe tener 11 dígitos.', 1;

    IF @NumeroDocumento LIKE '%[^0-9]%'
        THROW 81004, 'El número de documento solo debe contener dígitos.', 1;

    IF @Correo = '' OR @Correo NOT LIKE '%_@_%._%'
        THROW 81005, 'Ingresa un correo válido.', 1;

    IF LEN(@ContrasenaHash) < 20
        THROW 81006, 'La contraseña no fue procesada correctamente.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Usuario WHERE LOWER(Correo) = @Correo)
        THROW 81007, 'Ya existe una cuenta registrada con este correo.', 1;

    SELECT @IdTipoDocumento = IdTipoDocumento
    FROM dbo.TipoDocumento
    WHERE UPPER(Nombre) = @TipoDocumento
      AND Estado = 1;

    IF @IdTipoDocumento IS NULL
        THROW 81008, 'El tipo de documento no está configurado en el sistema.', 1;

    IF @TipoDocumento = 'RUC'
    BEGIN
        SET @EsEmpresa = 1;

        SELECT @IdTipoCliente = IdTipoCliente
        FROM dbo.TipoCliente
        WHERE UPPER(Nombre) = 'EMPRESA'
          AND Estado = 1;

        IF @RazonSocial IS NULL
            THROW 81009, 'Ingresa la razón social de la empresa.', 1;
    END
    ELSE
    BEGIN
        SET @EsEmpresa = 0;

        SELECT @IdTipoCliente = IdTipoCliente
        FROM dbo.TipoCliente
        WHERE UPPER(Nombre) = 'PERSONA NATURAL'
          AND Estado = 1;

        IF @Nombres IS NULL OR @ApellidoPaterno IS NULL
            THROW 81010, 'Ingresa nombres y apellido paterno.', 1;
    END

    IF @IdTipoCliente IS NULL
        THROW 81011, 'El tipo de cliente no está configurado en el sistema.', 1;

    SELECT @IdRolCliente = IdRol
    FROM dbo.Rol
    WHERE UPPER(Nombre) = 'CLIENTE'
      AND Estado = 1;

    IF @IdRolCliente IS NULL
        THROW 81012, 'El rol Cliente no está configurado en el sistema.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @IdCliente = IdCliente
        FROM dbo.Cliente
        WHERE IdTipoDocumento = @IdTipoDocumento
          AND NumeroDocumento = @NumeroDocumento;

        IF @IdCliente IS NOT NULL
        BEGIN
            IF EXISTS (
                SELECT 1
                FROM dbo.Cliente
                WHERE IdCliente = @IdCliente
                  AND IdUsuario IS NOT NULL
            )
            BEGIN
                THROW 81013, 'Este cliente ya tiene una cuenta web registrada.', 1;
            END

            UPDATE dbo.Cliente
            SET
                Correo = @Correo,
                Telefono = COALESCE(@Telefono, Telefono),
                Direccion = COALESCE(@Direccion, Direccion),
                Estado = 1
            WHERE IdCliente = @IdCliente;

            IF @EsEmpresa = 1
            BEGIN
                IF EXISTS (SELECT 1 FROM dbo.ClienteEmpresa WHERE IdCliente = @IdCliente)
                BEGIN
                    UPDATE dbo.ClienteEmpresa
                    SET
                        RazonSocial = @RazonSocial,
                        NombreComercial = @NombreComercial
                    WHERE IdCliente = @IdCliente;
                END
                ELSE
                BEGIN
                    INSERT INTO dbo.ClienteEmpresa (IdCliente, RazonSocial, NombreComercial)
                    VALUES (@IdCliente, @RazonSocial, @NombreComercial);
                END
            END
            ELSE
            BEGIN
                IF EXISTS (SELECT 1 FROM dbo.ClientePersonaNatural WHERE IdCliente = @IdCliente)
                BEGIN
                    UPDATE dbo.ClientePersonaNatural
                    SET
                        Nombres = @Nombres,
                        ApellidoPaterno = @ApellidoPaterno,
                        ApellidoMaterno = @ApellidoMaterno
                    WHERE IdCliente = @IdCliente;
                END
                ELSE
                BEGIN
                    INSERT INTO dbo.ClientePersonaNatural (
                        IdCliente,
                        Nombres,
                        ApellidoPaterno,
                        ApellidoMaterno
                    )
                    VALUES (
                        @IdCliente,
                        @Nombres,
                        @ApellidoPaterno,
                        @ApellidoMaterno
                    );
                END
            END
        END
        ELSE
        BEGIN
            INSERT INTO dbo.Cliente (
                IdUsuario,
                IdTipoCliente,
                IdTipoDocumento,
                IdUbigeo,
                NumeroDocumento,
                Correo,
                Telefono,
                Direccion,
                Estado,
                FechaRegistro
            )
            VALUES (
                NULL,
                @IdTipoCliente,
                @IdTipoDocumento,
                NULL,
                @NumeroDocumento,
                @Correo,
                @Telefono,
                @Direccion,
                1,
                GETDATE()
            );

            SET @IdCliente = SCOPE_IDENTITY();

            IF @EsEmpresa = 1
            BEGIN
                INSERT INTO dbo.ClienteEmpresa (
                    IdCliente,
                    RazonSocial,
                    NombreComercial
                )
                VALUES (
                    @IdCliente,
                    @RazonSocial,
                    @NombreComercial
                );
            END
            ELSE
            BEGIN
                INSERT INTO dbo.ClientePersonaNatural (
                    IdCliente,
                    Nombres,
                    ApellidoPaterno,
                    ApellidoMaterno
                )
                VALUES (
                    @IdCliente,
                    @Nombres,
                    @ApellidoPaterno,
                    @ApellidoMaterno
                );
            END
        END

        INSERT INTO dbo.Usuario (
            IdRol,
            Correo,
            ContrasenaHash,
            Estado,
            FechaRegistro
        )
        VALUES (
            @IdRolCliente,
            @Correo,
            @ContrasenaHash,
            1,
            GETDATE()
        );

        SET @IdUsuario = SCOPE_IDENTITY();

        UPDATE dbo.Cliente
        SET IdUsuario = @IdUsuario
        WHERE IdCliente = @IdCliente;

        IF @EsEmpresa = 1
        BEGIN
            SET @NombreCliente = @RazonSocial;
            SET @Mensaje = 'Cuenta empresarial creada correctamente. Ya puedes iniciar sesión y solicitar cotizaciones web para tu empresa.';
        END
        ELSE
        BEGIN
            SET @NombreCliente = CONCAT(@Nombres, ' ', @ApellidoPaterno, ISNULL(' ' + @ApellidoMaterno, ''));
            SET @Mensaje = 'Cuenta creada correctamente. Ya puedes iniciar sesión y solicitar cotizaciones web.';
        END

        COMMIT TRANSACTION;

        SELECT
            @IdCliente AS IdCliente,
            @IdUsuario AS IdUsuario,
            @IdRolCliente AS IdRol,
            @Correo AS Correo,
            'Cliente' AS Rol,
            @TipoDocumento AS TipoDocumento,
            @NumeroDocumento AS NumeroDocumento,
            CASE WHEN @EsEmpresa = 1 THEN 'Empresa' ELSE 'Persona Natural' END AS TipoCliente,
            @NombreCliente AS NombreCliente,
            @EsEmpresa AS EsEmpresa,
            @Mensaje AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
END;

GO
CREATE   PROCEDURE sp_RegistrarCompraCompleta
    @IdProveedor INT,
    @IdUsuarioRegistro INT,

    @TipoComprobanteProveedor NVARCHAR(50),
    @SerieComprobante NVARCHAR(20),
    @NumeroComprobante NVARCHAR(30),
    @FechaEmisionComprobante DATE,
    @ObservacionCompra NVARCHAR(500) = NULL,

    @TieneGuia BIT = 0,
    @NumeroGuia NVARCHAR(50) = NULL,
    @FechaEmisionGuia DATE = NULL,
    @FechaTrasladoGuia DATE = NULL,
    @PuntoPartida NVARCHAR(250) = NULL,
    @PuntoLlegada NVARCHAR(250) = NULL,
    @Transportista NVARCHAR(200) = NULL,
    @RucTransportista NVARCHAR(20) = NULL,
    @PlacaVehiculo NVARCHAR(20) = NULL,
    @ObservacionGuia NVARCHAR(500) = NULL,

    @TipoPago NVARCHAR(30),
    @MetodoPago NVARCHAR(50),
    @MontoPagado DECIMAL(18,2),
    @ObservacionPago NVARCHAR(300) = NULL,
    @NumeroCuotas INT = NULL,
    @FechaPrimerVencimiento DATE = NULL,

    @Detalles CompraDetalleType READONLY,

    -- NUEVO:
    -- JSON opcional para recibir las fechas exactas de cada cuota.
    -- Si viene NULL o vacío, se usa la lógica anterior mensual con @FechaPrimerVencimiento.
    @CuotasJson NVARCHAR(MAX) = NULL
AS
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @BloqueoCaja INT;
    EXEC @BloqueoCaja=sys.sp_getapplock @Resource=N'Sistema3S:operaciones-caja', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
    IF @BloqueoCaja<0 THROW 72002, 'Caja está ocupada. Intenta nuevamente.', 1;
    SET NOCOUNT ON;

    DECLARE @IdCompra INT;
    DECLARE @IdEstadoCompra INT;
    DECLARE @IdTipoMovimientoEntrada INT;

    DECLARE @Subtotal DECIMAL(18,2);
    DECLARE @Igv DECIMAL(18,2);
    DECLARE @Total DECIMAL(18,2);
    DECLARE @SaldoPendiente DECIMAL(18,2);
    DECLARE @EstadoPago NVARCHAR(30);

    DECLARE @UsaCuotasPersonalizadas BIT = 0;

    DECLARE @CuotasProgramadas TABLE (
        NumeroCuota INT NOT NULL,
        FechaVencimiento DATE NOT NULL
    );

    SET @TipoComprobanteProveedor = LTRIM(RTRIM(ISNULL(@TipoComprobanteProveedor, '')));
    SET @SerieComprobante = UPPER(LTRIM(RTRIM(ISNULL(@SerieComprobante, ''))));
    SET @NumeroComprobante = LTRIM(RTRIM(ISNULL(@NumeroComprobante, '')));
    SET @TipoPago = LTRIM(RTRIM(ISNULL(@TipoPago, '')));
    SET @MetodoPago = LTRIM(RTRIM(ISNULL(@MetodoPago, '')));

    SET @CuotasJson = NULLIF(LTRIM(RTRIM(ISNULL(@CuotasJson, ''))), '');

    IF @IdProveedor <= 0
        THROW 62001, 'Selecciona un proveedor válido.', 1;

    IF NOT EXISTS (SELECT 1 FROM Proveedor WHERE IdProveedor = @IdProveedor AND Estado = 1)
        THROW 62002, 'El proveedor seleccionado no existe o está inactivo.', 1;

    IF @IdUsuarioRegistro <= 0 OR NOT EXISTS (SELECT 1 FROM Usuario WHERE IdUsuario = @IdUsuarioRegistro)
        THROW 62003, 'El usuario que registra la compra no es válido.', 1;

    IF @TipoComprobanteProveedor = ''
        THROW 62004, 'Selecciona el tipo de comprobante recibido.', 1;

    IF @SerieComprobante = ''
        THROW 62005, 'Ingresa la serie del comprobante recibido.', 1;

    IF @NumeroComprobante = ''
        THROW 62006, 'Ingresa el número del comprobante recibido.', 1;

    IF @FechaEmisionComprobante IS NULL
        THROW 62007, 'Ingresa la fecha de emisión del comprobante recibido.', 1;

    IF NOT EXISTS (SELECT 1 FROM @Detalles)
        THROW 62008, 'Agrega al menos un producto a la compra.', 1;

    IF EXISTS (SELECT 1 FROM @Detalles WHERE Cantidad <= 0)
        THROW 62009, 'La cantidad de cada producto debe ser mayor a 0.', 1;

    IF EXISTS (SELECT 1 FROM @Detalles WHERE PrecioCompra <= 0)
        THROW 62010, 'El precio de compra debe ser mayor a 0.', 1;

    IF EXISTS (
        SELECT 1
        FROM @Detalles d
        LEFT JOIN Producto p ON p.IdProducto = d.IdProducto
        WHERE p.IdProducto IS NULL
    )
    BEGIN
        THROW 62011, 'Uno o más productos seleccionados no existen.', 1;
    END;

    IF @TieneGuia = 1
    BEGIN
        IF LTRIM(RTRIM(ISNULL(@NumeroGuia, ''))) = ''
            THROW 62012, 'Ingresa el número de guía de remisión.', 1;

        IF @FechaEmisionGuia IS NULL
            THROW 62013, 'Ingresa la fecha de emisión de la guía.', 1;

        IF @FechaTrasladoGuia IS NULL
            THROW 62014, 'Ingresa la fecha de traslado de la guía.', 1;

        IF LTRIM(RTRIM(ISNULL(@PuntoPartida, ''))) = ''
            THROW 62015, 'Ingresa el punto de partida.', 1;

        IF LTRIM(RTRIM(ISNULL(@PuntoLlegada, ''))) = ''
            THROW 62016, 'Ingresa el punto de llegada.', 1;
    END;

    SELECT @IdEstadoCompra = IdEstadoCompra
    FROM EstadoCompra
    WHERE Nombre = 'Registrada';

    SELECT @IdTipoMovimientoEntrada = IdTipoMovimientoStock
    FROM TipoMovimientoStock
    WHERE Nombre = 'Entrada';

    IF @IdEstadoCompra IS NULL
        THROW 62017, 'No existe el estado de compra Registrada.', 1;

    IF @IdTipoMovimientoEntrada IS NULL
        THROW 62018, 'No existe el tipo de movimiento Entrada.', 1;

    /*
       PrecioCompra ya incluye IGV.
       Total = cantidad * precioCompra.
       Subtotal = total / 1.18.
       IGV = total - subtotal.
    */
    SELECT @Total = SUM(Cantidad * PrecioCompra)
    FROM @Detalles;

    SET @Total = ROUND(ISNULL(@Total, 0), 2);
    SET @Subtotal = ROUND(@Total / 1.18, 2);
    SET @Igv = ROUND(@Total - @Subtotal, 2);

    IF @MontoPagado IS NULL
        SET @MontoPagado = 0;

    SET @MontoPagado = ROUND(@MontoPagado, 2);

    IF @MontoPagado < 0
        THROW 62019, 'El monto pagado no puede ser negativo.', 1;

    IF @MontoPagado > @Total
        THROW 62020, 'El monto pagado no puede ser mayor al total.', 1;

    IF @TipoPago = 'Total' AND @MontoPagado <> @Total
        THROW 62021, 'Para pago total, el monto pagado debe ser igual al total.', 1;

    IF @TipoPago = 'Parcial' AND (@MontoPagado <= 0 OR @MontoPagado >= @Total)
        THROW 62022, 'Para pago parcial, el monto debe ser mayor a 0 y menor al total.', 1;

    SET @SaldoPendiente = ROUND(@Total - @MontoPagado, 2);

    IF @TipoPago = 'Cuotas'
    BEGIN
        IF @NumeroCuotas IS NULL OR @NumeroCuotas <= 0
            THROW 62023, 'Ingresa un número de cuotas válido.', 1;

        IF @NumeroCuotas > 24
            THROW 62026, 'El número máximo permitido es 24 cuotas.', 1;

        IF @FechaPrimerVencimiento IS NULL
            THROW 62024, 'Ingresa la fecha del primer vencimiento.', 1;

        IF @MontoPagado >= @Total
            THROW 62025, 'Para compra en cuotas debe quedar saldo pendiente.', 1;

        IF @CuotasJson IS NOT NULL
        BEGIN
            IF ISJSON(@CuotasJson) <> 1
                THROW 62027, 'El cronograma de cuotas no tiene formato JSON válido.', 1;

            INSERT INTO @CuotasProgramadas (
                NumeroCuota,
                FechaVencimiento
            )
            SELECT
                COALESCE(NumeroCuota, NumeroCuotaPascal),
                COALESCE(FechaVencimiento, FechaVencimientoPascal)
            FROM OPENJSON(@CuotasJson)
            WITH (
                NumeroCuota INT '$.numeroCuota',
                NumeroCuotaPascal INT '$.NumeroCuota',
                FechaVencimiento DATE '$.fechaVencimiento',
                FechaVencimientoPascal DATE '$.FechaVencimiento'
            );

            IF EXISTS (
                SELECT 1
                FROM @CuotasProgramadas
                WHERE NumeroCuota IS NULL
                   OR FechaVencimiento IS NULL
            )
            BEGIN
                THROW 62028, 'Todas las cuotas deben tener número y fecha de vencimiento.', 1;
            END;

            IF (SELECT COUNT(*) FROM @CuotasProgramadas) <> @NumeroCuotas
                THROW 62029, 'La cantidad de cuotas enviadas no coincide con el número de cuotas indicado.', 1;

            IF EXISTS (
                SELECT 1
                FROM @CuotasProgramadas
                WHERE NumeroCuota < 1
                   OR NumeroCuota > @NumeroCuotas
            )
            BEGIN
                THROW 62030, 'El número de cuota está fuera del rango permitido.', 1;
            END;

            IF (
                SELECT COUNT(DISTINCT NumeroCuota)
                FROM @CuotasProgramadas
            ) <> @NumeroCuotas
            BEGIN
                THROW 62031, 'Las cuotas no pueden estar repetidas y deben cubrir todo el cronograma.', 1;
            END;

            SET @UsaCuotasPersonalizadas = 1;
        END;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO Compra (
            IdProveedor,
            IdUsuarioRegistro,
            IdEstadoCompra,
            FechaCompra,
            Subtotal,
            Igv,
            Total,
            TipoComprobanteProveedor,
            SerieComprobante,
            NumeroComprobante,
            FechaEmisionComprobante,
            ObservacionCompra
        )
        VALUES (
            @IdProveedor,
            @IdUsuarioRegistro,
            @IdEstadoCompra,
            GETDATE(),
            @Subtotal,
            @Igv,
            @Total,
            @TipoComprobanteProveedor,
            @SerieComprobante,
            @NumeroComprobante,
            @FechaEmisionComprobante,
            @ObservacionCompra
        );

        SET @IdCompra = SCOPE_IDENTITY();

        INSERT INTO DetalleCompra (
            IdCompra,
            IdProducto,
            Cantidad,
            PrecioCompra,
            Subtotal
        )
        SELECT
            @IdCompra,
            IdProducto,
            Cantidad,
            PrecioCompra,
            ROUND(Cantidad * PrecioCompra, 2)
        FROM @Detalles;

        MERGE Inventario AS target
        USING (
            SELECT IdProducto, SUM(Cantidad) AS Cantidad
            FROM @Detalles
            GROUP BY IdProducto
        ) AS source
        ON target.IdProducto = source.IdProducto
        WHEN MATCHED THEN
            UPDATE SET
                StockActual = target.StockActual + source.Cantidad,
                FechaActualizacion = GETDATE()
        WHEN NOT MATCHED THEN
            INSERT (
                IdProducto,
                StockActual,
                StockMinimo,
                FechaActualizacion
            )
            VALUES (
                source.IdProducto,
                source.Cantidad,
                0,
                GETDATE()
            );

        INSERT INTO MovimientoStock (
            IdProducto,
            IdUsuarioRegistro,
            IdTipoMovimientoStock,
            IdVenta,
            IdCompra,
            Cantidad,
            FechaMovimiento,
            Motivo
        )
        SELECT
            IdProducto,
            @IdUsuarioRegistro,
            @IdTipoMovimientoEntrada,
            NULL,
            @IdCompra,
            Cantidad,
            GETDATE(),
            CONCAT('Entrada por compra ', @SerieComprobante, '-', @NumeroComprobante)
        FROM @Detalles;

        IF @TieneGuia = 1
        BEGIN
            INSERT INTO GuiaRemisionCompra (
                IdCompra,
                NumeroGuia,
                FechaEmision,
                FechaTraslado,
                PuntoPartida,
                PuntoLlegada,
                Transportista,
                RucTransportista,
                PlacaVehiculo,
                Observacion
            )
            VALUES (
                @IdCompra,
                @NumeroGuia,
                @FechaEmisionGuia,
                @FechaTrasladoGuia,
                @PuntoPartida,
                @PuntoLlegada,
                @Transportista,
                @RucTransportista,
                @PlacaVehiculo,
                @ObservacionGuia
            );
        END;

        IF @MontoPagado > 0
        BEGIN
            INSERT INTO PagoCompra (
                IdCompra,
                IdUsuarioRegistro,
                MetodoPago,
                MontoPagado,
                FechaPago,
                Observacion
            )
            VALUES (
                @IdCompra,
                @IdUsuarioRegistro,
                @MetodoPago,
                @MontoPagado,
                GETDATE(),
                @ObservacionPago
            );
        END;

        IF @TipoPago = 'Cuotas'
        BEGIN
            DECLARE @Contador INT = 1;
            DECLARE @MontoBaseCuota DECIMAL(18,2);
            DECLARE @MontoUltimaCuota DECIMAL(18,2);
            DECLARE @Acumulado DECIMAL(18,2) = 0;
            DECLARE @FechaVencimientoCuota DATE;

            SET @MontoBaseCuota = ROUND(@SaldoPendiente / @NumeroCuotas, 2);

            WHILE @Contador <= @NumeroCuotas
            BEGIN
                IF @UsaCuotasPersonalizadas = 1
                BEGIN
                    SELECT @FechaVencimientoCuota = FechaVencimiento
                    FROM @CuotasProgramadas
                    WHERE NumeroCuota = @Contador;
                END
                ELSE
                BEGIN
                    SET @FechaVencimientoCuota = DATEADD(MONTH, @Contador - 1, @FechaPrimerVencimiento);
                END;

                IF @Contador < @NumeroCuotas
                BEGIN
                    INSERT INTO CuotaCompra (
                        IdCompra,
                        NumeroCuota,
                        FechaVencimiento,
                        MontoCuota,
                        MontoPagado,
                        EstadoCuota
                    )
                    VALUES (
                        @IdCompra,
                        @Contador,
                        @FechaVencimientoCuota,
                        @MontoBaseCuota,
                        0,
                        'Pendiente'
                    );

                    SET @Acumulado = @Acumulado + @MontoBaseCuota;
                END
                ELSE
                BEGIN
                    SET @MontoUltimaCuota = ROUND(@SaldoPendiente - @Acumulado, 2);

                    INSERT INTO CuotaCompra (
                        IdCompra,
                        NumeroCuota,
                        FechaVencimiento,
                        MontoCuota,
                        MontoPagado,
                        EstadoCuota
                    )
                    VALUES (
                        @IdCompra,
                        @Contador,
                        @FechaVencimientoCuota,
                        @MontoUltimaCuota,
                        0,
                        'Pendiente'
                    );
                END;

                SET @Contador = @Contador + 1;
            END;
        END;

        COMMIT TRANSACTION;

        SET @EstadoPago =
            CASE
                WHEN @SaldoPendiente <= 0 THEN 'Pagada'
                WHEN @TipoPago = 'Cuotas' THEN 'En cuotas'
                WHEN @MontoPagado > 0 THEN 'Parcial'
                ELSE 'Pendiente'
            END;

        SELECT
            @IdCompra AS IdCompra,
            @Subtotal AS Subtotal,
            @Igv AS Igv,
            @Total AS Total,
            @MontoPagado AS TotalPagado,
            @SaldoPendiente AS SaldoPendiente,
            @EstadoPago AS EstadoPago,
            CONCAT('Compra registrada correctamente. Documento: ', @SerieComprobante, '-', @NumeroComprobante) AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
    COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

GO
/* =========================================================
   5. REGISTRAR COTIZACIÓN
   ========================================================= */

CREATE   PROCEDURE dbo.sp_RegistrarCotizacion
    @IdCliente INT,
    @IdUsuarioRegistro INT = NULL,
    @OrigenCotizacion NVARCHAR(60) = 'Manual',
    @Descuento DECIMAL(18,2) = 0,
    @Observacion NVARCHAR(1000) = NULL,
    @Detalles dbo.CotizacionDetalleType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdCotizacion INT;
    DECLARE @IdEstadoPendiente INT;
    DECLARE @Subtotal DECIMAL(18,2);
    DECLARE @Total DECIMAL(18,2);
    DECLARE @Igv DECIMAL(18,2);

    SET @OrigenCotizacion = NULLIF(LTRIM(RTRIM(ISNULL(@OrigenCotizacion, ''))), '');
    SET @Observacion = NULLIF(LTRIM(RTRIM(ISNULL(@Observacion, ''))), '');
    SET @Descuento = ROUND(ISNULL(@Descuento, 0), 2);

    IF @IdCliente <= 0
        THROW 61001, 'Selecciona un cliente válido.', 1;

    IF NOT EXISTS (SELECT 1 FROM Cliente WHERE IdCliente = @IdCliente AND Estado = 1)
        THROW 61002, 'El cliente seleccionado no existe o está inactivo.', 1;

    IF @IdUsuarioRegistro IS NOT NULL
       AND @IdUsuarioRegistro <= 0
        THROW 61003, 'El usuario que registra la cotización no es válido.', 1;

    IF @IdUsuarioRegistro IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM Usuario WHERE IdUsuario = @IdUsuarioRegistro)
        THROW 61004, 'El usuario que registra la cotización no existe.', 1;

    IF @OrigenCotizacion IS NULL
        SET @OrigenCotizacion = 'Manual';

    IF @OrigenCotizacion NOT IN ('Manual', 'Web', 'WhatsApp', 'Correo')
        THROW 61005, 'Origen de cotización no válido.', 1;

    IF NOT EXISTS (SELECT 1 FROM @Detalles)
        THROW 61006, 'Agrega al menos un producto o servicio a la cotización.', 1;

    IF EXISTS (SELECT 1 FROM @Detalles WHERE Cantidad <= 0)
        THROW 61007, 'La cantidad de cada detalle debe ser mayor a 0.', 1;

    IF EXISTS (SELECT 1 FROM @Detalles WHERE PrecioUnitario <= 0)
        THROW 61008, 'El precio unitario de cada detalle debe ser mayor a 0.', 1;

    IF EXISTS (
        SELECT 1
        FROM @Detalles d
        LEFT JOIN ElementoCatalogo e ON e.IdElementoCatalogo = d.IdElementoCatalogo
        WHERE e.IdElementoCatalogo IS NULL
           OR e.Estado = 0
    )
    BEGIN
        THROW 61009, 'Uno o más productos o servicios no existen o están inactivos.', 1;
    END;

    SELECT @IdEstadoPendiente = IdEstadoCotizacion
    FROM EstadoCotizacion
    WHERE Nombre = 'Pendiente'
      AND Estado = 1;

    IF @IdEstadoPendiente IS NULL
        THROW 61010, 'No existe el estado Pendiente para cotizaciones.', 1;

    SELECT @Subtotal = ROUND(SUM(Cantidad * PrecioUnitario), 2)
    FROM @Detalles;

    SET @Subtotal = ROUND(ISNULL(@Subtotal, 0), 2);

    IF @Descuento < 0
        THROW 61011, 'El descuento no puede ser negativo.', 1;

    IF @Descuento > @Subtotal
        THROW 61012, 'El descuento no puede superar el subtotal.', 1;

    SET @Total = ROUND(@Subtotal - @Descuento, 2);
    SET @Igv = ROUND(@Total - (@Total / 1.18), 2);

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO Cotizacion (
            IdCliente,
            IdUsuarioRegistro,
            IdUsuarioAtencion,
            IdEstadoCotizacion,
            FechaCotizacion,
            TotalReferencial,
            Observacion,
            ArchivoPdf,
            CorreoEnviado,
            OrigenCotizacion,
            Subtotal,
            Descuento,
            Igv,
            Total,
            WhatsappEnviado,
            FechaRespuesta,
            CanalRespuesta,
            FechaActualizacion,
            IdVentaGenerada
        )
        VALUES (
            @IdCliente,
            @IdUsuarioRegistro,
            NULL,
            @IdEstadoPendiente,
            GETDATE(),
            @Total,
            @Observacion,
            NULL,
            0,
            @OrigenCotizacion,
            @Subtotal,
            @Descuento,
            @Igv,
            @Total,
            0,
            NULL,
            NULL,
            GETDATE(),
            NULL
        );

        SET @IdCotizacion = SCOPE_IDENTITY();

        INSERT INTO DetalleCotizacion (
            IdCotizacion,
            IdElementoCatalogo,
            Cantidad,
            PrecioUnitario,
            Subtotal,
            Observacion
        )
        SELECT
            @IdCotizacion,
            IdElementoCatalogo,
            Cantidad,
            PrecioUnitario,
            ROUND(Cantidad * PrecioUnitario, 2),
            NULLIF(LTRIM(RTRIM(ISNULL(Observacion, ''))), '')
        FROM @Detalles;

        COMMIT TRANSACTION;

        SELECT
            @IdCotizacion AS IdCotizacion,
            CONCAT('COT-', RIGHT(REPLICATE('0', 5) + CAST(@IdCotizacion AS NVARCHAR(20)), 5)) AS CodigoCotizacion,
            @Subtotal AS Subtotal,
            @Descuento AS Descuento,
            @Igv AS Igv,
            @Total AS Total,
            'Pendiente' AS EstadoCotizacion,
            'Cotización registrada correctamente.' AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;

GO
CREATE   PROCEDURE dbo.sp_RegistrarCotizacionWeb
    @IdCliente INT,
    @IdUsuarioRegistro INT = NULL,
    @Observacion NVARCHAR(1000) = NULL,
    @Detalles dbo.CotizacionWebDetalleType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdCotizacion INT;
    DECLARE @IdEstadoPendiente INT;
    DECLARE @Subtotal DECIMAL(18,2);
    DECLARE @Descuento DECIMAL(18,2) = 0;
    DECLARE @Total DECIMAL(18,2);
    DECLARE @Igv DECIMAL(18,2);
    DECLARE @EsEmpresa BIT = 0;
    DECLARE @Mensaje NVARCHAR(600);

    SET @Observacion = NULLIF(LTRIM(RTRIM(ISNULL(@Observacion, ''))), '');

    IF @IdCliente <= 0
        THROW 68001, 'No se pudo identificar al cliente web.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM Cliente
        WHERE IdCliente = @IdCliente
          AND Estado = 1
          AND IdUsuario IS NOT NULL
    )
    BEGIN
        THROW 68002, 'La cuenta cliente no está activa o no está vinculada correctamente.', 1;
    END;

    IF @IdUsuarioRegistro IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM Usuario WHERE IdUsuario = @IdUsuarioRegistro AND Estado = 1)
        THROW 68003, 'El usuario cliente no existe o se encuentra inactivo.', 1;

    IF NOT EXISTS (SELECT 1 FROM @Detalles)
        THROW 68004, 'Agrega al menos un producto al carrito de cotización.', 1;

    IF EXISTS (SELECT 1 FROM @Detalles WHERE IdProducto <= 0)
        THROW 68005, 'Uno o más productos del carrito no son válidos.', 1;

    IF EXISTS (SELECT 1 FROM @Detalles WHERE Cantidad <= 0)
        THROW 68006, 'La cantidad de cada producto debe ser mayor a 0.', 1;

    IF EXISTS (
        SELECT 1
        FROM @Detalles d
        LEFT JOIN Producto p ON p.IdProducto = d.IdProducto
        LEFT JOIN ElementoCatalogo e ON e.IdElementoCatalogo = p.IdElementoCatalogo
        WHERE p.IdProducto IS NULL
           OR e.IdElementoCatalogo IS NULL
           OR e.Estado = 0
    )
    BEGIN
        THROW 68007, 'Uno o más productos ya no están disponibles para cotización.', 1;
    END;

    SELECT @IdEstadoPendiente = IdEstadoCotizacion
    FROM EstadoCotizacion
    WHERE Nombre = 'Pendiente'
      AND Estado = 1;

    IF @IdEstadoPendiente IS NULL
        THROW 68008, 'No existe el estado Pendiente para cotizaciones.', 1;

    SELECT @EsEmpresa =
        CASE
            WHEN EXISTS (
                SELECT 1
                FROM Cliente c
                INNER JOIN TipoDocumento td ON td.IdTipoDocumento = c.IdTipoDocumento
                WHERE c.IdCliente = @IdCliente
                  AND UPPER(td.Nombre) = 'RUC'
            )
            THEN 1
            ELSE 0
        END;

    DECLARE @DetalleFinal TABLE
    (
        IdElementoCatalogo INT NOT NULL,
        Cantidad INT NOT NULL,
        PrecioUnitario DECIMAL(18,2) NOT NULL,
        Subtotal DECIMAL(18,2) NOT NULL,
        Observacion NVARCHAR(1000) NULL
    );

    INSERT INTO @DetalleFinal (
        IdElementoCatalogo,
        Cantidad,
        PrecioUnitario,
        Subtotal,
        Observacion
    )
    SELECT
        p.IdElementoCatalogo,
        SUM(d.Cantidad) AS Cantidad,
        CAST(ISNULL(NULLIF(e.PrecioReferencial, 0), 0) AS DECIMAL(18,2)) AS PrecioUnitario,
        ROUND(SUM(d.Cantidad) * CAST(ISNULL(NULLIF(e.PrecioReferencial, 0), 0) AS DECIMAL(18,2)), 2) AS Subtotal,
        NULLIF(LTRIM(RTRIM(MAX(ISNULL(d.Observacion, '')))), '') AS Observacion
    FROM @Detalles d
    INNER JOIN Producto p ON p.IdProducto = d.IdProducto
    INNER JOIN ElementoCatalogo e ON e.IdElementoCatalogo = p.IdElementoCatalogo
    WHERE e.Estado = 1
    GROUP BY
        p.IdElementoCatalogo,
        CAST(ISNULL(NULLIF(e.PrecioReferencial, 0), 0) AS DECIMAL(18,2));

    SELECT @Subtotal = ROUND(SUM(Subtotal), 2)
    FROM @DetalleFinal;

    SET @Subtotal = ISNULL(@Subtotal, 0);
    SET @Total = ROUND(@Subtotal - @Descuento, 2);
    SET @Igv = ROUND(@Total - (@Total / 1.18), 2);

    SET @Mensaje =
        CASE
            WHEN @EsEmpresa = 1 THEN
                'Tu solicitud empresarial fue enviada correctamente. Nuestro equipo comercial revisará el requerimiento de tu empresa y responderá por el correo empresarial o canal registrado.'
            ELSE
                'Tu solicitud fue enviada correctamente. Nuestro equipo encargado revisará los productos solicitados y se comunicará contigo por WhatsApp para brindarte la atención correspondiente.'
        END;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO Cotizacion (
            IdCliente,
            IdUsuarioRegistro,
            IdUsuarioAtencion,
            IdEstadoCotizacion,
            FechaCotizacion,
            TotalReferencial,
            Observacion,
            ArchivoPdf,
            CorreoEnviado,
            OrigenCotizacion,
            Subtotal,
            Descuento,
            Igv,
            Total,
            WhatsappEnviado,
            FechaRespuesta,
            CanalRespuesta,
            FechaActualizacion,
            IdVentaGenerada
        )
        VALUES (
            @IdCliente,
            @IdUsuarioRegistro,
            NULL,
            @IdEstadoPendiente,
            GETDATE(),
            @Total,
            @Observacion,
            NULL,
            0,
            'Web',
            @Subtotal,
            @Descuento,
            @Igv,
            @Total,
            0,
            NULL,
            NULL,
            GETDATE(),
            NULL
        );

        SET @IdCotizacion = SCOPE_IDENTITY();

        INSERT INTO DetalleCotizacion (
            IdCotizacion,
            IdElementoCatalogo,
            Cantidad,
            PrecioUnitario,
            Subtotal,
            Observacion
        )
        SELECT
            @IdCotizacion,
            IdElementoCatalogo,
            Cantidad,
            PrecioUnitario,
            Subtotal,
            Observacion
        FROM @DetalleFinal;

        COMMIT TRANSACTION;

        SELECT
            @IdCotizacion AS IdCotizacion,
            CONCAT('COT-', RIGHT(REPLICATE('0', 5) + CAST(@IdCotizacion AS NVARCHAR(20)), 5)) AS CodigoCotizacion,
            GETDATE() AS FechaCotizacion,
            'Pendiente' AS EstadoCotizacion,
            'Web' AS OrigenCotizacion,
            @Subtotal AS Subtotal,
            @Descuento AS Descuento,
            @Igv AS Igv,
            @Total AS Total,
            @EsEmpresa AS EsEmpresa,
            @Mensaje AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;

GO
/* =========================================================
   9. PROCEDIMIENTO INTERNO: REGISTRAR MOVIMIENTO AUTOMÁTICO
   Este procedimiento lo usarán ventas y compras.
   No exige administrador porque es automático.
   ========================================================= */

CREATE   PROCEDURE dbo.sp_RegistrarMovimientoCajaAutomatico
    @OrigenMovimiento NVARCHAR(100),
    @IdUsuarioRegistro INT,
    @Monto DECIMAL(18,2),
    @MetodoPago NVARCHAR(100),
    @IdVenta INT = NULL,
    @IdCompra INT = NULL,
    @IdPagoVenta INT = NULL,
    @IdPagoCompra INT = NULL,
    @Descripcion NVARCHAR(600) = NULL
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
    DECLARE @TipoMovimientoNombre NVARCHAR(100);

    SET @OrigenMovimiento = LTRIM(RTRIM(ISNULL(@OrigenMovimiento, '')));
    SET @MetodoPago = NULLIF(LTRIM(RTRIM(ISNULL(@MetodoPago, ''))), '');
    SET @Descripcion = NULLIF(LTRIM(RTRIM(ISNULL(@Descripcion, ''))), '');
    SET @Monto = ROUND(ISNULL(@Monto, 0), 2);

    IF @IdUsuarioRegistro <= 0
       OR NOT EXISTS (SELECT 1 FROM dbo.Usuario WHERE IdUsuario = @IdUsuarioRegistro AND Estado = 1)
    BEGIN
        THROW 70201, 'El usuario que registra el movimiento de caja no es válido.', 1;
    END;

    IF @Monto <= 0
        THROW 70202, 'El monto del movimiento de caja debe ser mayor a 0.', 1;

    IF @OrigenMovimiento NOT IN ('Venta', 'Compra')
        THROW 70203, 'El origen automático de caja no es válido.', 1;

    IF @OrigenMovimiento = 'Venta'
    BEGIN
        SET @TipoMovimientoNombre = 'Ingreso por venta';

        IF @IdVenta IS NULL OR @IdPagoVenta IS NULL
            THROW 70204, 'Para registrar ingreso por venta se requiere IdVenta e IdPagoVenta.', 1;

        IF EXISTS (
            SELECT 1
            FROM dbo.MovimientoCaja
            WHERE IdPagoVenta = @IdPagoVenta
              AND Estado = 1
        )
        BEGIN
            COMMIT TRANSACTION; RETURN;
        END;
    END;

    IF @OrigenMovimiento = 'Compra'
    BEGIN
        SET @TipoMovimientoNombre = 'Egreso por compra';

        IF @IdCompra IS NULL OR @IdPagoCompra IS NULL
            THROW 70205, 'Para registrar egreso por compra se requiere IdCompra e IdPagoCompra.', 1;

        IF EXISTS (
            SELECT 1
            FROM dbo.MovimientoCaja
            WHERE IdPagoCompra = @IdPagoCompra
              AND Estado = 1
        )
        BEGIN
            COMMIT TRANSACTION; RETURN;
        END;
    END;

    SELECT @IdCaja = c.IdCaja
    FROM dbo.Caja c
    INNER JOIN dbo.EstadoCaja ec ON ec.IdEstadoCaja = c.IdEstadoCaja
    WHERE ec.Nombre = 'Abierta'
      AND ec.Estado = 1;

    IF @IdCaja IS NULL
        THROW 70206, 'No existe una caja abierta. El administrador debe abrir caja antes de registrar cobros o pagos.', 1;

    SELECT @IdTipoMovimientoCaja = IdTipoMovimientoCaja
    FROM dbo.TipoMovimientoCaja
    WHERE Nombre = @TipoMovimientoNombre
      AND Estado = 1;

    IF @IdTipoMovimientoCaja IS NULL
        THROW 70207, 'No existe el tipo de movimiento de caja requerido.', 1;

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
        @IdVenta,
        @IdCompra,
        @IdUsuarioRegistro,
        @Monto,
        @Descripcion,
        GETDATE(),
        @IdPagoVenta,
        @IdPagoCompra,
        @MetodoPago,
        @OrigenMovimiento,
        1,
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
    COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

GO
/* =========================================================
   10. PROCEDIMIENTO: REGISTRAR MOVIMIENTO MANUAL
   ========================================================= */

CREATE   PROCEDURE dbo.sp_RegistrarMovimientoCajaManual
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
CREATE   PROCEDURE sp_RegistrarPagoCompra
    @IdCompra INT,
    @IdUsuarioRegistro INT,
    @MetodoPago NVARCHAR(50),
    @MontoPagado DECIMAL(18,2),
    @Observacion NVARCHAR(1000) = NULL,
    @CuotasPagadasJson NVARCHAR(MAX) = NULL
AS
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @BloqueoCaja INT;
    EXEC @BloqueoCaja=sys.sp_getapplock @Resource=N'Sistema3S:operaciones-caja', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
    IF @BloqueoCaja<0 THROW 72002, 'Caja está ocupada. Intenta nuevamente.', 1;
    SET NOCOUNT ON;

    DECLARE @Total DECIMAL(18,2);
    DECLARE @PagadoActual DECIMAL(18,2);
    DECLARE @Saldo DECIMAL(18,2);
    DECLARE @EstadoCompra NVARCHAR(50);
    DECLARE @TieneCuotas BIT = 0;
    DECLARE @IdPagoCompra INT;
    DECLARE @MontoEsperadoCuotas DECIMAL(18,2) = 0;

    DECLARE @CuotasSeleccionadas TABLE (
        IdCuotaCompra INT NOT NULL PRIMARY KEY
    );

    SET @MetodoPago = LTRIM(RTRIM(ISNULL(@MetodoPago, '')));
    SET @CuotasPagadasJson = NULLIF(LTRIM(RTRIM(ISNULL(@CuotasPagadasJson, ''))), '');
    SET @MontoPagado = ROUND(ISNULL(@MontoPagado, 0), 2);

    SELECT
        @Total = c.Total,
        @EstadoCompra = ec.Nombre
    FROM Compra c
    INNER JOIN EstadoCompra ec ON ec.IdEstadoCompra = c.IdEstadoCompra
    WHERE c.IdCompra = @IdCompra;

    IF @Total IS NULL
        THROW 63001, 'La compra seleccionada no existe.', 1;

    IF @EstadoCompra = 'Anulada'
        THROW 63002, 'No se puede registrar pago a una compra anulada.', 1;

    IF @IdUsuarioRegistro <= 0 OR NOT EXISTS (SELECT 1 FROM Usuario WHERE IdUsuario = @IdUsuarioRegistro)
        THROW 63003, 'El usuario que registra el pago no es válido.', 1;

    IF @MetodoPago = ''
        THROW 63004, 'Selecciona el método de pago.', 1;

    IF @MontoPagado <= 0
        THROW 63005, 'El monto pagado debe ser mayor a 0.', 1;

    SELECT @PagadoActual = ISNULL(SUM(MontoPagado), 0)
    FROM PagoCompra
    WHERE IdCompra = @IdCompra;

    SET @PagadoActual = ROUND(ISNULL(@PagadoActual, 0), 2);
    SET @Saldo = ROUND(@Total - @PagadoActual, 2);

    IF @Saldo <= 0
        THROW 63006, 'La compra ya se encuentra pagada.', 1;

    IF @MontoPagado > @Saldo
        THROW 63007, 'El monto no puede superar el saldo pendiente.', 1;

    IF EXISTS (SELECT 1 FROM CuotaCompra WHERE IdCompra = @IdCompra)
        SET @TieneCuotas = 1;

    IF @TieneCuotas = 1
    BEGIN
        IF @CuotasPagadasJson IS NULL
            THROW 63008, 'Selecciona las cuotas que se van a pagar.', 1;

        IF ISJSON(@CuotasPagadasJson) <> 1
            THROW 63009, 'La lista de cuotas seleccionadas no tiene formato válido.', 1;

        INSERT INTO @CuotasSeleccionadas (IdCuotaCompra)
        SELECT DISTINCT
            COALESCE(
                TRY_CONVERT(INT, j.[value]),
                TRY_CONVERT(INT, JSON_VALUE(j.[value], '$.idCuotaCompra')),
                TRY_CONVERT(INT, JSON_VALUE(j.[value], '$.IdCuotaCompra'))
            ) AS IdCuotaCompra
        FROM OPENJSON(@CuotasPagadasJson) j
        WHERE COALESCE(
                TRY_CONVERT(INT, j.[value]),
                TRY_CONVERT(INT, JSON_VALUE(j.[value], '$.idCuotaCompra')),
                TRY_CONVERT(INT, JSON_VALUE(j.[value], '$.IdCuotaCompra'))
              ) IS NOT NULL;

        IF NOT EXISTS (SELECT 1 FROM @CuotasSeleccionadas)
            THROW 63010, 'Selecciona al menos una cuota pendiente.', 1;

        IF EXISTS (
            SELECT 1
            FROM @CuotasSeleccionadas s
            LEFT JOIN CuotaCompra cc ON cc.IdCuotaCompra = s.IdCuotaCompra
            WHERE cc.IdCuotaCompra IS NULL
               OR cc.IdCompra <> @IdCompra
        )
        BEGIN
            THROW 63011, 'Una o más cuotas seleccionadas no pertenecen a la compra.', 1;
        END;

        IF EXISTS (
            SELECT 1
            FROM CuotaCompra cc
            INNER JOIN @CuotasSeleccionadas s ON s.IdCuotaCompra = cc.IdCuotaCompra
            WHERE cc.EstadoCuota = 'Pagada'
               OR cc.MontoPagado >= cc.MontoCuota
        )
        BEGIN
            THROW 63012, 'Una o más cuotas seleccionadas ya están pagadas.', 1;
        END;

        SELECT @MontoEsperadoCuotas = ROUND(SUM(cc.MontoCuota - cc.MontoPagado), 2)
        FROM CuotaCompra cc
        INNER JOIN @CuotasSeleccionadas s ON s.IdCuotaCompra = cc.IdCuotaCompra;

        SET @MontoEsperadoCuotas = ROUND(ISNULL(@MontoEsperadoCuotas, 0), 2);

        IF @MontoPagado <> @MontoEsperadoCuotas
            THROW 63013, 'El monto pagado debe ser igual a la suma de las cuotas seleccionadas.', 1;
    END
    ELSE
    BEGIN
        IF @CuotasPagadasJson IS NOT NULL
            THROW 63014, 'Esta compra no tiene cuotas para seleccionar.', 1;

        IF @MontoPagado <> @Saldo
            THROW 63015, 'Para una compra parcial, debes cancelar el saldo pendiente completo.', 1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO PagoCompra (
            IdCompra,
            IdUsuarioRegistro,
            MetodoPago,
            MontoPagado,
            FechaPago,
            Observacion
        )
        VALUES (
            @IdCompra,
            @IdUsuarioRegistro,
            @MetodoPago,
            @MontoPagado,
            GETDATE(),
            @Observacion
        );

        SET @IdPagoCompra = SCOPE_IDENTITY();

        IF @TieneCuotas = 1
        BEGIN
            INSERT INTO PagoCompraCuota (
                IdPagoCompra,
                IdCuotaCompra,
                MontoAplicado
            )
            SELECT
                @IdPagoCompra,
                cc.IdCuotaCompra,
                ROUND(cc.MontoCuota - cc.MontoPagado, 2)
            FROM CuotaCompra cc
            INNER JOIN @CuotasSeleccionadas s ON s.IdCuotaCompra = cc.IdCuotaCompra;

            UPDATE cc
            SET
                cc.MontoPagado = cc.MontoCuota,
                cc.EstadoCuota = 'Pagada'
            FROM CuotaCompra cc
            INNER JOIN @CuotasSeleccionadas s ON s.IdCuotaCompra = cc.IdCuotaCompra;
        END;

        COMMIT TRANSACTION;

        SELECT @PagadoActual = ISNULL(SUM(MontoPagado), 0)
        FROM PagoCompra
        WHERE IdCompra = @IdCompra;

        SET @PagadoActual = ROUND(ISNULL(@PagadoActual, 0), 2);
        SET @Saldo = ROUND(@Total - @PagadoActual, 2);

        SELECT
            @IdCompra AS IdCompra,
            0 AS Subtotal,
            0 AS Igv,
            @Total AS Total,
            @PagadoActual AS TotalPagado,
            @Saldo AS SaldoPendiente,
            CASE
                WHEN @Saldo <= 0 THEN 'Pagada'
                WHEN EXISTS (SELECT 1 FROM CuotaCompra WHERE IdCompra = @IdCompra) THEN 'En cuotas'
                ELSE 'Parcial'
            END AS EstadoPago,
            'Pago registrado correctamente.' AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
    COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

GO
CREATE   PROCEDURE sp_RegistrarPagoVenta
    @IdVenta INT,
    @IdUsuarioRegistro INT,
    @MetodoPago NVARCHAR(50),
    @MontoPagado DECIMAL(18,2),
    @Observacion NVARCHAR(600) = NULL,
    @CuotasCobradasJson NVARCHAR(MAX) = NULL
AS
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @BloqueoCaja INT;
    EXEC @BloqueoCaja=sys.sp_getapplock @Resource=N'Sistema3S:operaciones-caja', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
    IF @BloqueoCaja<0 THROW 72002, 'Caja está ocupada. Intenta nuevamente.', 1;
    SET NOCOUNT ON;

    DECLARE @Total DECIMAL(18,2);
    DECLARE @PagadoActual DECIMAL(18,2);
    DECLARE @Saldo DECIMAL(18,2);
    DECLARE @EstadoVenta NVARCHAR(50);
    DECLARE @TieneCuotas BIT = 0;
    DECLARE @IdPagoVenta INT;
    DECLARE @MontoEsperadoCuotas DECIMAL(18,2) = 0;

    DECLARE @CuotasSeleccionadas TABLE (
        IdCuotaVenta INT NOT NULL PRIMARY KEY
    );

    SET @MetodoPago = LTRIM(RTRIM(ISNULL(@MetodoPago, '')));
    SET @CuotasCobradasJson = NULLIF(LTRIM(RTRIM(ISNULL(@CuotasCobradasJson, ''))), '');
    SET @MontoPagado = ROUND(ISNULL(@MontoPagado, 0), 2);

    IF @IdVenta <= 0
        THROW 52001, 'Selecciona una venta válida.', 1;

    IF @IdUsuarioRegistro <= 0 OR NOT EXISTS (SELECT 1 FROM Usuario WHERE IdUsuario = @IdUsuarioRegistro)
        THROW 52002, 'El usuario que registra el cobro no es válido.', 1;

    IF @MetodoPago = ''
        THROW 52003, 'Selecciona el método de cobro.', 1;

    IF @MontoPagado <= 0
        THROW 52004, 'El monto cobrado debe ser mayor a 0.', 1;

    SELECT
        @Total = v.Total,
        @EstadoVenta = ev.Nombre
    FROM Venta v
    INNER JOIN EstadoVenta ev ON ev.IdEstadoVenta = v.IdEstadoVenta
    WHERE v.IdVenta = @IdVenta;

    IF @Total IS NULL
        THROW 52005, 'La venta seleccionada no existe.', 1;

    IF @EstadoVenta = 'Anulada'
        THROW 52006, 'No se puede registrar cobro para una venta anulada.', 1;

    SELECT @PagadoActual = ISNULL(SUM(MontoPagado), 0)
    FROM PagoVenta
    WHERE IdVenta = @IdVenta;

    SET @PagadoActual = ROUND(ISNULL(@PagadoActual, 0), 2);
    SET @Saldo = ROUND(@Total - @PagadoActual, 2);

    IF @Saldo <= 0
        THROW 52007, 'La venta ya se encuentra cobrada.', 1;

    IF @MontoPagado > @Saldo
        THROW 52008, 'El monto no puede superar el saldo pendiente.', 1;

    IF EXISTS (SELECT 1 FROM CuotaVenta WHERE IdVenta = @IdVenta)
        SET @TieneCuotas = 1;

    IF @TieneCuotas = 1
    BEGIN
        IF @CuotasCobradasJson IS NULL
            THROW 52009, 'Selecciona las cuotas que se van a cobrar.', 1;

        IF ISJSON(@CuotasCobradasJson) <> 1
            THROW 52010, 'La lista de cuotas seleccionadas no tiene formato válido.', 1;

        INSERT INTO @CuotasSeleccionadas (IdCuotaVenta)
        SELECT DISTINCT
            COALESCE(
                TRY_CONVERT(INT, j.[value]),
                TRY_CONVERT(INT, JSON_VALUE(j.[value], '$.idCuotaVenta')),
                TRY_CONVERT(INT, JSON_VALUE(j.[value], '$.IdCuotaVenta'))
            ) AS IdCuotaVenta
        FROM OPENJSON(@CuotasCobradasJson) j
        WHERE COALESCE(
                TRY_CONVERT(INT, j.[value]),
                TRY_CONVERT(INT, JSON_VALUE(j.[value], '$.idCuotaVenta')),
                TRY_CONVERT(INT, JSON_VALUE(j.[value], '$.IdCuotaVenta'))
              ) IS NOT NULL;

        IF NOT EXISTS (SELECT 1 FROM @CuotasSeleccionadas)
            THROW 52011, 'Selecciona al menos una cuota pendiente.', 1;

        IF EXISTS (
            SELECT 1
            FROM @CuotasSeleccionadas s
            LEFT JOIN CuotaVenta cv ON cv.IdCuotaVenta = s.IdCuotaVenta
            WHERE cv.IdCuotaVenta IS NULL
               OR cv.IdVenta <> @IdVenta
        )
        BEGIN
            THROW 52012, 'Una o más cuotas seleccionadas no pertenecen a la venta.', 1;
        END;

        IF EXISTS (
            SELECT 1
            FROM CuotaVenta cv
            INNER JOIN @CuotasSeleccionadas s ON s.IdCuotaVenta = cv.IdCuotaVenta
            WHERE cv.EstadoCuota = 'Pagada'
               OR cv.MontoPagado >= cv.MontoCuota
        )
        BEGIN
            THROW 52013, 'Una o más cuotas seleccionadas ya están cobradas.', 1;
        END;

        SELECT @MontoEsperadoCuotas = ROUND(SUM(cv.MontoCuota - cv.MontoPagado), 2)
        FROM CuotaVenta cv
        INNER JOIN @CuotasSeleccionadas s ON s.IdCuotaVenta = cv.IdCuotaVenta;

        SET @MontoEsperadoCuotas = ROUND(ISNULL(@MontoEsperadoCuotas, 0), 2);

        IF @MontoPagado <> @MontoEsperadoCuotas
            THROW 52014, 'El monto cobrado debe ser igual a la suma de las cuotas seleccionadas.', 1;
    END
    ELSE
    BEGIN
        IF @CuotasCobradasJson IS NOT NULL
            THROW 52015, 'Esta venta no tiene cuotas para seleccionar.', 1;

        IF @MontoPagado <> @Saldo
            THROW 52016, 'Para una venta parcial o pendiente, debes cobrar el saldo pendiente completo.', 1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO PagoVenta (
            IdVenta,
            IdUsuarioRegistro,
            MetodoPago,
            MontoPagado,
            FechaPago,
            Observacion
        )
        VALUES (
            @IdVenta,
            @IdUsuarioRegistro,
            @MetodoPago,
            @MontoPagado,
            GETDATE(),
            @Observacion
        );

        SET @IdPagoVenta = SCOPE_IDENTITY();

        IF @TieneCuotas = 1
        BEGIN
            INSERT INTO PagoVentaCuota (
                IdPagoVenta,
                IdCuotaVenta,
                MontoAplicado
            )
            SELECT
                @IdPagoVenta,
                cv.IdCuotaVenta,
                ROUND(cv.MontoCuota - cv.MontoPagado, 2)
            FROM CuotaVenta cv
            INNER JOIN @CuotasSeleccionadas s ON s.IdCuotaVenta = cv.IdCuotaVenta;

            UPDATE cv
            SET
                cv.MontoPagado = cv.MontoCuota,
                cv.EstadoCuota = 'Pagada'
            FROM CuotaVenta cv
            INNER JOIN @CuotasSeleccionadas s ON s.IdCuotaVenta = cv.IdCuotaVenta;
        END;

        COMMIT TRANSACTION;

        SELECT
            @IdVenta AS IdVenta,
            v.Subtotal,
            v.Igv,
            v.Total,
            ISNULL(pagos.TotalPagado, 0) AS TotalPagado,
            ROUND(v.Total - ISNULL(pagos.TotalPagado, 0), 2) AS SaldoPendiente,
            CASE
                WHEN ROUND(v.Total - ISNULL(pagos.TotalPagado, 0), 2) <= 0 THEN 'Pagada'
                WHEN EXISTS (SELECT 1 FROM CuotaVenta cv WHERE cv.IdVenta = v.IdVenta) THEN 'En cuotas'
                WHEN ISNULL(pagos.TotalPagado, 0) > 0 THEN 'Parcial'
                ELSE 'Pendiente'
            END AS EstadoPago,
            'Cobro registrado correctamente.' AS Mensaje
        FROM Venta v
        OUTER APPLY (
            SELECT SUM(pv.MontoPagado) AS TotalPagado
            FROM PagoVenta pv
            WHERE pv.IdVenta = v.IdVenta
        ) pagos
        WHERE v.IdVenta = @IdVenta;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
    COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

GO
CREATE   PROCEDURE sp_RegistrarVentaCompleta
    @IdCliente INT,
    @IdCotizacion INT = NULL,
    @IdUsuarioRegistro INT,
    @TipoComprobante NVARCHAR(50),
    @Serie NVARCHAR(10) = NULL,
    @Numero NVARCHAR(20) = NULL,
    @FechaEmision DATETIME = NULL,
    @Observacion NVARCHAR(1000) = NULL,
    @TipoPago NVARCHAR(30),
    @MetodoPago NVARCHAR(50),
    @MontoPagado DECIMAL(18,2),
    @NumeroCuotas INT = NULL,
    @FechaPrimerVencimiento DATE = NULL,
    @Detalles VentaDetalleType READONLY,
    @CuotasJson NVARCHAR(MAX) = NULL
AS
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @BloqueoCaja INT;
    EXEC @BloqueoCaja=sys.sp_getapplock @Resource=N'Sistema3S:operaciones-caja', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
    IF @BloqueoCaja<0 THROW 72002, 'Caja está ocupada. Intenta nuevamente.', 1;
    SET NOCOUNT ON;

    DECLARE @IdVenta INT;
    DECLARE @IdEstadoVentaConfirmada INT;
    DECLARE @IdTipoComprobante INT;
    DECLARE @IdEstadoComprobanteEmitido INT;
    DECLARE @IdTipoMovimientoSalida INT;
    DECLARE @IdEstadoCotizacionConvertida INT;

    DECLARE @Subtotal DECIMAL(18,2);
    DECLARE @Igv DECIMAL(18,2);
    DECLARE @Total DECIMAL(18,2);
    DECLARE @SaldoPendiente DECIMAL(18,2);
    DECLARE @EstadoPago NVARCHAR(30);

    DECLARE @UltimoNumero INT;
    DECLARE @SiguienteNumero INT;

    DECLARE @CuotasProgramadas TABLE (
        NumeroCuota INT NOT NULL,
        FechaVencimiento DATE NOT NULL
    );

    IF @FechaEmision IS NULL
        SET @FechaEmision = GETDATE();

    SET @TipoComprobante = LTRIM(RTRIM(ISNULL(@TipoComprobante, '')));
    SET @Serie = UPPER(LTRIM(RTRIM(ISNULL(@Serie, ''))));
    SET @Numero = LTRIM(RTRIM(ISNULL(@Numero, '')));
    SET @TipoPago = LTRIM(RTRIM(ISNULL(@TipoPago, '')));
    SET @MetodoPago = LTRIM(RTRIM(ISNULL(@MetodoPago, '')));
    SET @CuotasJson = NULLIF(LTRIM(RTRIM(ISNULL(@CuotasJson, ''))), '');

    IF @IdCliente <= 0
        THROW 51001, 'Selecciona un cliente válido.', 1;

    IF NOT EXISTS (SELECT 1 FROM Cliente WHERE IdCliente = @IdCliente AND Estado = 1)
        THROW 51002, 'El cliente seleccionado no existe o está inactivo.', 1;

    IF @IdUsuarioRegistro <= 0 OR NOT EXISTS (SELECT 1 FROM Usuario WHERE IdUsuario = @IdUsuarioRegistro)
        THROW 51003, 'El usuario que registra la venta no es válido.', 1;

    IF @TipoComprobante = ''
        THROW 51004, 'Selecciona el tipo de comprobante.', 1;

    IF @TipoComprobante NOT IN ('Factura', 'Boleta', 'Nota de venta')
        THROW 51005, 'Tipo de comprobante no válido.', 1;

    IF @TipoPago NOT IN ('Total', 'Parcial', 'Cuotas')
        THROW 51006, 'Selecciona un tipo de cobro válido.', 1;

    IF NOT EXISTS (SELECT 1 FROM @Detalles)
        THROW 51007, 'Agrega al menos un producto o servicio a la venta.', 1;

    IF EXISTS (SELECT 1 FROM @Detalles WHERE Cantidad <= 0)
        THROW 51008, 'La cantidad de cada detalle debe ser mayor a 0.', 1;

    IF EXISTS (SELECT 1 FROM @Detalles WHERE PrecioUnitario <= 0)
        THROW 51009, 'El precio de cada detalle debe ser mayor a 0.', 1;

    SELECT @IdTipoComprobante = IdTipoComprobante
    FROM TipoComprobante
    WHERE Nombre = @TipoComprobante AND Estado = 1;

    IF @IdTipoComprobante IS NULL
        THROW 51010, 'El tipo de comprobante no existe o está inactivo.', 1;

    IF @TipoComprobante = 'Factura'
       AND NOT EXISTS (SELECT 1 FROM ClienteEmpresa WHERE IdCliente = @IdCliente)
    BEGIN
        THROW 51011, 'Para emitir factura, el cliente debe ser empresa con RUC.', 1;
    END;

    IF @Serie = ''
    BEGIN
        IF @TipoComprobante = 'Factura'
            SET @Serie = 'F001';
        ELSE IF @TipoComprobante = 'Boleta'
            SET @Serie = 'B001';
        ELSE IF @TipoComprobante = 'Nota de venta'
            SET @Serie = 'NV01';
        ELSE
            THROW 51012, 'Tipo de comprobante no válido.', 1;
    END;

    IF @IdCotizacion IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Cotizacion WHERE IdCotizacion = @IdCotizacion)
            THROW 51013, 'La cotización seleccionada no existe.', 1;

        IF EXISTS (SELECT 1 FROM Venta WHERE IdCotizacion = @IdCotizacion)
            THROW 51014, 'La cotización ya fue convertida en venta.', 1;
    END;

    IF EXISTS (
        SELECT 1
        FROM @Detalles d
        LEFT JOIN ElementoCatalogo e ON e.IdElementoCatalogo = d.IdElementoCatalogo
        WHERE e.IdElementoCatalogo IS NULL OR e.Estado = 0
    )
    BEGIN
        THROW 51015, 'Uno o más elementos seleccionados no existen o están inactivos.', 1;
    END;

    IF EXISTS (
        SELECT 1
        FROM @Detalles d
        INNER JOIN Producto p ON p.IdElementoCatalogo = d.IdElementoCatalogo
        LEFT JOIN Inventario i ON i.IdProducto = p.IdProducto
        WHERE p.AplicaInventario = 1
          AND (i.IdInventario IS NULL OR i.StockActual < d.Cantidad)
    )
    BEGIN
        THROW 51016, 'No hay stock suficiente para uno o más productos.', 1;
    END;

    SELECT @IdEstadoVentaConfirmada = IdEstadoVenta
    FROM EstadoVenta
    WHERE Nombre = 'Confirmada';

    SELECT @IdEstadoComprobanteEmitido = IdEstadoComprobante
    FROM EstadoComprobante
    WHERE Nombre = 'Emitido';

    SELECT @IdTipoMovimientoSalida = IdTipoMovimientoStock
    FROM TipoMovimientoStock
    WHERE Nombre = 'Salida';

    SELECT @IdEstadoCotizacionConvertida = IdEstadoCotizacion
    FROM EstadoCotizacion
    WHERE Nombre = 'Convertida a venta';

    IF @IdEstadoVentaConfirmada IS NULL
        THROW 51017, 'No existe el estado de venta Confirmada.', 1;

    IF @IdEstadoComprobanteEmitido IS NULL
        THROW 51018, 'No existe el estado de comprobante Emitido.', 1;

    IF @IdTipoMovimientoSalida IS NULL
        THROW 51019, 'No existe el tipo de movimiento de stock Salida.', 1;

    SELECT @Total = SUM(Cantidad * PrecioUnitario)
    FROM @Detalles;

    SET @Total = ROUND(ISNULL(@Total, 0), 2);
    SET @Subtotal = ROUND(@Total / 1.18, 2);
    SET @Igv = ROUND(@Total - @Subtotal, 2);

    IF @MontoPagado IS NULL
        SET @MontoPagado = 0;

    SET @MontoPagado = ROUND(@MontoPagado, 2);

    IF @MontoPagado < 0
        THROW 51020, 'El monto cobrado no puede ser negativo.', 1;

    IF @MontoPagado > @Total
        THROW 51021, 'El monto cobrado no puede ser mayor al total.', 1;

    IF @MontoPagado > 0 AND @MetodoPago = ''
        THROW 51022, 'Selecciona el método de cobro.', 1;

    IF @TipoPago = 'Total' AND @MontoPagado <> @Total
        THROW 51023, 'Para cobro total, el monto cobrado debe ser igual al total.', 1;

    IF @TipoPago = 'Parcial' AND (@MontoPagado <= 0 OR @MontoPagado >= @Total)
        THROW 51024, 'Para cobro parcial, el monto debe ser mayor a 0 y menor al total.', 1;

    IF @TipoPago <> 'Cuotas' AND @CuotasJson IS NOT NULL
        THROW 51025, 'Solo las ventas en cuotas pueden enviar cronograma de cuotas.', 1;

    IF @TipoPago = 'Cuotas'
    BEGIN
        IF @NumeroCuotas IS NULL OR @NumeroCuotas <= 0
            THROW 51026, 'Ingresa un número de cuotas válido.', 1;

        IF @NumeroCuotas > 24
            THROW 51027, 'El número máximo permitido es 24 cuotas.', 1;

        IF @FechaPrimerVencimiento IS NULL
            THROW 51028, 'Ingresa la fecha del primer vencimiento.', 1;

        IF @MontoPagado >= @Total
            THROW 51029, 'Para cobro en cuotas, debe quedar un saldo pendiente.', 1;

        IF @CuotasJson IS NOT NULL
        BEGIN
            IF ISJSON(@CuotasJson) <> 1
                THROW 51030, 'El cronograma de cuotas no tiene formato válido.', 1;

            INSERT INTO @CuotasProgramadas (
                NumeroCuota,
                FechaVencimiento
            )
            SELECT
                COALESCE(NumeroCuota, NumeroCuotaPascal),
                COALESCE(FechaVencimiento, FechaVencimientoPascal)
            FROM OPENJSON(@CuotasJson)
            WITH (
                NumeroCuota INT '$.numeroCuota',
                NumeroCuotaPascal INT '$.NumeroCuota',
                FechaVencimiento DATE '$.fechaVencimiento',
                FechaVencimientoPascal DATE '$.FechaVencimiento'
            );

            IF (SELECT COUNT(*) FROM @CuotasProgramadas) <> @NumeroCuotas
                THROW 51031, 'La cantidad de cuotas no coincide con el número de cuotas indicado.', 1;

            IF EXISTS (
                SELECT 1
                FROM @CuotasProgramadas
                WHERE NumeroCuota IS NULL
                   OR NumeroCuota <= 0
                   OR NumeroCuota > @NumeroCuotas
                   OR FechaVencimiento IS NULL
            )
            BEGIN
                THROW 51032, 'Existe una cuota con número o fecha inválida.', 1;
            END;

            IF EXISTS (
                SELECT NumeroCuota
                FROM @CuotasProgramadas
                GROUP BY NumeroCuota
                HAVING COUNT(*) > 1
            )
            BEGIN
                THROW 51033, 'Las cuotas no pueden estar repetidas.', 1;
            END;
        END;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @UltimoNumero = ISNULL(MAX(TRY_CONVERT(INT, Numero)), 0)
        FROM Comprobante WITH (UPDLOCK, HOLDLOCK)
        WHERE Serie = @Serie;

        SET @SiguienteNumero = @UltimoNumero + 1;
        SET @Numero = RIGHT(REPLICATE('0', 6) + CAST(@SiguienteNumero AS NVARCHAR(20)), 6);

        INSERT INTO Venta (
            IdCliente,
            IdCotizacion,
            IdUsuarioRegistro,
            IdEstadoVenta,
            FechaVenta,
            Subtotal,
            Igv,
            Total,
            Observacion
        )
        VALUES (
            @IdCliente,
            @IdCotizacion,
            @IdUsuarioRegistro,
            @IdEstadoVentaConfirmada,
            GETDATE(),
            @Subtotal,
            @Igv,
            @Total,
            @Observacion
        );

        SET @IdVenta = SCOPE_IDENTITY();

        INSERT INTO DetalleVenta (
            IdVenta,
            IdElementoCatalogo,
            Cantidad,
            PrecioUnitario,
            Subtotal
        )
        SELECT
            @IdVenta,
            IdElementoCatalogo,
            Cantidad,
            PrecioUnitario,
            ROUND(Cantidad * PrecioUnitario, 2)
        FROM @Detalles;

        INSERT INTO Comprobante (
            IdVenta,
            IdTipoComprobante,
            IdEstadoComprobante,
            Serie,
            Numero,
            FechaEmision
        )
        VALUES (
            @IdVenta,
            @IdTipoComprobante,
            @IdEstadoComprobanteEmitido,
            @Serie,
            @Numero,
            @FechaEmision
        );

        UPDATE i
        SET
            i.StockActual = i.StockActual - d.Cantidad,
            i.FechaActualizacion = GETDATE()
        FROM Inventario i
        INNER JOIN Producto p ON p.IdProducto = i.IdProducto
        INNER JOIN @Detalles d ON d.IdElementoCatalogo = p.IdElementoCatalogo
        WHERE p.AplicaInventario = 1;

        INSERT INTO MovimientoStock (
            IdProducto,
            IdUsuarioRegistro,
            IdTipoMovimientoStock,
            IdVenta,
            IdCompra,
            Cantidad,
            FechaMovimiento,
            Motivo
        )
        SELECT
            p.IdProducto,
            @IdUsuarioRegistro,
            @IdTipoMovimientoSalida,
            @IdVenta,
            NULL,
            d.Cantidad,
            GETDATE(),
            CONCAT('Salida por venta interna ', @Serie, '-', @Numero)
        FROM @Detalles d
        INNER JOIN Producto p ON p.IdElementoCatalogo = d.IdElementoCatalogo
        WHERE p.AplicaInventario = 1;

        IF @MontoPagado > 0
        BEGIN
            INSERT INTO PagoVenta (
                IdVenta,
                IdUsuarioRegistro,
                MetodoPago,
                MontoPagado,
                FechaPago,
                Observacion
            )
            VALUES (
                @IdVenta,
                @IdUsuarioRegistro,
                @MetodoPago,
                @MontoPagado,
                GETDATE(),
                CASE
                    WHEN @TipoPago = 'Total' THEN 'Cobro total de venta'
                    WHEN @TipoPago = 'Parcial' THEN 'Cobro parcial inicial de venta'
                    WHEN @TipoPago = 'Cuotas' THEN 'Cobro inicial de venta en cuotas'
                    ELSE NULL
                END
            );
        END;

        SET @SaldoPendiente = ROUND(@Total - @MontoPagado, 2);

        IF @TipoPago = 'Cuotas'
        BEGIN
            DECLARE @Contador INT = 1;
            DECLARE @MontoBaseCuota DECIMAL(18,2);
            DECLARE @MontoUltimaCuota DECIMAL(18,2);
            DECLARE @AcumuladoCuotas DECIMAL(18,2) = 0;
            DECLARE @FechaVencimientoCuota DATE;

            SET @MontoBaseCuota = ROUND(@SaldoPendiente / @NumeroCuotas, 2);

            WHILE @Contador <= @NumeroCuotas
            BEGIN
                SELECT @FechaVencimientoCuota = FechaVencimiento
                FROM @CuotasProgramadas
                WHERE NumeroCuota = @Contador;

                IF @FechaVencimientoCuota IS NULL
                    SET @FechaVencimientoCuota = DATEADD(MONTH, @Contador - 1, @FechaPrimerVencimiento);

                IF @Contador < @NumeroCuotas
                BEGIN
                    INSERT INTO CuotaVenta (
                        IdVenta,
                        NumeroCuota,
                        FechaVencimiento,
                        MontoCuota,
                        MontoPagado,
                        EstadoCuota
                    )
                    VALUES (
                        @IdVenta,
                        @Contador,
                        @FechaVencimientoCuota,
                        @MontoBaseCuota,
                        0,
                        'Pendiente'
                    );

                    SET @AcumuladoCuotas = @AcumuladoCuotas + @MontoBaseCuota;
                END
                ELSE
                BEGIN
                    SET @MontoUltimaCuota = ROUND(@SaldoPendiente - @AcumuladoCuotas, 2);

                    INSERT INTO CuotaVenta (
                        IdVenta,
                        NumeroCuota,
                        FechaVencimiento,
                        MontoCuota,
                        MontoPagado,
                        EstadoCuota
                    )
                    VALUES (
                        @IdVenta,
                        @Contador,
                        @FechaVencimientoCuota,
                        @MontoUltimaCuota,
                        0,
                        'Pendiente'
                    );
                END;

                SET @FechaVencimientoCuota = NULL;
                SET @Contador = @Contador + 1;
            END;
        END;

        IF @IdCotizacion IS NOT NULL AND @IdEstadoCotizacionConvertida IS NOT NULL
        BEGIN
            UPDATE Cotizacion
            SET IdEstadoCotizacion = @IdEstadoCotizacionConvertida
            WHERE IdCotizacion = @IdCotizacion;
        END;

        COMMIT TRANSACTION;

        SET @EstadoPago =
            CASE
                WHEN @SaldoPendiente <= 0 THEN 'Pagada'
                WHEN @TipoPago = 'Cuotas' THEN 'En cuotas'
                WHEN @MontoPagado > 0 THEN 'Parcial'
                ELSE 'Pendiente'
            END;

        SELECT
            @IdVenta AS IdVenta,
            @Subtotal AS Subtotal,
            @Igv AS Igv,
            @Total AS Total,
            @MontoPagado AS TotalPagado,
            @SaldoPendiente AS SaldoPendiente,
            @EstadoPago AS EstadoPago,
            CONCAT('Venta registrada correctamente. Documento: ', @Serie, '-', @Numero) AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
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

CREATE   PROCEDURE dbo.sp_ReporteCaja
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
/* =========================================================
   10. REPORTE DE COMPRAS PDF/EXCEL
   ========================================================= */

CREATE   PROCEDURE sp_ReporteCompras
    @Buscar NVARCHAR(200) = NULL,
    @EstadoPago NVARCHAR(30) = NULL,
    @FechaInicio DATE = NULL,
    @FechaFin DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Base AS (
        SELECT
            c.IdCompra,
            c.FechaCompra,
            ISNULL(c.TipoComprobanteProveedor, '-') AS TipoComprobanteProveedor,
            ISNULL(c.SerieComprobante, '-') AS SerieComprobante,
            ISNULL(c.NumeroComprobante, '-') AS NumeroComprobante,
            CONCAT(ISNULL(c.SerieComprobante, '-'), '-', ISNULL(c.NumeroComprobante, '-')) AS DocumentoCompleto,
            p.Ruc AS RucProveedor,
            p.RazonSocial AS RazonSocialProveedor,
            c.Subtotal,
            c.Igv,
            c.Total,
            ISNULL((SELECT SUM(pc.MontoPagado) FROM PagoCompra pc WHERE pc.IdCompra = c.IdCompra), 0) AS TotalPagado,
            c.Total - ISNULL((SELECT SUM(pc.MontoPagado) FROM PagoCompra pc WHERE pc.IdCompra = c.IdCompra), 0) AS SaldoPendiente,
            ec.Nombre AS EstadoCompra,
            CASE
                WHEN ec.Nombre = 'Anulada' THEN 'Anulada'
                WHEN c.Total - ISNULL((SELECT SUM(pc.MontoPagado) FROM PagoCompra pc WHERE pc.IdCompra = c.IdCompra), 0) <= 0 THEN 'Pagada'
                WHEN EXISTS (SELECT 1 FROM CuotaCompra cc WHERE cc.IdCompra = c.IdCompra) THEN 'En cuotas'
                WHEN ISNULL((SELECT SUM(pc.MontoPagado) FROM PagoCompra pc WHERE pc.IdCompra = c.IdCompra), 0) > 0 THEN 'Parcial'
                ELSE 'Pendiente'
            END AS EstadoPago,
            CAST(CASE WHEN EXISTS (SELECT 1 FROM GuiaRemisionCompra g WHERE g.IdCompra = c.IdCompra) THEN 1 ELSE 0 END AS BIT) AS TieneGuia
        FROM Compra c
        INNER JOIN Proveedor p ON p.IdProveedor = c.IdProveedor
        INNER JOIN EstadoCompra ec ON ec.IdEstadoCompra = c.IdEstadoCompra
        WHERE
            (
                @Buscar IS NULL OR @Buscar = '' OR
                p.Ruc LIKE '%' + @Buscar + '%' OR
                p.RazonSocial LIKE '%' + @Buscar + '%' OR
                c.SerieComprobante LIKE '%' + @Buscar + '%' OR
                c.NumeroComprobante LIKE '%' + @Buscar + '%'
            )
            AND (@FechaInicio IS NULL OR CAST(c.FechaCompra AS DATE) >= @FechaInicio)
            AND (@FechaFin IS NULL OR CAST(c.FechaCompra AS DATE) <= @FechaFin)
    )
    SELECT *
    FROM Base
    WHERE @EstadoPago IS NULL OR @EstadoPago = '' OR EstadoPago = @EstadoPago
    ORDER BY IdCompra DESC;
END

GO
/* =========================================================
   10. REPORTE DE VENTAS
   ========================================================= */

CREATE PROCEDURE sp_ReporteVentas
    @Buscar NVARCHAR(150) = NULL,
    @EstadoPago NVARCHAR(30) = NULL,
    @TipoComprobante NVARCHAR(50) = NULL,
    @OrigenVenta NVARCHAR(30) = NULL,
    @FechaInicio DATE = NULL,
    @FechaFin DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Base AS (
        SELECT
            v.IdVenta,
            v.FechaVenta,
            tc.Nombre AS TipoComprobante,
            c.Serie,
            c.Numero,
            CONCAT(c.Serie, '-', c.Numero) AS DocumentoCompleto,
            cl.NumeroDocumento AS DocumentoCliente,
            COALESCE(
                ce.RazonSocial,
                CONCAT(cpn.Nombres, ' ', cpn.ApellidoPaterno, ' ', ISNULL(cpn.ApellidoMaterno, ''))
            ) AS Cliente,
            CASE WHEN v.IdCotizacion IS NULL THEN 'Directa' ELSE 'Cotización' END AS OrigenVenta,
            v.Subtotal,
            v.Igv,
            v.Total,
            ISNULL(pagos.TotalPagado, 0) AS TotalPagado,
            ROUND(v.Total - ISNULL(pagos.TotalPagado, 0), 2) AS SaldoPendiente,
            ev.Nombre AS EstadoVenta,
            CASE
                WHEN ev.Nombre = 'Anulada' THEN 'Anulada'
                WHEN ROUND(v.Total - ISNULL(pagos.TotalPagado, 0), 2) <= 0 THEN 'Pagada'
                WHEN EXISTS (SELECT 1 FROM CuotaVenta cv WHERE cv.IdVenta = v.IdVenta) THEN 'En cuotas'
                WHEN ISNULL(pagos.TotalPagado, 0) > 0 THEN 'Parcial'
                ELSE 'Pendiente'
            END AS EstadoPago
        FROM Venta v
        INNER JOIN EstadoVenta ev ON ev.IdEstadoVenta = v.IdEstadoVenta
        INNER JOIN Cliente cl ON cl.IdCliente = v.IdCliente
        LEFT JOIN ClienteEmpresa ce ON ce.IdCliente = cl.IdCliente
        LEFT JOIN ClientePersonaNatural cpn ON cpn.IdCliente = cl.IdCliente
        INNER JOIN Comprobante c ON c.IdVenta = v.IdVenta
        INNER JOIN TipoComprobante tc ON tc.IdTipoComprobante = c.IdTipoComprobante
        OUTER APPLY (
            SELECT SUM(pv.MontoPagado) AS TotalPagado
            FROM PagoVenta pv
            WHERE pv.IdVenta = v.IdVenta
        ) pagos
        WHERE
            (@Buscar IS NULL OR @Buscar = ''
                OR COALESCE(ce.RazonSocial, CONCAT(cpn.Nombres, ' ', cpn.ApellidoPaterno, ' ', ISNULL(cpn.ApellidoMaterno, ''))) LIKE '%' + @Buscar + '%'
                OR cl.NumeroDocumento LIKE '%' + @Buscar + '%'
                OR c.Serie LIKE '%' + @Buscar + '%'
                OR c.Numero LIKE '%' + @Buscar + '%'
                OR CONCAT(c.Serie, '-', c.Numero) LIKE '%' + @Buscar + '%')
            AND (@TipoComprobante IS NULL OR @TipoComprobante = '' OR tc.Nombre = @TipoComprobante)
            AND (@OrigenVenta IS NULL OR @OrigenVenta = '' OR CASE WHEN v.IdCotizacion IS NULL THEN 'Directa' ELSE 'Cotización' END = @OrigenVenta)
            AND (@FechaInicio IS NULL OR CAST(v.FechaVenta AS DATE) >= @FechaInicio)
            AND (@FechaFin IS NULL OR CAST(v.FechaVenta AS DATE) <= @FechaFin)
    )
    SELECT *
    FROM Base
    WHERE (@EstadoPago IS NULL OR @EstadoPago = '' OR EstadoPago = @EstadoPago)
    ORDER BY FechaVenta DESC;
END

GO
CREATE   PROCEDURE dbo.sp_RevertirCajaCompra
    @IdCompra INT, @IdUsuarioRegistro INT, @Motivo NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @lock INT;
        EXEC @lock=sys.sp_getapplock @Resource=N'Sistema3S:operaciones-caja', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
        IF @lock<0 THROW 72002, 'Caja está ocupada. Intenta nuevamente.', 1;
        EXEC dbo.sp_ExigirAdministradorAuditoria @IdUsuarioRegistro;
        IF NOT EXISTS (SELECT 1 FROM dbo.Compra c JOIN dbo.EstadoCompra e ON e.IdEstadoCompra=c.IdEstadoCompra WHERE c.IdCompra=@IdCompra AND e.Nombre='Anulada')
            THROW 72005, 'La devolución corresponde únicamente a una compra anulada.', 1;
        IF LEN(LTRIM(RTRIM(ISNULL(@Motivo,''))))<5 THROW 72006, 'Ingresa el motivo de la devolución.', 1;
        IF EXISTS (SELECT 1 FROM dbo.MovimientoCaja m JOIN dbo.TipoMovimientoCaja t ON t.IdTipoMovimientoCaja=m.IdTipoMovimientoCaja
            WHERE m.IdCompra=@IdCompra AND m.Estado=1 AND t.Nombre='Egreso por compra'
            AND NOT EXISTS (SELECT 1 FROM dbo.MovimientoCaja r WHERE r.IdMovimientoRevertido=m.IdMovimientoCaja))
        BEGIN
            DECLARE @IdCaja INT, @cantidad INT, @tipo INT;
            SELECT @IdCaja=MAX(c.IdCaja), @cantidad=COUNT(*) FROM dbo.Caja c WITH(UPDLOCK,HOLDLOCK)
                JOIN dbo.EstadoCaja e ON e.IdEstadoCaja=c.IdEstadoCaja WHERE e.Nombre='Abierta' AND e.Estado=1;
            IF @cantidad<>1 THROW 72007, 'Abre una única caja antes de devolver el dinero de la compra anulada.', 1;
            SELECT @tipo=IdTipoMovimientoCaja FROM dbo.TipoMovimientoCaja WHERE Nombre='Ajuste ingreso' AND Estado=1;
            IF @tipo IS NULL THROW 72008, 'No existe el tipo Ajuste ingreso activo.', 1;
            INSERT dbo.MovimientoCaja(IdCaja,IdTipoMovimientoCaja,IdCompra,IdUsuarioRegistro,Monto,Descripcion,FechaMovimiento,MetodoPago,OrigenMovimiento,EsAutomatico,Estado,IdMovimientoRevertido)
            SELECT @IdCaja,@tipo,@IdCompra,@IdUsuarioRegistro,m.Monto,
                LEFT(CONCAT('Devolución compra ',@IdCompra,'; movimiento ',m.IdMovimientoCaja,'. ',@Motivo),300),GETDATE(),m.MetodoPago,'Anulación compra',1,1,m.IdMovimientoCaja
            FROM dbo.MovimientoCaja m JOIN dbo.TipoMovimientoCaja t ON t.IdTipoMovimientoCaja=m.IdTipoMovimientoCaja
            WHERE m.IdCompra=@IdCompra AND m.Estado=1 AND t.Nombre='Egreso por compra'
                AND NOT EXISTS (SELECT 1 FROM dbo.MovimientoCaja r WHERE r.IdMovimientoRevertido=m.IdMovimientoCaja);
            EXEC dbo.sp_RecalcularCajaAuditoria @IdCaja;
        END;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

GO
/* =========================================================
   10. PROCEDIMIENTO: VALIDAR PERMISO DE USUARIO
   ========================================================= */

CREATE   PROCEDURE dbo.sp_ValidarPermisoUsuario
    @IdUsuario INT,
    @Permiso NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TienePermiso BIT = 0;
    DECLARE @IdRol INT;
    DECLARE @NombreRol NVARCHAR(100);

    SET @Permiso = UPPER(LTRIM(RTRIM(ISNULL(@Permiso, ''))));

    SELECT
        @IdRol = u.IdRol,
        @NombreRol = r.Nombre
    FROM dbo.Usuario u
    INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
    WHERE u.IdUsuario = @IdUsuario
      AND u.Estado = 1
      AND r.Estado = 1;

    IF @IdRol IS NULL
    BEGIN
        SELECT CAST(0 AS BIT) AS TienePermiso;
        RETURN;
    END;

    IF UPPER(LTRIM(RTRIM(@NombreRol))) IN ('ADMINISTRADOR', 'ADMIN')
    BEGIN
        SELECT CAST(1 AS BIT) AS TienePermiso;
        RETURN;
    END;

    IF EXISTS (
        SELECT 1
        FROM dbo.RolPermiso rp
        INNER JOIN dbo.Permiso p ON p.IdPermiso = rp.IdPermiso
        WHERE rp.IdRol = @IdRol
          AND p.Estado = 1
          AND UPPER(p.Nombre) = @Permiso
    )
    BEGIN
        SET @TienePermiso = 1;
    END;

    SELECT @TienePermiso AS TienePermiso;
END;

GO
CREATE   TRIGGER dbo.trg_Caja_Auditoria ON dbo.[Caja] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdCaja] IS NULL THEN N'CREAR' WHEN i.[IdCaja] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'Caja',
 COALESCE(i.[IdCaja],d.[IdCaja]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdCaja] IS NOT NULL THEN (SELECT d.[IdCaja],d.[IdUsuarioApertura],d.[IdUsuarioCierre],d.[IdEstadoCaja],d.[FechaApertura],d.[FechaCierre],d.[SaldoInicial],d.[SaldoFinal],d.[SaldoSistema],d.[TotalIngresos],d.[TotalEgresos],d.[SaldoContado],d.[Diferencia],d.[ObservacionApertura],d.[ObservacionCierre] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdCaja] IS NOT NULL THEN (SELECT i.[IdCaja],i.[IdUsuarioApertura],i.[IdUsuarioCierre],i.[IdEstadoCaja],i.[FechaApertura],i.[FechaCierre],i.[SaldoInicial],i.[SaldoFinal],i.[SaldoSistema],i.[TotalIngresos],i.[TotalEgresos],i.[SaldoContado],i.[Diferencia],i.[ObservacionApertura],i.[ObservacionCierre] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdCaja]=d.[IdCaja];
END;

GO
CREATE   TRIGGER dbo.trg_Categoria_Auditoria ON dbo.[Categoria] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdCategoria] IS NULL THEN N'CREAR' WHEN i.[IdCategoria] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'Categoria',
 COALESCE(i.[IdCategoria],d.[IdCategoria]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdCategoria] IS NOT NULL THEN (SELECT d.[IdCategoria],d.[Nombre],d.[Descripcion],d.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdCategoria] IS NOT NULL THEN (SELECT i.[IdCategoria],i.[Nombre],i.[Descripcion],i.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdCategoria]=d.[IdCategoria];
END;

GO
CREATE   TRIGGER dbo.trg_Cliente_Auditoria ON dbo.[Cliente] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdCliente] IS NULL THEN N'CREAR' WHEN i.[IdCliente] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'Cliente',
 COALESCE(i.[IdCliente],d.[IdCliente]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdCliente] IS NOT NULL THEN (SELECT d.[IdCliente],d.[IdUsuario],d.[IdTipoCliente],d.[IdTipoDocumento],d.[IdUbigeo],d.[NumeroDocumento],d.[Correo],d.[Telefono],d.[Direccion],d.[Estado],d.[FechaRegistro] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdCliente] IS NOT NULL THEN (SELECT i.[IdCliente],i.[IdUsuario],i.[IdTipoCliente],i.[IdTipoDocumento],i.[IdUbigeo],i.[NumeroDocumento],i.[Correo],i.[Telefono],i.[Direccion],i.[Estado],i.[FechaRegistro] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdCliente]=d.[IdCliente];
END;

GO
CREATE   TRIGGER dbo.trg_ClienteEmpresa_Auditoria ON dbo.[ClienteEmpresa] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdCliente] IS NULL THEN N'CREAR' WHEN i.[IdCliente] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'ClienteEmpresa',
 COALESCE(i.[IdCliente],d.[IdCliente]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdCliente] IS NOT NULL THEN (SELECT d.[IdCliente],d.[RazonSocial],d.[NombreComercial] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdCliente] IS NOT NULL THEN (SELECT i.[IdCliente],i.[RazonSocial],i.[NombreComercial] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdCliente]=d.[IdCliente];
END;

GO
CREATE   TRIGGER dbo.trg_ClientePersonaNatural_Auditoria ON dbo.[ClientePersonaNatural] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdCliente] IS NULL THEN N'CREAR' WHEN i.[IdCliente] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'ClientePersonaNatural',
 COALESCE(i.[IdCliente],d.[IdCliente]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdCliente] IS NOT NULL THEN (SELECT d.[IdCliente],d.[Nombres],d.[ApellidoPaterno],d.[ApellidoMaterno] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdCliente] IS NOT NULL THEN (SELECT i.[IdCliente],i.[Nombres],i.[ApellidoPaterno],i.[ApellidoMaterno] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdCliente]=d.[IdCliente];
END;

GO
CREATE   TRIGGER dbo.trg_Compra_AnulacionAdministrador ON dbo.Compra AFTER UPDATE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.IdCompra=i.IdCompra JOIN dbo.EstadoCompra e ON e.IdEstadoCompra=i.IdEstadoCompra
   WHERE i.IdEstadoCompra<>d.IdEstadoCompra AND e.Nombre=N'Anulada')
 BEGIN
   DECLARE @usuario INT=TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria'));
   EXEC dbo.sp_ExigirAdministradorAuditoria @usuario;
 END;
END;

GO
CREATE   TRIGGER dbo.trg_Compra_Auditoria ON dbo.[Compra] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdCompra] IS NULL THEN N'CREAR' WHEN i.[IdCompra] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'Compra',
 COALESCE(i.[IdCompra],d.[IdCompra]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdCompra] IS NOT NULL THEN (SELECT d.[IdCompra],d.[IdProveedor],d.[IdUsuarioRegistro],d.[IdEstadoCompra],d.[FechaCompra],d.[Total],d.[TipoComprobanteProveedor],d.[SerieComprobante],d.[NumeroComprobante],d.[FechaEmisionComprobante],d.[Subtotal],d.[Igv],d.[TotalPagado],d.[SaldoPendiente],d.[IdEstadoPagoCompra],d.[Observacion],d.[FechaRegistro],d.[ObservacionCompra] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdCompra] IS NOT NULL THEN (SELECT i.[IdCompra],i.[IdProveedor],i.[IdUsuarioRegistro],i.[IdEstadoCompra],i.[FechaCompra],i.[Total],i.[TipoComprobanteProveedor],i.[SerieComprobante],i.[NumeroComprobante],i.[FechaEmisionComprobante],i.[Subtotal],i.[Igv],i.[TotalPagado],i.[SaldoPendiente],i.[IdEstadoPagoCompra],i.[Observacion],i.[FechaRegistro],i.[ObservacionCompra] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdCompra]=d.[IdCompra];
END;

GO
CREATE   TRIGGER dbo.trg_Comprobante_Auditoria ON dbo.[Comprobante] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdComprobante] IS NULL THEN N'CREAR' WHEN i.[IdComprobante] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'Comprobante',
 COALESCE(i.[IdComprobante],d.[IdComprobante]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdComprobante] IS NOT NULL THEN (SELECT d.[IdComprobante],d.[IdVenta],d.[IdTipoComprobante],d.[IdEstadoComprobante],d.[Serie],d.[Numero],d.[FechaEmision],d.[ArchivoPdf],d.[ArchivoXml],d.[IdSerieComprobante] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdComprobante] IS NOT NULL THEN (SELECT i.[IdComprobante],i.[IdVenta],i.[IdTipoComprobante],i.[IdEstadoComprobante],i.[Serie],i.[Numero],i.[FechaEmision],i.[ArchivoPdf],i.[ArchivoXml],i.[IdSerieComprobante] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdComprobante]=d.[IdComprobante];
END;

GO
CREATE   TRIGGER dbo.trg_ContactoCliente_Auditoria ON dbo.[ContactoCliente] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdContactoCliente] IS NULL THEN N'CREAR' WHEN i.[IdContactoCliente] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'ContactoCliente',
 COALESCE(i.[IdContactoCliente],d.[IdContactoCliente]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdContactoCliente] IS NOT NULL THEN (SELECT d.[IdContactoCliente],d.[IdCliente],d.[Nombres],d.[ApellidoPaterno],d.[ApellidoMaterno],d.[Cargo],d.[Correo],d.[Telefono],d.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdContactoCliente] IS NOT NULL THEN (SELECT i.[IdContactoCliente],i.[IdCliente],i.[Nombres],i.[ApellidoPaterno],i.[ApellidoMaterno],i.[Cargo],i.[Correo],i.[Telefono],i.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdContactoCliente]=d.[IdContactoCliente];
END;

GO
CREATE   TRIGGER dbo.trg_ContactoProveedor_Auditoria ON dbo.[ContactoProveedor] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdContactoProveedor] IS NULL THEN N'CREAR' WHEN i.[IdContactoProveedor] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'ContactoProveedor',
 COALESCE(i.[IdContactoProveedor],d.[IdContactoProveedor]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdContactoProveedor] IS NOT NULL THEN (SELECT d.[IdContactoProveedor],d.[IdProveedor],d.[Nombres],d.[ApellidoPaterno],d.[ApellidoMaterno],d.[Cargo],d.[Correo],d.[Telefono],d.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdContactoProveedor] IS NOT NULL THEN (SELECT i.[IdContactoProveedor],i.[IdProveedor],i.[Nombres],i.[ApellidoPaterno],i.[ApellidoMaterno],i.[Cargo],i.[Correo],i.[Telefono],i.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdContactoProveedor]=d.[IdContactoProveedor];
END;

GO
CREATE   TRIGGER dbo.trg_Cotizacion_AnulacionAdministrador ON dbo.Cotizacion AFTER UPDATE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.IdCotizacion=i.IdCotizacion JOIN dbo.EstadoCotizacion e ON e.IdEstadoCotizacion=i.IdEstadoCotizacion
   WHERE i.IdEstadoCotizacion<>d.IdEstadoCotizacion AND e.Nombre=N'Cancelada')
 BEGIN
   DECLARE @usuario INT=TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria'));
   EXEC dbo.sp_ExigirAdministradorAuditoria @usuario;
 END;
END;

GO
CREATE   TRIGGER dbo.trg_Cotizacion_Auditoria ON dbo.[Cotizacion] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdCotizacion] IS NULL THEN N'CREAR' WHEN i.[IdCotizacion] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'Cotizacion',
 COALESCE(i.[IdCotizacion],d.[IdCotizacion]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdCotizacion] IS NOT NULL THEN (SELECT d.[IdCotizacion],d.[IdCliente],d.[IdUsuarioRegistro],d.[IdUsuarioAtencion],d.[IdEstadoCotizacion],d.[FechaCotizacion],d.[TotalReferencial],d.[Observacion],d.[ArchivoPdf],d.[CorreoEnviado],d.[OrigenCotizacion],d.[Subtotal],d.[Descuento],d.[Igv],d.[Total],d.[WhatsappEnviado],d.[FechaRespuesta],d.[CanalRespuesta],d.[FechaActualizacion],d.[IdVentaGenerada] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdCotizacion] IS NOT NULL THEN (SELECT i.[IdCotizacion],i.[IdCliente],i.[IdUsuarioRegistro],i.[IdUsuarioAtencion],i.[IdEstadoCotizacion],i.[FechaCotizacion],i.[TotalReferencial],i.[Observacion],i.[ArchivoPdf],i.[CorreoEnviado],i.[OrigenCotizacion],i.[Subtotal],i.[Descuento],i.[Igv],i.[Total],i.[WhatsappEnviado],i.[FechaRespuesta],i.[CanalRespuesta],i.[FechaActualizacion],i.[IdVentaGenerada] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdCotizacion]=d.[IdCotizacion];
END;

GO
CREATE   TRIGGER dbo.trg_CuotaCompra_Auditoria ON dbo.[CuotaCompra] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdCuotaCompra] IS NULL THEN N'CREAR' WHEN i.[IdCuotaCompra] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'CuotaCompra',
 COALESCE(i.[IdCuotaCompra],d.[IdCuotaCompra]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdCuotaCompra] IS NOT NULL THEN (SELECT d.[IdCuotaCompra],d.[IdCompra],d.[NumeroCuota],d.[FechaVencimiento],d.[MontoCuota],d.[MontoPagado],d.[EstadoCuota],d.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdCuotaCompra] IS NOT NULL THEN (SELECT i.[IdCuotaCompra],i.[IdCompra],i.[NumeroCuota],i.[FechaVencimiento],i.[MontoCuota],i.[MontoPagado],i.[EstadoCuota],i.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdCuotaCompra]=d.[IdCuotaCompra];
END;

GO
CREATE   TRIGGER dbo.trg_CuotaVenta_Auditoria ON dbo.[CuotaVenta] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdCuotaVenta] IS NULL THEN N'CREAR' WHEN i.[IdCuotaVenta] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'CuotaVenta',
 COALESCE(i.[IdCuotaVenta],d.[IdCuotaVenta]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdCuotaVenta] IS NOT NULL THEN (SELECT d.[IdCuotaVenta],d.[IdVenta],d.[NumeroCuota],d.[FechaVencimiento],d.[MontoCuota],d.[MontoPagado],d.[EstadoCuota] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdCuotaVenta] IS NOT NULL THEN (SELECT i.[IdCuotaVenta],i.[IdVenta],i.[NumeroCuota],i.[FechaVencimiento],i.[MontoCuota],i.[MontoPagado],i.[EstadoCuota] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdCuotaVenta]=d.[IdCuotaVenta];
END;

GO
CREATE   TRIGGER dbo.trg_DetalleCompra_Auditoria ON dbo.[DetalleCompra] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdDetalleCompra] IS NULL THEN N'CREAR' WHEN i.[IdDetalleCompra] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'DetalleCompra',
 COALESCE(i.[IdDetalleCompra],d.[IdDetalleCompra]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdDetalleCompra] IS NOT NULL THEN (SELECT d.[IdDetalleCompra],d.[IdCompra],d.[IdProducto],d.[Cantidad],d.[PrecioCompra],d.[Subtotal] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdDetalleCompra] IS NOT NULL THEN (SELECT i.[IdDetalleCompra],i.[IdCompra],i.[IdProducto],i.[Cantidad],i.[PrecioCompra],i.[Subtotal] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdDetalleCompra]=d.[IdDetalleCompra];
END;

GO
CREATE   TRIGGER dbo.trg_DetalleCotizacion_Auditoria ON dbo.[DetalleCotizacion] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdDetalleCotizacion] IS NULL THEN N'CREAR' WHEN i.[IdDetalleCotizacion] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'DetalleCotizacion',
 COALESCE(i.[IdDetalleCotizacion],d.[IdDetalleCotizacion]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdDetalleCotizacion] IS NOT NULL THEN (SELECT d.[IdDetalleCotizacion],d.[IdCotizacion],d.[IdElementoCatalogo],d.[Cantidad],d.[PrecioUnitario],d.[Subtotal],d.[Observacion] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdDetalleCotizacion] IS NOT NULL THEN (SELECT i.[IdDetalleCotizacion],i.[IdCotizacion],i.[IdElementoCatalogo],i.[Cantidad],i.[PrecioUnitario],i.[Subtotal],i.[Observacion] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdDetalleCotizacion]=d.[IdDetalleCotizacion];
END;

GO
CREATE   TRIGGER dbo.trg_DetalleVenta_Auditoria ON dbo.[DetalleVenta] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdDetalleVenta] IS NULL THEN N'CREAR' WHEN i.[IdDetalleVenta] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'DetalleVenta',
 COALESCE(i.[IdDetalleVenta],d.[IdDetalleVenta]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdDetalleVenta] IS NOT NULL THEN (SELECT d.[IdDetalleVenta],d.[IdVenta],d.[IdElementoCatalogo],d.[Cantidad],d.[PrecioUnitario],d.[Subtotal] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdDetalleVenta] IS NOT NULL THEN (SELECT i.[IdDetalleVenta],i.[IdVenta],i.[IdElementoCatalogo],i.[Cantidad],i.[PrecioUnitario],i.[Subtotal] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdDetalleVenta]=d.[IdDetalleVenta];
END;

GO
CREATE   TRIGGER dbo.trg_ElementoCatalogo_Auditoria ON dbo.[ElementoCatalogo] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdElementoCatalogo] IS NULL THEN N'CREAR' WHEN i.[IdElementoCatalogo] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'ElementoCatalogo',
 COALESCE(i.[IdElementoCatalogo],d.[IdElementoCatalogo]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdElementoCatalogo] IS NOT NULL THEN (SELECT d.[IdElementoCatalogo],d.[IdTipoElemento],d.[Nombre],d.[Descripcion],d.[PrecioReferencial],d.[ImagenUrl],d.[Estado],d.[FechaRegistro] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdElementoCatalogo] IS NOT NULL THEN (SELECT i.[IdElementoCatalogo],i.[IdTipoElemento],i.[Nombre],i.[Descripcion],i.[PrecioReferencial],i.[ImagenUrl],i.[Estado],i.[FechaRegistro] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdElementoCatalogo]=d.[IdElementoCatalogo];
END;

GO
CREATE   TRIGGER dbo.trg_EnvioCorreo_Auditoria ON dbo.[EnvioCorreo] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdEnvioCorreo] IS NULL THEN N'CREAR' WHEN i.[IdEnvioCorreo] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'EnvioCorreo',
 COALESCE(i.[IdEnvioCorreo],d.[IdEnvioCorreo]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdEnvioCorreo] IS NOT NULL THEN (SELECT d.[IdEnvioCorreo],d.[IdCotizacion],d.[IdComprobante],d.[IdUsuarioRegistro],d.[Destinatario],d.[Asunto],d.[FechaEnvio],d.[Exitoso] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdEnvioCorreo] IS NOT NULL THEN (SELECT i.[IdEnvioCorreo],i.[IdCotizacion],i.[IdComprobante],i.[IdUsuarioRegistro],i.[Destinatario],i.[Asunto],i.[FechaEnvio],i.[Exitoso] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdEnvioCorreo]=d.[IdEnvioCorreo];
END;

GO
CREATE   TRIGGER dbo.trg_GuiaRemisionCompra_Auditoria ON dbo.[GuiaRemisionCompra] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdGuiaRemisionCompra] IS NULL THEN N'CREAR' WHEN i.[IdGuiaRemisionCompra] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'GuiaRemisionCompra',
 COALESCE(i.[IdGuiaRemisionCompra],d.[IdGuiaRemisionCompra]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdGuiaRemisionCompra] IS NOT NULL THEN (SELECT d.[IdGuiaRemisionCompra],d.[IdCompra],d.[NumeroGuia],d.[FechaEmision],d.[FechaTraslado],d.[PuntoPartida],d.[PuntoLlegada],d.[Transportista],d.[RucTransportista],d.[PlacaVehiculo],d.[Observacion],d.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdGuiaRemisionCompra] IS NOT NULL THEN (SELECT i.[IdGuiaRemisionCompra],i.[IdCompra],i.[NumeroGuia],i.[FechaEmision],i.[FechaTraslado],i.[PuntoPartida],i.[PuntoLlegada],i.[Transportista],i.[RucTransportista],i.[PlacaVehiculo],i.[Observacion],i.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdGuiaRemisionCompra]=d.[IdGuiaRemisionCompra];
END;

GO
CREATE   TRIGGER dbo.trg_Inventario_Auditoria ON dbo.[Inventario] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdInventario] IS NULL THEN N'CREAR' WHEN i.[IdInventario] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'Inventario',
 COALESCE(i.[IdInventario],d.[IdInventario]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdInventario] IS NOT NULL THEN (SELECT d.[IdInventario],d.[IdProducto],d.[StockActual],d.[StockMinimo],d.[FechaActualizacion] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdInventario] IS NOT NULL THEN (SELECT i.[IdInventario],i.[IdProducto],i.[StockActual],i.[StockMinimo],i.[FechaActualizacion] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdInventario]=d.[IdInventario];
END;

GO
CREATE   TRIGGER dbo.trg_Marca_Auditoria ON dbo.[Marca] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdMarca] IS NULL THEN N'CREAR' WHEN i.[IdMarca] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'Marca',
 COALESCE(i.[IdMarca],d.[IdMarca]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdMarca] IS NOT NULL THEN (SELECT d.[IdMarca],d.[Nombre],d.[LogoUrl],d.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdMarca] IS NOT NULL THEN (SELECT i.[IdMarca],i.[Nombre],i.[LogoUrl],i.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdMarca]=d.[IdMarca];
END;

GO
CREATE   TRIGGER dbo.trg_MovimientoCaja_Auditoria ON dbo.[MovimientoCaja] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdMovimientoCaja] IS NULL THEN N'CREAR' WHEN i.[IdMovimientoCaja] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'MovimientoCaja',
 COALESCE(i.[IdMovimientoCaja],d.[IdMovimientoCaja]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdMovimientoCaja] IS NOT NULL THEN (SELECT d.[IdMovimientoCaja],d.[IdCaja],d.[IdTipoMovimientoCaja],d.[IdVenta],d.[IdCompra],d.[IdUsuarioRegistro],d.[Monto],d.[Descripcion],d.[FechaMovimiento],d.[IdPagoVenta],d.[IdPagoCompra],d.[MetodoPago],d.[OrigenMovimiento],d.[EsAutomatico],d.[Estado],d.[IdMovimientoRevertido] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdMovimientoCaja] IS NOT NULL THEN (SELECT i.[IdMovimientoCaja],i.[IdCaja],i.[IdTipoMovimientoCaja],i.[IdVenta],i.[IdCompra],i.[IdUsuarioRegistro],i.[Monto],i.[Descripcion],i.[FechaMovimiento],i.[IdPagoVenta],i.[IdPagoCompra],i.[MetodoPago],i.[OrigenMovimiento],i.[EsAutomatico],i.[Estado],i.[IdMovimientoRevertido] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdMovimientoCaja]=d.[IdMovimientoCaja];
END;

GO
CREATE   TRIGGER dbo.trg_MovimientoCaja_ValidarSaldo ON dbo.MovimientoCaja AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    -- El bloqueo se comparte con apertura, cierre, pagos y anulaciones.
    DECLARE @lock INT;
    EXEC @lock=sys.sp_getapplock @Resource=N'Sistema3S:operaciones-caja', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
    IF @lock<0 THROW 72002, 'Caja está ocupada. Intenta nuevamente.', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.Caja c ON c.IdCaja=i.IdCaja JOIN dbo.EstadoCaja e ON e.IdEstadoCaja=c.IdEstadoCaja WHERE e.Nombre<>'Abierta')
       OR EXISTS (SELECT 1 FROM deleted i JOIN dbo.Caja c ON c.IdCaja=i.IdCaja JOIN dbo.EstadoCaja e ON e.IdEstadoCaja=c.IdEstadoCaja WHERE e.Nombre<>'Abierta')
        THROW 72003, 'No se pueden modificar los movimientos de una caja cerrada.', 1;
    -- Una devolución puede sanear un saldo negativo histórico; sólo bloqueamos las nuevas salidas netas.
    IF EXISTS (
        SELECT 1 FROM dbo.Caja c WITH (UPDLOCK,HOLDLOCK)
        CROSS APPLY (SELECT ISNULL(SUM(CASE WHEN t.Nombre IN ('Ingreso por venta','Ingreso manual','Ajuste ingreso') THEN m.Monto ELSE -m.Monto END),0) neto
            FROM dbo.MovimientoCaja m JOIN dbo.TipoMovimientoCaja t ON t.IdTipoMovimientoCaja=m.IdTipoMovimientoCaja WHERE m.IdCaja=c.IdCaja AND m.Estado=1) s
        CROSS APPLY (SELECT ISNULL(SUM(d.valor),0) delta FROM (
            SELECT CASE WHEN t.Nombre IN ('Ingreso por venta','Ingreso manual','Ajuste ingreso') THEN i.Monto ELSE -i.Monto END valor
                FROM inserted i JOIN dbo.TipoMovimientoCaja t ON t.IdTipoMovimientoCaja=i.IdTipoMovimientoCaja WHERE i.IdCaja=c.IdCaja AND i.Estado=1
            UNION ALL SELECT CASE WHEN t.Nombre IN ('Ingreso por venta','Ingreso manual','Ajuste ingreso') THEN -i.Monto ELSE i.Monto END
                FROM deleted i JOIN dbo.TipoMovimientoCaja t ON t.IdTipoMovimientoCaja=i.IdTipoMovimientoCaja WHERE i.IdCaja=c.IdCaja AND i.Estado=1
        ) d) cambio
        WHERE cambio.delta<0 AND c.SaldoInicial+s.neto<0
    ) THROW 72004, 'Saldo de caja insuficiente para el pago. Reduce el importe pagado o registra un ingreso antes de continuar.', 1;
END;

GO
CREATE   TRIGGER dbo.trg_MovimientoStock_Auditoria ON dbo.[MovimientoStock] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdMovimientoStock] IS NULL THEN N'CREAR' WHEN i.[IdMovimientoStock] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'MovimientoStock',
 COALESCE(i.[IdMovimientoStock],d.[IdMovimientoStock]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdMovimientoStock] IS NOT NULL THEN (SELECT d.[IdMovimientoStock],d.[IdProducto],d.[IdUsuarioRegistro],d.[IdTipoMovimientoStock],d.[IdVenta],d.[IdCompra],d.[Cantidad],d.[FechaMovimiento],d.[Motivo] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdMovimientoStock] IS NOT NULL THEN (SELECT i.[IdMovimientoStock],i.[IdProducto],i.[IdUsuarioRegistro],i.[IdTipoMovimientoStock],i.[IdVenta],i.[IdCompra],i.[Cantidad],i.[FechaMovimiento],i.[Motivo] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdMovimientoStock]=d.[IdMovimientoStock];
END;

GO
CREATE   TRIGGER dbo.trg_PagoCompra_Auditoria ON dbo.[PagoCompra] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdPagoCompra] IS NULL THEN N'CREAR' WHEN i.[IdPagoCompra] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'PagoCompra',
 COALESCE(i.[IdPagoCompra],d.[IdPagoCompra]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdPagoCompra] IS NOT NULL THEN (SELECT d.[IdPagoCompra],d.[IdCompra],d.[IdUsuarioRegistro],d.[MetodoPago],d.[MontoPagado],d.[FechaPago],d.[Observacion],d.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdPagoCompra] IS NOT NULL THEN (SELECT i.[IdPagoCompra],i.[IdCompra],i.[IdUsuarioRegistro],i.[MetodoPago],i.[MontoPagado],i.[FechaPago],i.[Observacion],i.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdPagoCompra]=d.[IdPagoCompra];
END;

GO
/* =========================================================
   4. TRIGGER: PAGO DE COMPRA → EGRESO AUTOMÁTICO A CAJA
   ========================================================= */

CREATE   TRIGGER dbo.trg_PagoCompra_MovimientoCaja
ON dbo.PagoCompra
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdCaja INT;
    DECLARE @CantidadCajasAbiertas INT;
    DECLARE @IdTipoMovimientoCaja INT;

    IF NOT EXISTS (
        SELECT 1
        FROM inserted
        WHERE MontoPagado > 0
          AND Estado = 1
    )
    BEGIN
        RETURN;
    END;

    SELECT @CantidadCajasAbiertas = COUNT(*)
    FROM dbo.Caja c
    INNER JOIN dbo.EstadoCaja ec ON ec.IdEstadoCaja = c.IdEstadoCaja
    WHERE ec.Nombre = 'Abierta'
      AND ec.Estado = 1;

    IF @CantidadCajasAbiertas = 0
    BEGIN
        THROW 71101, 'No existe una caja abierta. El administrador debe abrir caja antes de registrar pagos de compra.', 1;
    END;

    IF @CantidadCajasAbiertas > 1
    BEGIN
        THROW 71102, 'Existe más de una caja abierta. Corrige el estado de caja antes de registrar pagos.', 1;
    END;

    SELECT TOP 1 @IdCaja = c.IdCaja
    FROM dbo.Caja c
    INNER JOIN dbo.EstadoCaja ec ON ec.IdEstadoCaja = c.IdEstadoCaja
    WHERE ec.Nombre = 'Abierta'
      AND ec.Estado = 1
    ORDER BY c.IdCaja DESC;

    SELECT @IdTipoMovimientoCaja = IdTipoMovimientoCaja
    FROM dbo.TipoMovimientoCaja
    WHERE Nombre = 'Egreso por compra'
      AND Estado = 1;

    IF @IdTipoMovimientoCaja IS NULL
    BEGIN
        THROW 71103, 'No existe el tipo de movimiento de caja: Egreso por compra.', 1;
    END;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        LEFT JOIN dbo.Usuario u ON u.IdUsuario = i.IdUsuarioRegistro
        WHERE u.IdUsuario IS NULL
           OR u.Estado = 0
    )
    BEGIN
        THROW 71104, 'El usuario que registra el pago de compra no es válido para caja.', 1;
    END;

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
    SELECT
        @IdCaja,
        @IdTipoMovimientoCaja,
        NULL,
        i.IdCompra,
        i.IdUsuarioRegistro,
        ROUND(i.MontoPagado, 2),
        COALESCE(
            NULLIF(LTRIM(RTRIM(i.Observacion)), ''),
            CONCAT('Egreso automático por pago de compra N° ', i.IdCompra)
        ),
        ISNULL(i.FechaPago, GETDATE()),
        NULL,
        i.IdPagoCompra,
        i.MetodoPago,
        'Compra',
        1,
        1
    FROM inserted i
    WHERE i.MontoPagado > 0
      AND i.Estado = 1
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.MovimientoCaja mc
          WHERE mc.IdPagoCompra = i.IdPagoCompra
            AND mc.Estado = 1
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
END;

GO
CREATE   TRIGGER dbo.trg_PagoCompraCuota_Auditoria ON dbo.[PagoCompraCuota] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdPagoCompraCuota] IS NULL THEN N'CREAR' WHEN i.[IdPagoCompraCuota] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'PagoCompraCuota',
 COALESCE(i.[IdPagoCompraCuota],d.[IdPagoCompraCuota]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdPagoCompraCuota] IS NOT NULL THEN (SELECT d.[IdPagoCompraCuota],d.[IdPagoCompra],d.[IdCuotaCompra],d.[MontoAplicado],d.[FechaRegistro],d.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdPagoCompraCuota] IS NOT NULL THEN (SELECT i.[IdPagoCompraCuota],i.[IdPagoCompra],i.[IdCuotaCompra],i.[MontoAplicado],i.[FechaRegistro],i.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdPagoCompraCuota]=d.[IdPagoCompraCuota];
END;

GO
CREATE   TRIGGER dbo.trg_PagoVenta_Auditoria ON dbo.[PagoVenta] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdPagoVenta] IS NULL THEN N'CREAR' WHEN i.[IdPagoVenta] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'PagoVenta',
 COALESCE(i.[IdPagoVenta],d.[IdPagoVenta]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdPagoVenta] IS NOT NULL THEN (SELECT d.[IdPagoVenta],d.[IdVenta],d.[IdUsuarioRegistro],d.[MetodoPago],d.[MontoPagado],d.[FechaPago],d.[Observacion] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdPagoVenta] IS NOT NULL THEN (SELECT i.[IdPagoVenta],i.[IdVenta],i.[IdUsuarioRegistro],i.[MetodoPago],i.[MontoPagado],i.[FechaPago],i.[Observacion] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdPagoVenta]=d.[IdPagoVenta];
END;

GO
/* =========================================================
   3. TRIGGER: PAGO DE VENTA → INGRESO AUTOMÁTICO A CAJA
   ========================================================= */

CREATE   TRIGGER dbo.trg_PagoVenta_MovimientoCaja
ON dbo.PagoVenta
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdCaja INT;
    DECLARE @CantidadCajasAbiertas INT;
    DECLARE @IdTipoMovimientoCaja INT;

    IF NOT EXISTS (
        SELECT 1
        FROM inserted
        WHERE MontoPagado > 0
    )
    BEGIN
        RETURN;
    END;

    SELECT @CantidadCajasAbiertas = COUNT(*)
    FROM dbo.Caja c
    INNER JOIN dbo.EstadoCaja ec ON ec.IdEstadoCaja = c.IdEstadoCaja
    WHERE ec.Nombre = 'Abierta'
      AND ec.Estado = 1;

    IF @CantidadCajasAbiertas = 0
    BEGIN
        THROW 71001, 'No existe una caja abierta. El administrador debe abrir caja antes de registrar cobros de venta.', 1;
    END;

    IF @CantidadCajasAbiertas > 1
    BEGIN
        THROW 71002, 'Existe más de una caja abierta. Corrige el estado de caja antes de registrar cobros.', 1;
    END;

    SELECT TOP 1 @IdCaja = c.IdCaja
    FROM dbo.Caja c
    INNER JOIN dbo.EstadoCaja ec ON ec.IdEstadoCaja = c.IdEstadoCaja
    WHERE ec.Nombre = 'Abierta'
      AND ec.Estado = 1
    ORDER BY c.IdCaja DESC;

    SELECT @IdTipoMovimientoCaja = IdTipoMovimientoCaja
    FROM dbo.TipoMovimientoCaja
    WHERE Nombre = 'Ingreso por venta'
      AND Estado = 1;

    IF @IdTipoMovimientoCaja IS NULL
    BEGIN
        THROW 71003, 'No existe el tipo de movimiento de caja: Ingreso por venta.', 1;
    END;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        LEFT JOIN dbo.Usuario u ON u.IdUsuario = i.IdUsuarioRegistro
        WHERE u.IdUsuario IS NULL
           OR u.Estado = 0
    )
    BEGIN
        THROW 71004, 'El usuario que registra el cobro de venta no es válido para caja.', 1;
    END;

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
    SELECT
        @IdCaja,
        @IdTipoMovimientoCaja,
        i.IdVenta,
        NULL,
        i.IdUsuarioRegistro,
        ROUND(i.MontoPagado, 2),
        COALESCE(
            NULLIF(LTRIM(RTRIM(i.Observacion)), ''),
            CONCAT('Ingreso automático por cobro de venta N° ', i.IdVenta)
        ),
        ISNULL(i.FechaPago, GETDATE()),
        i.IdPagoVenta,
        NULL,
        i.MetodoPago,
        'Venta',
        1,
        1
    FROM inserted i
    WHERE i.MontoPagado > 0
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.MovimientoCaja mc
          WHERE mc.IdPagoVenta = i.IdPagoVenta
            AND mc.Estado = 1
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
END;

GO
CREATE   TRIGGER dbo.trg_PagoVentaCuota_Auditoria ON dbo.[PagoVentaCuota] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdPagoVentaCuota] IS NULL THEN N'CREAR' WHEN i.[IdPagoVentaCuota] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'PagoVentaCuota',
 COALESCE(i.[IdPagoVentaCuota],d.[IdPagoVentaCuota]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdPagoVentaCuota] IS NOT NULL THEN (SELECT d.[IdPagoVentaCuota],d.[IdPagoVenta],d.[IdCuotaVenta],d.[MontoAplicado],d.[FechaRegistro],d.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdPagoVentaCuota] IS NOT NULL THEN (SELECT i.[IdPagoVentaCuota],i.[IdPagoVenta],i.[IdCuotaVenta],i.[MontoAplicado],i.[FechaRegistro],i.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdPagoVentaCuota]=d.[IdPagoVentaCuota];
END;

GO
CREATE   TRIGGER dbo.trg_Permiso_Auditoria ON dbo.[Permiso] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdPermiso] IS NULL THEN N'CREAR' WHEN i.[IdPermiso] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'Permiso',
 COALESCE(i.[IdPermiso],d.[IdPermiso]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdPermiso] IS NOT NULL THEN (SELECT d.[IdPermiso],d.[Nombre],d.[Descripcion],d.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdPermiso] IS NOT NULL THEN (SELECT i.[IdPermiso],i.[Nombre],i.[Descripcion],i.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdPermiso]=d.[IdPermiso];
END;

GO
CREATE   TRIGGER dbo.trg_PersonalInterno_Auditoria ON dbo.[PersonalInterno] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdPersonalInterno] IS NULL THEN N'CREAR' WHEN i.[IdPersonalInterno] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'PersonalInterno',
 COALESCE(i.[IdPersonalInterno],d.[IdPersonalInterno]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdPersonalInterno] IS NOT NULL THEN (SELECT d.[IdPersonalInterno],d.[IdUsuario],d.[Nombres],d.[ApellidoPaterno],d.[ApellidoMaterno],d.[Telefono],d.[Cargo],d.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdPersonalInterno] IS NOT NULL THEN (SELECT i.[IdPersonalInterno],i.[IdUsuario],i.[Nombres],i.[ApellidoPaterno],i.[ApellidoMaterno],i.[Telefono],i.[Cargo],i.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdPersonalInterno]=d.[IdPersonalInterno];
END;

GO
CREATE   TRIGGER dbo.trg_Producto_Auditoria ON dbo.[Producto] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdProducto] IS NULL THEN N'CREAR' WHEN i.[IdProducto] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'Producto',
 COALESCE(i.[IdProducto],d.[IdProducto]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdProducto] IS NOT NULL THEN (SELECT d.[IdProducto],d.[IdElementoCatalogo],d.[IdCategoria],d.[IdMarca],d.[IdUnidadMedida],d.[CodigoProducto],d.[FichaTecnicaPdf],d.[AplicaInventario] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdProducto] IS NOT NULL THEN (SELECT i.[IdProducto],i.[IdElementoCatalogo],i.[IdCategoria],i.[IdMarca],i.[IdUnidadMedida],i.[CodigoProducto],i.[FichaTecnicaPdf],i.[AplicaInventario] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdProducto]=d.[IdProducto];
END;

GO
CREATE   TRIGGER dbo.trg_Proveedor_Auditoria ON dbo.[Proveedor] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdProveedor] IS NULL THEN N'CREAR' WHEN i.[IdProveedor] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'Proveedor',
 COALESCE(i.[IdProveedor],d.[IdProveedor]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdProveedor] IS NOT NULL THEN (SELECT d.[IdProveedor],d.[IdUbigeo],d.[Ruc],d.[RazonSocial],d.[NombreComercial],d.[Correo],d.[Telefono],d.[Direccion],d.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdProveedor] IS NOT NULL THEN (SELECT i.[IdProveedor],i.[IdUbigeo],i.[Ruc],i.[RazonSocial],i.[NombreComercial],i.[Correo],i.[Telefono],i.[Direccion],i.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdProveedor]=d.[IdProveedor];
END;

GO
CREATE   TRIGGER dbo.trg_Rol_Auditoria ON dbo.[Rol] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdRol] IS NULL THEN N'CREAR' WHEN i.[IdRol] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'Rol',
 COALESCE(i.[IdRol],d.[IdRol]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdRol] IS NOT NULL THEN (SELECT d.[IdRol],d.[Nombre],d.[Descripcion],d.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdRol] IS NOT NULL THEN (SELECT i.[IdRol],i.[Nombre],i.[Descripcion],i.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdRol]=d.[IdRol];
END;

GO
CREATE   TRIGGER dbo.trg_RolPermiso_Auditoria ON dbo.[RolPermiso] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdRol] IS NULL THEN N'CREAR' WHEN i.[IdRol] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'RolPermiso',
 NULL,GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdRol] IS NOT NULL THEN (SELECT d.[IdRol],d.[IdPermiso] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdRol] IS NOT NULL THEN (SELECT i.[IdRol],i.[IdPermiso] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdRol]=d.[IdRol] AND i.[IdPermiso]=d.[IdPermiso];
END;

GO
CREATE   TRIGGER dbo.trg_SerieComprobante_Auditoria ON dbo.[SerieComprobante] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdSerieComprobante] IS NULL THEN N'CREAR' WHEN i.[IdSerieComprobante] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'SerieComprobante',
 COALESCE(i.[IdSerieComprobante],d.[IdSerieComprobante]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdSerieComprobante] IS NOT NULL THEN (SELECT d.[IdSerieComprobante],d.[IdTipoComprobante],d.[Serie],d.[NumeroActual],d.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdSerieComprobante] IS NOT NULL THEN (SELECT i.[IdSerieComprobante],i.[IdTipoComprobante],i.[Serie],i.[NumeroActual],i.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdSerieComprobante]=d.[IdSerieComprobante];
END;

GO
CREATE   TRIGGER dbo.trg_Servicio_Auditoria ON dbo.[Servicio] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdServicio] IS NULL THEN N'CREAR' WHEN i.[IdServicio] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'Servicio',
 COALESCE(i.[IdServicio],d.[IdServicio]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdServicio] IS NOT NULL THEN (SELECT d.[IdServicio],d.[IdElementoCatalogo],d.[SectorAplicacion],d.[MensajeWhatsApp],d.[RequiereVisitaTecnica] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdServicio] IS NOT NULL THEN (SELECT i.[IdServicio],i.[IdElementoCatalogo],i.[SectorAplicacion],i.[MensajeWhatsApp],i.[RequiereVisitaTecnica] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdServicio]=d.[IdServicio];
END;

GO
CREATE   TRIGGER dbo.trg_UnidadMedida_Auditoria ON dbo.[UnidadMedida] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdUnidadMedida] IS NULL THEN N'CREAR' WHEN i.[IdUnidadMedida] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'UnidadMedida',
 COALESCE(i.[IdUnidadMedida],d.[IdUnidadMedida]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdUnidadMedida] IS NOT NULL THEN (SELECT d.[IdUnidadMedida],d.[Nombre],d.[Abreviatura],d.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdUnidadMedida] IS NOT NULL THEN (SELECT i.[IdUnidadMedida],i.[Nombre],i.[Abreviatura],i.[Estado] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdUnidadMedida]=d.[IdUnidadMedida];
END;

GO
CREATE   TRIGGER dbo.trg_Usuario_Auditoria ON dbo.[Usuario] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdUsuario] IS NULL THEN N'CREAR' WHEN i.[IdUsuario] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'Usuario',
 COALESCE(i.[IdUsuario],d.[IdUsuario]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdUsuario] IS NOT NULL THEN (SELECT d.[IdUsuario],d.[IdRol],d.[Correo],d.[Estado],d.[FechaRegistro] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdUsuario] IS NOT NULL THEN (SELECT i.[IdUsuario],i.[IdRol],i.[Correo],i.[Estado],i.[FechaRegistro] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdUsuario]=d.[IdUsuario];
END;

GO
CREATE   TRIGGER dbo.trg_Venta_AnulacionAdministrador ON dbo.Venta AFTER UPDATE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.IdVenta=i.IdVenta JOIN dbo.EstadoVenta e ON e.IdEstadoVenta=i.IdEstadoVenta
   WHERE i.IdEstadoVenta<>d.IdEstadoVenta AND e.Nombre=N'Anulada')
 BEGIN
   DECLARE @usuario INT=TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria'));
   EXEC dbo.sp_ExigirAdministradorAuditoria @usuario;
 END;
END;

GO
CREATE   TRIGGER dbo.trg_Venta_Auditoria ON dbo.[Venta] AFTER INSERT, UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditoriaLog(IdUsuario,Accion,TablaAfectada,IdRegistro,Fecha,Descripcion,DatosAntes,DatosDespues,Solicitud,Origen)
 SELECT TRY_CONVERT(INT,SESSION_CONTEXT(N'IdUsuarioAuditoria')),
 CASE WHEN d.[IdVenta] IS NULL THEN N'CREAR' WHEN i.[IdVenta] IS NULL THEN N'ELIMINAR' ELSE N'ACTUALIZAR' END,N'Venta',
 COALESCE(i.[IdVenta],d.[IdVenta]),GETDATE(),
 LEFT(COALESCE(CONVERT(NVARCHAR(500),SESSION_CONTEXT(N'AccionAuditoria')),N'Operación SQL'),500),
 CASE WHEN d.[IdVenta] IS NOT NULL THEN (SELECT d.[IdVenta],d.[IdCliente],d.[IdCotizacion],d.[IdUsuarioRegistro],d.[IdEstadoVenta],d.[FechaVenta],d.[Subtotal],d.[Igv],d.[Total],d.[Observacion] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CASE WHEN i.[IdVenta] IS NOT NULL THEN (SELECT i.[IdVenta],i.[IdCliente],i.[IdCotizacion],i.[IdUsuarioRegistro],i.[IdEstadoVenta],i.[FechaVenta],i.[Subtotal],i.[Igv],i.[Total],i.[Observacion] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
 CONVERT(NVARCHAR(100),SESSION_CONTEXT(N'SolicitudAuditoria')),
 COALESCE(CONVERT(NVARCHAR(128),SESSION_CONTEXT(N'OrigenAuditoria')),CONCAT(N'SQL: ',ORIGINAL_LOGIN()))
 FROM inserted i FULL OUTER JOIN deleted d ON i.[IdVenta]=d.[IdVenta];
END;

GO