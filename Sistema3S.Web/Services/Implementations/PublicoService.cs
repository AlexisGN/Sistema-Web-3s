using Microsoft.EntityFrameworkCore;
using Sistema3S.Web.Data;
using Sistema3S.Web.DTOs.Publico;
using Sistema3S.Web.Models;
using Sistema3S.Web.Services.Interfaces;

namespace Sistema3S.Web.Services.Implementations
{
    public class PublicoService : IPublicoService
    {
        private readonly Bd3sContext _context;

        public PublicoService(Bd3sContext context)
        {
            _context = context;
        }

        public async Task<InicioPublicoDto> ObtenerInicioAsync()
        {
            var categorias = await ObtenerCategoriasPublicasAsync();
            var marcas = await ObtenerMarcasPublicasAsync();
            var productosNuevos = await ObtenerProductosNuevosAsync();
            var productos = await ObtenerProductosInicioAsync();
            var servicios = await ObtenerServiciosInicioAsync();

            return new InicioPublicoDto
            {
                Categorias = categorias,
                Marcas = marcas,
                ProductosNuevos = productosNuevos,
                Productos = productos,
                Servicios = servicios
            };
        }

        public async Task<NosotrosPublicoDto> ObtenerNosotrosAsync()
        {
            var empresaDb = await _context.Empresa
                .AsNoTracking()
                .Where(e => e.Estado)
                .OrderBy(e => e.IdEmpresa)
                .Select(e => new EmpresaPublicaDto
                {
                    IdEmpresa = e.IdEmpresa,
                    RazonSocial = e.RazonSocial ?? string.Empty,
                    NombreComercial = e.NombreComercial ?? string.Empty,
                    Ruc = e.Ruc ?? string.Empty,
                    Rubro = e.Rubro ?? string.Empty,
                    Telefono = e.Telefono ?? string.Empty,
                    Correo = e.Correo ?? string.Empty,
                    Direccion = e.Direccion ?? string.Empty,
                    SitioWeb = e.SitioWeb ?? string.Empty
                })
                .FirstOrDefaultAsync();

            var idEmpresa = empresaDb?.IdEmpresa;
            var empresa = empresaDb ?? ObtenerEmpresaPublicaDefault();

            var valores = await _context.ValorCorporativo
                .AsNoTracking()
                .Where(v =>
                    v.Estado &&
                    (!idEmpresa.HasValue || v.IdEmpresa == idEmpresa.Value)
                )
                .OrderBy(v => v.IdValor)
                .Select(v => new ValorCorporativoPublicoDto
                {
                    IdValor = v.IdValor,
                    Nombre = v.Nombre,
                    Descripcion = v.Descripcion ?? string.Empty
                })
                .ToListAsync();

            if (valores.Count == 0)
            {
                valores = ObtenerValoresDefault();
            }

            var industrias = await _context.SectorIndustrial
                .AsNoTracking()
                .Where(s => s.Estado)
                .OrderBy(s => s.Nombre)
                .Select(s => new SectorIndustrialPublicoDto
                {
                    IdSectorIndustrial = s.IdSectorIndustrial,
                    Nombre = s.Nombre,
                    Descripcion = s.Descripcion ?? string.Empty
                })
                .ToListAsync();

            if (industrias.Count == 0)
            {
                industrias = ObtenerIndustriasDefault();
            }

            return new NosotrosPublicoDto
            {
                Empresa = empresa,
                Titulo = "Soluciones profesionales para el sector industrial",
                Subtitulo = "Quiénes somos",
                DescripcionPrincipal =
                    "En 3S brindamos productos, servicios y soluciones industriales orientadas a mejorar la continuidad operativa, la seguridad y la eficiencia de empresas que requieren soporte técnico especializado.",

                Mision =
                    "Brindar soluciones industriales confiables mediante productos, servicios técnicos y asesoría especializada, contribuyendo a la eficiencia, seguridad y continuidad operativa de nuestros clientes.",

                Vision =
                    "Ser una empresa reconocida en el sector industrial por la calidad de sus soluciones, la atención técnica especializada y el compromiso con la mejora continua de nuestros clientes.",

                Compromisos = ObtenerCompromisosDefault(),
                Industrias = industrias,
                Valores = valores
            };
        }





