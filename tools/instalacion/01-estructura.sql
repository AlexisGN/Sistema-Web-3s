-- Instalación nueva: sólo estructura. Ejecutar en una BD_3S nueva y vacía.
IF EXISTS(SELECT 1 FROM sys.tables WHERE is_ms_shipped=0) THROW 79001,N'La instalación exige una base nueva y vacía.',1;
GO
SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON;
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
