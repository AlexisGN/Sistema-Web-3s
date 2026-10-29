-- Ampliación aditiva; sólo relaciona entidades sintéticas con el catálogo operativo existente.
SET XACT_ABORT ON;
IF COL_LENGTH('bi.EntidadReferencia','IdClienteOrigen') IS NULL
 ALTER TABLE bi.EntidadReferencia ADD IdClienteOrigen int NULL;
IF COL_LENGTH('bi.EntidadReferencia','IdProveedorOrigen') IS NULL
 ALTER TABLE bi.EntidadReferencia ADD IdProveedorOrigen int NULL;
GO
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_BI_Referencia_ClienteOrigen')
 ALTER TABLE bi.EntidadReferencia WITH CHECK ADD CONSTRAINT FK_BI_Referencia_ClienteOrigen FOREIGN KEY(IdClienteOrigen) REFERENCES dbo.Cliente(IdCliente);
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_BI_Referencia_ProveedorOrigen')
 ALTER TABLE bi.EntidadReferencia WITH CHECK ADD CONSTRAINT FK_BI_Referencia_ProveedorOrigen FOREIGN KEY(IdProveedorOrigen) REFERENCES dbo.Proveedor(IdProveedor);
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_BI_Referencia_TerceroOrigen')
 ALTER TABLE bi.EntidadReferencia WITH CHECK ADD CONSTRAINT CK_BI_Referencia_TerceroOrigen CHECK((Tipo='cliente' AND IdProveedorOrigen IS NULL) OR (Tipo='proveedor' AND IdClienteOrigen IS NULL));