        public async Task<List<CategoriaPublicaDto>> ObtenerCategoriasPublicasAsync()
        {
            var categorias = await _context.Categoria
                .AsNoTracking()
                .Where(c => c.Estado)
                .OrderBy(c => c.Nombre)
                .Select(c => new
                {
                    c.IdCategoria,
                    c.Nombre,
                    c.Descripcion,
                    CantidadProductos = c.Producto.Count(p =>
                        p.IdElementoCatalogoNavigation.Estado &&
                        (
                            p.IdMarcaNavigation == null ||
                            p.IdMarcaNavigation.Estado
                        )
                    )
                })
                .ToListAsync();

            return categorias.Select(c => new CategoriaPublicaDto
            {
                Id = c.IdCategoria,
                IdCategoria = c.IdCategoria,
                Nombre = c.Nombre,
                Descripcion = c.Descripcion ?? "Productos y soluciones industriales disponibles para cotización.",
                Icono = ObtenerIconoCategoria(c.Nombre),
                CantidadProductos = c.CantidadProductos
            }).ToList();
        }

        public async Task<List<MarcaPublicaDto>> ObtenerMarcasPublicasAsync()
        {
            return await _context.Marca
                .AsNoTracking()
                .Where(m => m.Estado)
                .OrderBy(m => m.Nombre)
                .Select(m => new MarcaPublicaDto
                {
                    Id = m.IdMarca,
                    IdMarca = m.IdMarca,
                    Nombre = m.Nombre,
                    LogoUrl = m.LogoUrl ?? string.Empty,
                    CantidadProductos = m.Producto.Count(p =>
                        p.IdElementoCatalogoNavigation.Estado &&
                        p.IdCategoriaNavigation.Estado
                    )
                })
                .ToListAsync();
        }

        public async Task<ProductoPublicoListadoDto> ObtenerProductosAsync(
            string? busqueda,
            int? idCategoria,
            int? idMarca,
            int pagina,
            int tamanioPagina)
        {
            pagina = pagina <= 0 ? 1 : pagina;
            tamanioPagina = tamanioPagina <= 0 ? 24 : tamanioPagina;
            tamanioPagina = tamanioPagina > 60 ? 60 : tamanioPagina;

            var query = ConsultaProductosBase();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var texto = busqueda.Trim();

                query = query.Where(p =>
                    EF.Functions.Like(p.IdElementoCatalogoNavigation.Nombre, $"%{texto}%") ||
                    EF.Functions.Like(p.CodigoProducto, $"%{texto}%") ||
                    EF.Functions.Like(p.IdCategoriaNavigation.Nombre, $"%{texto}%") ||
                    (
                        p.IdMarcaNavigation != null &&
                        EF.Functions.Like(p.IdMarcaNavigation.Nombre, $"%{texto}%")
                    )
                );
            }

            if (idCategoria.HasValue && idCategoria.Value > 0)
            {
                query = query.Where(p => p.IdCategoria == idCategoria.Value);
            }

            if (idMarca.HasValue && idMarca.Value > 0)
            {
                query = query.Where(p => p.IdMarca == idMarca.Value);
            }

            var totalRegistros = await query.CountAsync();

            var items = await query
                .OrderBy(p => p.IdCategoriaNavigation.Nombre)
                .ThenBy(p => p.IdElementoCatalogoNavigation.Nombre)
                .Skip((pagina - 1) * tamanioPagina)
                .Take(tamanioPagina)
                .Select(p => new ProductoPublicoDto
                {
                    Id = p.IdProducto,
                    IdProducto = p.IdProducto,
                    IdElementoCatalogo = p.IdElementoCatalogo,
                    IdCategoria = p.IdCategoria,
                    IdMarca = p.IdMarca,

                    Codigo = p.CodigoProducto,
                    Nombre = p.IdElementoCatalogoNavigation.Nombre,
                    Categoria = p.IdCategoriaNavigation.Nombre,
                    Marca = p.IdMarcaNavigation != null ? p.IdMarcaNavigation.Nombre : null,
                    Descripcion = p.IdElementoCatalogoNavigation.Descripcion ?? string.Empty,

                    ImagenUrl =
                        p.IdElementoCatalogoNavigation.ImagenElementoCatalogo
                            .Where(i => i.Estado)
                            .OrderByDescending(i => i.EsPrincipal)
                            .ThenByDescending(i => i.IdImagenElementoCatalogo)
                            .Select(i => i.UrlImagen)
                            .FirstOrDefault()
                        ?? p.IdElementoCatalogoNavigation.ImagenUrl
                        ?? string.Empty,

                    Nuevo = false,
                    TieneFichaTecnica = p.FichaTecnicaPdf != null && p.FichaTecnicaPdf != "",
                    FichaTecnicaPdf = p.FichaTecnicaPdf
                })
                .ToListAsync();

            var totalPaginas = totalRegistros == 0
                ? 0
                : (int)Math.Ceiling(totalRegistros / (double)tamanioPagina);

            return new ProductoPublicoListadoDto
            {
                Items = items,
                TotalRegistros = totalRegistros,
                Pagina = pagina,
                TamanioPagina = tamanioPagina,
                TotalPaginas = totalPaginas,
                HayMas = pagina < totalPaginas
            };
        }

        public async Task<ProductoDetallePublicoDto?> ObtenerProductoDetalleAsync(int idProducto)
        {
            return await ConsultaProductosBase()
                .Where(p => p.IdProducto == idProducto)
                .Select(p => new ProductoDetallePublicoDto
                {
                    Id = p.IdProducto,
                    IdProducto = p.IdProducto,
                    IdElementoCatalogo = p.IdElementoCatalogo,
                    IdCategoria = p.IdCategoria,
                    IdMarca = p.IdMarca,

                    Codigo = p.CodigoProducto,
                    Nombre = p.IdElementoCatalogoNavigation.Nombre,
                    Categoria = p.IdCategoriaNavigation.Nombre,
                    Marca = p.IdMarcaNavigation != null ? p.IdMarcaNavigation.Nombre : null,
                    Descripcion = p.IdElementoCatalogoNavigation.Descripcion ?? string.Empty,

                    ImagenUrl =
                        p.IdElementoCatalogoNavigation.ImagenElementoCatalogo
                            .Where(i => i.Estado)
                            .OrderByDescending(i => i.EsPrincipal)
                            .ThenByDescending(i => i.IdImagenElementoCatalogo)
                            .Select(i => i.UrlImagen)
                            .FirstOrDefault()
                        ?? p.IdElementoCatalogoNavigation.ImagenUrl
                        ?? string.Empty,

                    Nuevo = false,
                    TieneFichaTecnica = p.FichaTecnicaPdf != null && p.FichaTecnicaPdf != "",
                    FichaTecnicaPdf = p.FichaTecnicaPdf,

                    Imagenes = p.IdElementoCatalogoNavigation.ImagenElementoCatalogo
                        .Where(i => i.Estado)
                        .OrderByDescending(i => i.EsPrincipal)
                        .ThenByDescending(i => i.IdImagenElementoCatalogo)
                        .Select(i => new ImagenPublicaDto
                        {
                            IdImagen = i.IdImagenElementoCatalogo,
                            UrlImagen = i.UrlImagen,
                            TextoAlternativo = i.TextoAlternativo ?? p.IdElementoCatalogoNavigation.Nombre,
                            EsPrincipal = i.EsPrincipal
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<List<ServicioPublicoDto>> ObtenerServiciosPublicosAsync()
        {
            return await ConsultaServiciosBase()
                .OrderBy(s => s.IdElementoCatalogoNavigation.Nombre)
                .Select(s => new ServicioPublicoDto
                {
                    Id = s.IdServicio,
                    IdServicio = s.IdServicio,
                    IdElementoCatalogo = s.IdElementoCatalogo,

                    Nombre = s.IdElementoCatalogoNavigation.Nombre,
                    Descripcion = s.IdElementoCatalogoNavigation.Descripcion ?? string.Empty,
                    SectorAplicacion = s.SectorAplicacion,
                    MensajeWhatsApp = s.MensajeWhatsApp,
                    RequiereVisitaTecnica = s.RequiereVisitaTecnica,

                    ImagenUrl =
                        s.IdElementoCatalogoNavigation.ImagenElementoCatalogo
                            .Where(i => i.Estado)
                            .OrderByDescending(i => i.EsPrincipal)
                            .ThenByDescending(i => i.IdImagenElementoCatalogo)
                            .Select(i => i.UrlImagen)
                            .FirstOrDefault()
                        ?? s.IdElementoCatalogoNavigation.ImagenUrl
                        ?? string.Empty
                })
                .ToListAsync();
        }

        public async Task<ServicioDetallePublicoDto?> ObtenerServicioDetalleAsync(int idServicio)
        {
            return await ConsultaServiciosBase()
                .Where(s => s.IdServicio == idServicio)
                .Select(s => new ServicioDetallePublicoDto
                {
                    Id = s.IdServicio,
                    IdServicio = s.IdServicio,
                    IdElementoCatalogo = s.IdElementoCatalogo,

                    Nombre = s.IdElementoCatalogoNavigation.Nombre,
                    Descripcion = s.IdElementoCatalogoNavigation.Descripcion ?? string.Empty,
                    SectorAplicacion = s.SectorAplicacion,
                    MensajeWhatsApp = s.MensajeWhatsApp,
                    RequiereVisitaTecnica = s.RequiereVisitaTecnica,

                    ImagenUrl =
                        s.IdElementoCatalogoNavigation.ImagenElementoCatalogo
                            .Where(i => i.Estado)
                            .OrderByDescending(i => i.EsPrincipal)
                            .ThenByDescending(i => i.IdImagenElementoCatalogo)
                            .Select(i => i.UrlImagen)
                            .FirstOrDefault()
                        ?? s.IdElementoCatalogoNavigation.ImagenUrl
                        ?? string.Empty,

                    Imagenes = s.IdElementoCatalogoNavigation.ImagenElementoCatalogo
                        .Where(i => i.Estado)
                        .OrderByDescending(i => i.EsPrincipal)
                        .ThenByDescending(i => i.IdImagenElementoCatalogo)
                        .Select(i => new ImagenPublicaDto
                        {
                            IdImagen = i.IdImagenElementoCatalogo,
                            UrlImagen = i.UrlImagen,
                            TextoAlternativo = i.TextoAlternativo ?? s.IdElementoCatalogoNavigation.Nombre,
                            EsPrincipal = i.EsPrincipal
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }
        public async Task<BusquedaPublicaDto> BuscarAsync(string? busqueda, int limitePorTipo = 8)
        {
            var texto = (busqueda ?? string.Empty).Trim();

            limitePorTipo = limitePorTipo <= 0 ? 8 : limitePorTipo;
            limitePorTipo = limitePorTipo > 12 ? 12 : limitePorTipo;

            if (string.IsNullOrWhiteSpace(texto))
            {
                return new BusquedaPublicaDto
                {
                    Query = string.Empty,
                    Productos = new List<ProductoPublicoDto>(),
                    Servicios = new List<ServicioPublicoDto>(),
                    Categorias = new List<CategoriaPublicaDto>(),
                    Marcas = new List<MarcaPublicaDto>(),
                    TotalResultados = 0
                };
            }

            var productos = await ConsultaProductosBase()
                .Where(p =>
                    EF.Functions.Like(p.IdElementoCatalogoNavigation.Nombre, $"%{texto}%") ||
                    EF.Functions.Like(p.CodigoProducto, $"%{texto}%") ||
                    EF.Functions.Like(p.IdCategoriaNavigation.Nombre, $"%{texto}%") ||
                    (
                        p.IdMarcaNavigation != null &&
                        EF.Functions.Like(p.IdMarcaNavigation.Nombre, $"%{texto}%")
                    ) ||
                    (
                        p.IdElementoCatalogoNavigation.Descripcion != null &&
                        EF.Functions.Like(p.IdElementoCatalogoNavigation.Descripcion, $"%{texto}%")
                    )
                )
                .OrderBy(p => p.IdElementoCatalogoNavigation.Nombre)
                .Take(limitePorTipo)
                .Select(p => new ProductoPublicoDto
                {
                    Id = p.IdProducto,
                    IdProducto = p.IdProducto,
                    IdElementoCatalogo = p.IdElementoCatalogo,
                    IdCategoria = p.IdCategoria,
                    IdMarca = p.IdMarca,

                    Codigo = p.CodigoProducto,
                    Nombre = p.IdElementoCatalogoNavigation.Nombre,
                    Categoria = p.IdCategoriaNavigation.Nombre,
                    Marca = p.IdMarcaNavigation != null ? p.IdMarcaNavigation.Nombre : null,
                    Descripcion = p.IdElementoCatalogoNavigation.Descripcion ?? string.Empty,

                    ImagenUrl =
                        p.IdElementoCatalogoNavigation.ImagenElementoCatalogo
                            .Where(i => i.Estado)
                            .OrderByDescending(i => i.EsPrincipal)
                            .ThenByDescending(i => i.IdImagenElementoCatalogo)
                            .Select(i => i.UrlImagen)
                            .FirstOrDefault()
                        ?? p.IdElementoCatalogoNavigation.ImagenUrl
                        ?? string.Empty,

                    Nuevo = false,
                    TieneFichaTecnica = p.FichaTecnicaPdf != null && p.FichaTecnicaPdf != "",
                    FichaTecnicaPdf = p.FichaTecnicaPdf
                })
                .ToListAsync();

            var servicios = await ConsultaServiciosBase()
                .Where(s =>
                    EF.Functions.Like(s.IdElementoCatalogoNavigation.Nombre, $"%{texto}%") ||
                    (
                        s.IdElementoCatalogoNavigation.Descripcion != null &&
                        EF.Functions.Like(s.IdElementoCatalogoNavigation.Descripcion, $"%{texto}%")
                    ) ||
                    (
                        s.SectorAplicacion != null &&
                        EF.Functions.Like(s.SectorAplicacion, $"%{texto}%")
                    ) ||
                    (
                        s.MensajeWhatsApp != null &&
                        EF.Functions.Like(s.MensajeWhatsApp, $"%{texto}%")
                    )
                )
                .OrderBy(s => s.IdElementoCatalogoNavigation.Nombre)
                .Take(limitePorTipo)
                .Select(s => new ServicioPublicoDto
                {
                    Id = s.IdServicio,
                    IdServicio = s.IdServicio,
                    IdElementoCatalogo = s.IdElementoCatalogo,

                    Nombre = s.IdElementoCatalogoNavigation.Nombre,
                    Descripcion = s.IdElementoCatalogoNavigation.Descripcion ?? string.Empty,
                    SectorAplicacion = s.SectorAplicacion,
                    MensajeWhatsApp = s.MensajeWhatsApp,
                    RequiereVisitaTecnica = s.RequiereVisitaTecnica,

                    ImagenUrl =
                        s.IdElementoCatalogoNavigation.ImagenElementoCatalogo
                            .Where(i => i.Estado)
                            .OrderByDescending(i => i.EsPrincipal)
                            .ThenByDescending(i => i.IdImagenElementoCatalogo)
                            .Select(i => i.UrlImagen)
                            .FirstOrDefault()
                        ?? s.IdElementoCatalogoNavigation.ImagenUrl
                        ?? string.Empty
                })
                .ToListAsync();

            var categoriasRaw = await _context.Categoria
                .AsNoTracking()
                .Where(c =>
                    c.Estado &&
                    (
                        EF.Functions.Like(c.Nombre, $"%{texto}%") ||
                        (
                            c.Descripcion != null &&
                            EF.Functions.Like(c.Descripcion, $"%{texto}%")
                        )
                    )
                )
                .OrderBy(c => c.Nombre)
                .Take(limitePorTipo)
                .Select(c => new
                {
                    c.IdCategoria,
                    c.Nombre,
                    c.Descripcion,
                    CantidadProductos = c.Producto.Count(p =>
                        p.IdElementoCatalogoNavigation.Estado &&
                        (
                            p.IdMarcaNavigation == null ||
                            p.IdMarcaNavigation.Estado
                        )
                    )
                })
                .ToListAsync();

            var categorias = categoriasRaw.Select(c => new CategoriaPublicaDto
            {
                Id = c.IdCategoria,
                IdCategoria = c.IdCategoria,
                Nombre = c.Nombre,
                Descripcion = c.Descripcion ?? "Productos y soluciones industriales disponibles para cotización.",
                Icono = ObtenerIconoCategoria(c.Nombre),
                CantidadProductos = c.CantidadProductos
            }).ToList();

            var marcas = await _context.Marca
                .AsNoTracking()
                .Where(m =>
                    m.Estado &&
                    EF.Functions.Like(m.Nombre, $"%{texto}%")
                )
                .OrderBy(m => m.Nombre)
                .Take(limitePorTipo)
                .Select(m => new MarcaPublicaDto
                {
                    Id = m.IdMarca,
                    IdMarca = m.IdMarca,
                    Nombre = m.Nombre,
                    LogoUrl = m.LogoUrl ?? string.Empty,
                    CantidadProductos = m.Producto.Count(p =>
                        p.IdElementoCatalogoNavigation.Estado &&
                        p.IdCategoriaNavigation.Estado
                    )
                })
                .ToListAsync();

            return new BusquedaPublicaDto
            {
                Query = texto,
                Productos = productos,
                Servicios = servicios,
                Categorias = categorias,
                Marcas = marcas,
                TotalResultados = productos.Count + servicios.Count + categorias.Count + marcas.Count
            };
        }

        private async Task<List<ProductoPublicoDto>> ObtenerProductosNuevosAsync()
        {
            return await ConsultaProductosBase()
                .OrderByDescending(p => p.IdElementoCatalogoNavigation.FechaRegistro)
                .ThenByDescending(p => p.IdProducto)
                .Take(8)
                .Select(p => new ProductoPublicoDto
                {
                    Id = p.IdProducto,
                    IdProducto = p.IdProducto,
                    IdElementoCatalogo = p.IdElementoCatalogo,
                    IdCategoria = p.IdCategoria,
                    IdMarca = p.IdMarca,

                    Codigo = p.CodigoProducto,
                    Nombre = p.IdElementoCatalogoNavigation.Nombre,
                    Categoria = p.IdCategoriaNavigation.Nombre,
                    Marca = p.IdMarcaNavigation != null ? p.IdMarcaNavigation.Nombre : null,
                    Descripcion = p.IdElementoCatalogoNavigation.Descripcion ?? string.Empty,

                    ImagenUrl =
                        p.IdElementoCatalogoNavigation.ImagenElementoCatalogo
                            .Where(i => i.Estado)
                            .OrderByDescending(i => i.EsPrincipal)
                            .ThenByDescending(i => i.IdImagenElementoCatalogo)
                            .Select(i => i.UrlImagen)
                            .FirstOrDefault()
                        ?? p.IdElementoCatalogoNavigation.ImagenUrl
                        ?? string.Empty,

                    Nuevo = true,
                    TieneFichaTecnica = p.FichaTecnicaPdf != null && p.FichaTecnicaPdf != "",
                    FichaTecnicaPdf = p.FichaTecnicaPdf
                })
                .ToListAsync();
        }

        private async Task<List<ProductoPublicoDto>> ObtenerProductosInicioAsync()
        {
            return await ConsultaProductosBase()
                .OrderBy(p => p.IdCategoriaNavigation.Nombre)
                .ThenByDescending(p => p.IdElementoCatalogoNavigation.FechaRegistro)
                .ThenByDescending(p => p.IdProducto)
                .Take(80)
                .Select(p => new ProductoPublicoDto
                {
                    Id = p.IdProducto,
                    IdProducto = p.IdProducto,
                    IdElementoCatalogo = p.IdElementoCatalogo,
                    IdCategoria = p.IdCategoria,
                    IdMarca = p.IdMarca,

                    Codigo = p.CodigoProducto,
                    Nombre = p.IdElementoCatalogoNavigation.Nombre,
                    Categoria = p.IdCategoriaNavigation.Nombre,
                    Marca = p.IdMarcaNavigation != null ? p.IdMarcaNavigation.Nombre : null,
                    Descripcion = p.IdElementoCatalogoNavigation.Descripcion ?? string.Empty,

                    ImagenUrl =
                        p.IdElementoCatalogoNavigation.ImagenElementoCatalogo
                            .Where(i => i.Estado)
                            .OrderByDescending(i => i.EsPrincipal)
                            .ThenByDescending(i => i.IdImagenElementoCatalogo)
                            .Select(i => i.UrlImagen)
                            .FirstOrDefault()
                        ?? p.IdElementoCatalogoNavigation.ImagenUrl
                        ?? string.Empty,

                    Nuevo = false,
                    TieneFichaTecnica = p.FichaTecnicaPdf != null && p.FichaTecnicaPdf != "",
                    FichaTecnicaPdf = p.FichaTecnicaPdf
                })
                .ToListAsync();
        }

        private async Task<List<ServicioPublicoDto>> ObtenerServiciosInicioAsync()
        {
            return await ConsultaServiciosBase()
                .OrderByDescending(s => s.IdElementoCatalogoNavigation.FechaRegistro)
                .ThenByDescending(s => s.IdServicio)
                .Take(8)
                .Select(s => new ServicioPublicoDto
                {
                    Id = s.IdServicio,
                    IdServicio = s.IdServicio,
                    IdElementoCatalogo = s.IdElementoCatalogo,

                    Nombre = s.IdElementoCatalogoNavigation.Nombre,
                    Descripcion = s.IdElementoCatalogoNavigation.Descripcion ?? string.Empty,
                    SectorAplicacion = s.SectorAplicacion,
                    MensajeWhatsApp = s.MensajeWhatsApp,
                    RequiereVisitaTecnica = s.RequiereVisitaTecnica,

                    ImagenUrl =
                        s.IdElementoCatalogoNavigation.ImagenElementoCatalogo
                            .Where(i => i.Estado)
                            .OrderByDescending(i => i.EsPrincipal)
                            .ThenByDescending(i => i.IdImagenElementoCatalogo)
                            .Select(i => i.UrlImagen)
                            .FirstOrDefault()
                        ?? s.IdElementoCatalogoNavigation.ImagenUrl
                        ?? string.Empty
                })
                .ToListAsync();
        }

        private IQueryable<Producto> ConsultaProductosBase()
        {
            return _context.Producto
                .AsNoTracking()
                .Where(p =>
                    p.IdElementoCatalogoNavigation.Estado &&
                    p.IdCategoriaNavigation.Estado &&
                    (
                        p.IdMarcaNavigation == null ||
                        p.IdMarcaNavigation.Estado
                    )
                );
        }

        private IQueryable<Servicio> ConsultaServiciosBase()
        {
            return _context.Servicio
                .AsNoTracking()
                .Where(s => s.IdElementoCatalogoNavigation.Estado);
        }

        private static string ObtenerIconoCategoria(string nombre)
        {
            var texto = nombre.ToLower();

            if (texto.Contains("vapor") || texto.Contains("caldera"))
            {
                return "♨";
            }

            if (texto.Contains("instrument") || texto.Contains("automat"))
            {
                return "⚙";
            }

            if (texto.Contains("mantenimiento") || texto.Contains("herramient"))
            {
                return "🛠";
            }

            if (texto.Contains("repuesto") || texto.Contains("terminal") || texto.Contains("eléctr"))
            {
                return "🔩";
            }

            return "🏭";
        }
        private EmpresaPublicaDto ObtenerEmpresaPublicaDefault()
        {
            return new EmpresaPublicaDto
            {
                IdEmpresa = 0,
                RazonSocial = "Servicio y Soluciones Superiores S.A.C.",
                NombreComercial = "3S",
                Ruc = string.Empty,
                Rubro = "Soluciones industriales",
                Telefono = "+51 948 327 667",
                Correo = "cevallosindustrial@gmail.com",
                Direccion = "Av. Los Pinos 960 Urb. El Ermitaño, Independencia - Lima - Lima",
                SitioWeb = string.Empty
            };
        }

        private List<CompromisoPublicoDto> ObtenerCompromisosDefault()
        {
            return new List<CompromisoPublicoDto>
    {
        new CompromisoPublicoDto
        {
            Icono = "⚙",
            Titulo = "Eficiencia operativa",
            Descripcion = "Orientamos nuestras soluciones a reducir paradas, mejorar procesos y fortalecer la continuidad operativa."
        },
        new CompromisoPublicoDto
        {
            Icono = "🛠",
            Titulo = "Soporte técnico especializado",
            Descripcion = "Acompañamos a nuestros clientes con atención técnica, asesoría y soluciones ajustadas a sus necesidades industriales."
        },
        new CompromisoPublicoDto
        {
            Icono = "🛡",
            Titulo = "Seguridad y confiabilidad",
            Descripcion = "Priorizamos soluciones seguras, confiables y alineadas a las exigencias del entorno industrial."
        }
    };
        }

        private List<SectorIndustrialPublicoDto> ObtenerIndustriasDefault()
        {
            return new List<SectorIndustrialPublicoDto>
    {
        new SectorIndustrialPublicoDto
        {
            IdSectorIndustrial = 0,
            Nombre = "Minería",
            Descripcion = "Soluciones para operación, mantenimiento e instrumentación industrial."
        },
        new SectorIndustrialPublicoDto
        {
            IdSectorIndustrial = 0,
            Nombre = "Agroindustria",
            Descripcion = "Soporte técnico para procesos productivos y continuidad operativa."
        },
        new SectorIndustrialPublicoDto
        {
            IdSectorIndustrial = 0,
            Nombre = "Industria textil",
            Descripcion = "Atención técnica para sistemas industriales, vapor y automatización."
        },
        new SectorIndustrialPublicoDto
        {
            IdSectorIndustrial = 0,
            Nombre = "Industria química",
            Descripcion = "Soluciones para control, seguridad y procesos industriales."
        },
        new SectorIndustrialPublicoDto
        {
            IdSectorIndustrial = 0,
            Nombre = "Industria papelera",
            Descripcion = "Productos y servicios para mantenimiento y operación industrial."
        }
    };
        }

        private List<ValorCorporativoPublicoDto> ObtenerValoresDefault()
        {
            return new List<ValorCorporativoPublicoDto>
    {
        new ValorCorporativoPublicoDto
        {
            IdValor = 0,
            Nombre = "Responsabilidad",
            Descripcion = "Cumplimos nuestros compromisos con seriedad, orden y orientación al cliente."
        },
        new ValorCorporativoPublicoDto
        {
            IdValor = 0,
            Nombre = "Calidad",
            Descripcion = "Buscamos entregar soluciones confiables que respondan a las necesidades del sector industrial."
        },
        new ValorCorporativoPublicoDto
        {
            IdValor = 0,
            Nombre = "Compromiso",
            Descripcion = "Trabajamos con enfoque técnico y mejora continua para aportar valor a cada cliente."
        }
    };
        }
    }
}