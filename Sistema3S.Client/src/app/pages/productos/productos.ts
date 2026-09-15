import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';

import { ProductoService } from '../../core/services/producto';
import { CatalogoService } from '../../core/services/catalogo';
import {
  ProductoActualizar,
  ProductoCrear,
  ProductoListado
} from '../../core/models/producto.model';
import { CatalogoItem } from '../../core/models/catalogo-item.model';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-productos',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './productos.html',
  styleUrl: './productos.scss'
})
export class ProductosComponent implements OnInit {
  productos: ProductoListado[] = [];

  categorias: CatalogoItem[] = [];
  marcas: CatalogoItem[] = [];
  unidades: CatalogoItem[] = [];

  cargando = false;
  cargandoCombos = false;
  guardando = false;

  buscar = '';
  verInactivos = false;
  mensaje = '';
  error = '';

  erroresCampo: Record<string, string> = {};

  pagina = 1;
  tamanioPagina = 5;
  totalRegistros = 0;
  totalPaginas = 0;
  opcionesTamanioPagina = [5, 10, 20];

  editando = false;
  idProductoEditando: number | null = null;
  estadoProductoEditando = true;

  producto: ProductoCrear = this.nuevoProducto();
  imagenArchivo: File | null = null;
  fichaTecnicaArchivo: File | null = null;
  imagenVistaPrevia = '';

  private readonly baseArchivosUrl = environment.apiUrl.replace(/\/api\/?$/, '');

  constructor(
    private productoService: ProductoService,
    private catalogoService: CatalogoService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.subirArriba();
    this.cargarCombos();
    this.cargarProductos();
  }

  seleccionarImagen(event: Event): void {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0] ?? null;

    if (!archivo) {
      return;
    }

    const tiposPermitidos = ['image/jpeg', 'image/png', 'image/webp'];

    if (!tiposPermitidos.includes(archivo.type)) {
      input.value = '';
      this.imagenArchivo = null;
      this.imagenVistaPrevia = '';
      this.erroresCampo['imagenUrl'] = 'Selecciona una imagen JPG, PNG o WebP.';
      return;
    }

    if (archivo.size > 5 * 1024 * 1024) {
      input.value = '';
      this.imagenArchivo = null;
      this.imagenVistaPrevia = '';
      this.erroresCampo['imagenUrl'] = 'La imagen no puede superar 5 MB.';
      return;
    }

    this.imagenArchivo = archivo;
    delete this.erroresCampo['imagenUrl'];

    const reader = new FileReader();
    reader.onload = () => {
      this.imagenVistaPrevia = typeof reader.result === 'string' ? reader.result : '';
      this.cdr.detectChanges();
    };
    reader.readAsDataURL(archivo);
  }

  seleccionarFichaTecnica(event: Event): void {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0] ?? null;

    if (!archivo) {
      return;
    }

    const extensionValida = archivo.name.toLowerCase().endsWith('.pdf');

    if (archivo.type !== 'application/pdf' || !extensionValida) {
      input.value = '';
      this.fichaTecnicaArchivo = null;
      this.erroresCampo['fichaTecnicaPdf'] = 'Selecciona un archivo PDF válido.';
      return;
    }

    if (archivo.size > 15 * 1024 * 1024) {
      input.value = '';
      this.fichaTecnicaArchivo = null;
      this.erroresCampo['fichaTecnicaPdf'] = 'El PDF no puede superar 15 MB.';
      return;
    }

    this.fichaTecnicaArchivo = archivo;
    delete this.erroresCampo['fichaTecnicaPdf'];
  }

  quitarImagenSeleccionada(input: HTMLInputElement): void {
    input.value = '';
    this.imagenArchivo = null;
    this.imagenVistaPrevia = '';
  }

  quitarFichaSeleccionada(input: HTMLInputElement): void {
    input.value = '';
    this.fichaTecnicaArchivo = null;
  }

  obtenerImagenFormulario(): string {
    return this.imagenVistaPrevia || this.resolverImagen(this.producto.imagenUrl);
  }

  abrirFichaTecnicaActual(): void {
    if (!this.producto.fichaTecnicaPdf) {
      return;
    }

    window.open(this.resolverArchivo(this.producto.fichaTecnicaPdf), '_blank', 'noopener');
  }

  cargarCombos(): void {
    this.cargandoCombos = true;
    this.error = '';

    forkJoin({
      categorias: this.catalogoService.listarCategorias(),
      marcas: this.catalogoService.listarMarcas(),
      unidades: this.catalogoService.listarUnidadesMedida()
    }).subscribe({
      next: (data) => {
        this.categorias = data.categorias;
        this.marcas = data.marcas;
        this.unidades = data.unidades;

        this.producto.idCategoria = this.categorias[0]?.id ?? 0;
        this.producto.idMarca = this.marcas[0]?.id ?? null;
        this.producto.idUnidadMedida = this.unidades[0]?.id ?? null;

        this.cargandoCombos = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error cargando combos:', err);
        this.error = 'No se pudieron cargar categorías, marcas o unidades.';
        this.cargandoCombos = false;
        this.cdr.detectChanges();
      }
    });
  }

  cargarProductos(): void {
    this.cargando = true;
    this.error = '';

    this.productoService.listar(this.buscar, this.pagina, this.tamanioPagina, this.verInactivos ? false : undefined).subscribe({
      next: (data) => {
        this.productos = data.items;
        this.pagina = data.pagina;
        this.tamanioPagina = data.tamanioPagina;
        this.totalRegistros = data.totalRegistros;
        this.totalPaginas = data.totalPaginas;

        if (this.productos.length === 0 && this.totalRegistros > 0 && this.pagina > 1) {
          this.pagina--;
          this.cargando = false;
          this.cargarProductos();
          return;
        }

        this.cargando = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error cargando productos:', err);
        this.error = 'No se pudieron cargar los productos.';
        this.cargando = false;
        this.cdr.detectChanges();
      }
    });
  }

  guardarProducto(): void {
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    this.producto.nombre = (this.producto.nombre ?? '').trim();
    this.producto.codigoProducto = (this.producto.codigoProducto ?? '').trim().toUpperCase();

    this.producto.descripcion = this.normalizarTexto(this.producto.descripcion);
    this.producto.imagenUrl = this.normalizarTexto(this.producto.imagenUrl);
    this.producto.fichaTecnicaPdf = this.normalizarTexto(this.producto.fichaTecnicaPdf);

    this.producto.precioReferencial = this.obtenerNumero(this.producto.precioReferencial);
    this.producto.stockInicial = this.obtenerNumero(this.producto.stockInicial) ?? 0;
    this.producto.stockMinimo = this.obtenerNumero(this.producto.stockMinimo) ?? 0;

    const errorValidacion = this.validarFormulario();

    if (errorValidacion) {
      this.error = '';
      this.cdr.detectChanges();
      return;
    }

    if (this.editando) {
      this.actualizarProducto();
    } else {
      this.registrarProducto();
    }
  }

  registrarProducto(): void {
    this.guardando = true;

    this.productoService.crear(
      this.producto,
      this.imagenArchivo!,
      this.fichaTecnicaArchivo
    ).subscribe({
      next: () => {
        const mensajeOk = 'Producto registrado correctamente.';

        this.guardando = false;
        this.limpiarFormulario();
        this.mensaje = mensajeOk;
        this.pagina = 1;
        this.cargarProductos();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = err.error?.mensaje ?? 'No se pudo registrar el producto.';
        this.guardando = false;
        this.cdr.detectChanges();
      }
    });
  }

  actualizarProducto(): void {
    if (this.idProductoEditando === null) {
      this.error = 'No se encontró el producto a editar.';
      return;
    }

    const productoActualizar: ProductoActualizar = {
      nombre: (this.producto.nombre ?? '').trim(),
      descripcion: this.normalizarTexto(this.producto.descripcion),
      precioReferencial: this.obtenerNumero(this.producto.precioReferencial),
      imagenUrl: this.normalizarTexto(this.producto.imagenUrl),
      idCategoria: this.producto.idCategoria,
      idMarca: this.producto.idMarca,
      idUnidadMedida: this.producto.idUnidadMedida,
      codigoProducto: (this.producto.codigoProducto ?? '').trim().toUpperCase(),
      fichaTecnicaPdf: this.normalizarTexto(this.producto.fichaTecnicaPdf),
      stockMinimo: this.obtenerNumero(this.producto.stockMinimo) ?? 0,
      estado: this.estadoProductoEditando
    };

    this.guardando = true;

    this.productoService.actualizar(
      this.idProductoEditando,
      productoActualizar,
      this.imagenArchivo,
      this.fichaTecnicaArchivo
    ).subscribe({
      next: () => {
        const mensajeOk = 'Producto actualizado correctamente.';

        this.guardando = false;
        this.limpiarFormulario();
        this.mensaje = mensajeOk;
        this.cargarProductos();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = err.error?.mensaje ?? 'No se pudo actualizar el producto.';
        this.guardando = false;
        this.cdr.detectChanges();
      }
    });
  }

  editarProducto(item: ProductoListado): void {
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    this.editando = true;
    this.idProductoEditando = item.idProducto;

    // Al editar un producto inactivo, al actualizar volverá a estar activo
    this.estadoProductoEditando = true;
    this.imagenArchivo = null;
    this.fichaTecnicaArchivo = null;
    this.imagenVistaPrevia = '';

    this.producto = {
      nombre: item.nombre,
      descripcion: item.descripcion ?? '',
      precioReferencial: item.precioReferencial ?? null,
      imagenUrl: item.imagenUrl ?? '',
      idCategoria: item.idCategoria ?? 0,
      idMarca: item.idMarca ?? null,
      idUnidadMedida: item.idUnidadMedida ?? null,
      codigoProducto: item.codigoProducto,
      fichaTecnicaPdf: item.fichaTecnicaPdf ?? null,
      stockInicial: item.stockActual,
      stockMinimo: item.stockMinimo
    };

    window.scrollTo({
      top: 0,
      behavior: 'smooth'
    });

    this.cdr.detectChanges();
  }

  eliminarProducto(idProducto: number): void {
    const confirmar = confirm('¿Deseas eliminar este producto?');

    if (!confirmar) {
      return;
    }

    this.productoService.eliminar(idProducto).subscribe({
      next: () => {
        this.mensaje = 'Producto eliminado correctamente.';
        this.cargarProductos();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = 'No se pudo eliminar el producto.';
        this.cdr.detectChanges();
      }
    });
  }

  limpiarFormulario(): void {
    this.producto = this.nuevoProducto();
    this.imagenArchivo = null;
    this.fichaTecnicaArchivo = null;
    this.imagenVistaPrevia = '';

    this.producto.idCategoria = this.categorias[0]?.id ?? 0;
    this.producto.idMarca = this.marcas[0]?.id ?? null;
    this.producto.idUnidadMedida = this.unidades[0]?.id ?? null;

    this.editando = false;
    this.idProductoEditando = null;
    this.estadoProductoEditando = true;

    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    this.cdr.detectChanges();
  }

  resolverImagen(url?: string | null): string {
    if (!url || url.trim().length === 0) {
      return '';
    }

    const ruta = url.trim();

    return this.resolverArchivo(ruta);
  }

  marcarImagenFallida(item: ProductoListado): void {
    item.imagenUrl = '';
    this.cdr.detectChanges();
  }

  alternarInactivos(): void {
    this.verInactivos = !this.verInactivos;
    this.pagina = 1;
    this.cargarProductos();
  }

  buscarProductos(): void {
    this.pagina = 1;
    this.cargarProductos();
  }

  paginaAnterior(): void {
    if (this.pagina <= 1) {
      return;
    }

    this.pagina--;
    this.cargarProductos();
  }

  paginaSiguiente(): void {
    if (this.pagina >= this.totalPaginas) {
      return;
    }

    this.pagina++;
    this.cargarProductos();
  }

  cambiarTamanioPagina(): void {
    this.pagina = 1;
    this.cargarProductos();
  }

  formularioProductoValido(): boolean {
    const precio = this.obtenerNumero(this.producto.precioReferencial);
    const stockInicial = this.obtenerNumero(this.producto.stockInicial);
    const stockMinimo = this.obtenerNumero(this.producto.stockMinimo);

    const stockValido = this.editando
      ? stockMinimo !== null && stockMinimo >= 0
      : stockInicial !== null &&
        stockMinimo !== null &&
        stockInicial >= 0 &&
        stockMinimo >= 0 &&
        stockInicial > stockMinimo;

    return (
      this.campoTextoLleno(this.producto.nombre) &&
      this.campoTextoLleno(this.producto.codigoProducto) &&
      precio !== null &&
      precio >= 0 &&
      this.campoTextoLleno(this.producto.descripcion) &&
      !!this.producto.idCategoria &&
      this.producto.idCategoria > 0 &&
      !!this.producto.idUnidadMedida &&
      this.producto.idUnidadMedida > 0 &&
      (this.imagenArchivo !== null || this.campoTextoLleno(this.producto.imagenUrl)) &&
      stockValido
    );
  }

  limpiarErrorCampo(campo: string): void {
    if (!this.erroresCampo[campo]) {
      return;
    }

    const mensajeActual = this.obtenerMensajeErrorCampo(campo);

    if (mensajeActual) {
      this.erroresCampo[campo] = mensajeActual;
      return;
    }

    delete this.erroresCampo[campo];
  }

  private validarFormulario(): string | null {
    const precio = this.obtenerNumero(this.producto.precioReferencial);
    const stockInicial = this.obtenerNumero(this.producto.stockInicial);
    const stockMinimo = this.obtenerNumero(this.producto.stockMinimo);

    if (!this.campoTextoLleno(this.producto.nombre)) {
      return this.marcarErrorCampo('nombre', 'Ingresa el nombre del producto.');
    }

    if (!this.campoTextoLleno(this.producto.codigoProducto)) {
      return this.marcarErrorCampo('codigoProducto', 'Ingresa el código del producto.');
    }

    if (precio === null) {
      return this.marcarErrorCampo('precioReferencial', 'Ingresa el precio referencial.');
    }

    if (precio < 0) {
      return this.marcarErrorCampo('precioReferencial', 'El precio referencial no puede ser negativo.');
    }

    if (!this.campoTextoLleno(this.producto.descripcion)) {
      return this.marcarErrorCampo('descripcion', 'Ingresa la descripción del producto.');
    }

    if (!this.producto.idCategoria || this.producto.idCategoria <= 0) {
      return this.marcarErrorCampo('idCategoria', 'Selecciona la categoría del producto.');
    }

    if (!this.producto.idUnidadMedida || this.producto.idUnidadMedida <= 0) {
      return this.marcarErrorCampo('idUnidadMedida', 'Selecciona la unidad de medida.');
    }

    if (this.imagenArchivo === null && !this.campoTextoLleno(this.producto.imagenUrl)) {
      return this.marcarErrorCampo('imagenUrl', 'Selecciona una imagen desde tu equipo.');
    }

    if (!this.editando) {
      if (stockInicial === null) {
        return this.marcarErrorCampo('stockInicial', 'Ingresa el stock inicial.');
      }

      if (stockInicial < 0) {
        return this.marcarErrorCampo('stockInicial', 'El stock inicial no puede ser negativo.');
      }
    }

    if (stockMinimo === null) {
      return this.marcarErrorCampo('stockMinimo', 'Ingresa el stock mínimo.');
    }

    if (stockMinimo < 0) {
      return this.marcarErrorCampo('stockMinimo', 'El stock mínimo no puede ser negativo.');
    }

    if (!this.editando && stockInicial !== null && stockInicial <= stockMinimo) {
      return this.marcarErrorCampo(
        'stockInicial',
        'El stock inicial debe ser mayor que el stock mínimo.'
      );
    }

    return null;
  }

  private obtenerMensajeErrorCampo(campo: string): string | null {
    const precio = this.obtenerNumero(this.producto.precioReferencial);
    const stockInicial = this.obtenerNumero(this.producto.stockInicial);
    const stockMinimo = this.obtenerNumero(this.producto.stockMinimo);

    switch (campo) {
      case 'nombre':
        if (!this.campoTextoLleno(this.producto.nombre)) {
          return 'Ingresa el nombre del producto.';
        }

        return null;

      case 'codigoProducto':
        if (!this.campoTextoLleno(this.producto.codigoProducto)) {
          return 'Ingresa el código del producto.';
        }

        return null;

      case 'precioReferencial':
        if (precio === null) {
          return 'Ingresa el precio referencial.';
        }

        if (precio < 0) {
          return 'El precio referencial no puede ser negativo.';
        }

        return null;

      case 'descripcion':
        if (!this.campoTextoLleno(this.producto.descripcion)) {
          return 'Ingresa la descripción del producto.';
        }

        return null;

      case 'idCategoria':
        if (!this.producto.idCategoria || this.producto.idCategoria <= 0) {
          return 'Selecciona la categoría del producto.';
        }

        return null;

      case 'idUnidadMedida':
        if (!this.producto.idUnidadMedida || this.producto.idUnidadMedida <= 0) {
          return 'Selecciona la unidad de medida.';
        }

        return null;

      case 'imagenUrl':
        if (this.imagenArchivo === null && !this.campoTextoLleno(this.producto.imagenUrl)) {
          return 'Selecciona una imagen desde tu equipo.';
        }

        return null;

      case 'stockInicial':
        if (!this.editando) {
          if (stockInicial === null) {
            return 'Ingresa el stock inicial.';
          }

          if (stockInicial < 0) {
            return 'El stock inicial no puede ser negativo.';
          }

          if (stockMinimo !== null && stockInicial <= stockMinimo) {
            return 'El stock inicial debe ser mayor que el stock mínimo.';
          }
        }

        return null;

      case 'stockMinimo':
        if (stockMinimo === null) {
          return 'Ingresa el stock mínimo.';
        }

        if (stockMinimo < 0) {
          return 'El stock mínimo no puede ser negativo.';
        }

        if (!this.editando && stockInicial !== null && stockInicial <= stockMinimo) {
          return 'El stock inicial debe ser mayor que el stock mínimo.';
        }

        return null;

      default:
        return null;
    }
  }

  private marcarErrorCampo(campo: string, mensaje: string): string {
    this.erroresCampo[campo] = mensaje;
    return mensaje;
  }

  private campoTextoLleno(valor?: string | null): boolean {
    return !!valor && valor.trim().length > 0;
  }

  private obtenerNumero(valor: unknown): number | null {
    if (valor === null || valor === undefined || valor === '') {
      return null;
    }

    const numero = Number(valor);

    if (!Number.isFinite(numero)) {
      return null;
    }

    return numero;
  }

  private normalizarTexto(valor?: string | null): string | undefined {
    if (!valor || valor.trim().length === 0) {
      return undefined;
    }

    return valor.trim();
  }

  private resolverArchivo(ruta: string): string {
    const valor = ruta.trim();

    if (valor.startsWith('http://') || valor.startsWith('https://')) {
      return valor;
    }

    return valor.startsWith('/')
      ? `${this.baseArchivosUrl}${valor}`
      : `${this.baseArchivosUrl}/${valor}`;
  }

  private nuevoProducto(): ProductoCrear {
    return {
      nombre: '',
      descripcion: '',
      precioReferencial: null,
      imagenUrl: '',
      idCategoria: 0,
      idMarca: null,
      idUnidadMedida: null,
      codigoProducto: '',
      fichaTecnicaPdf: null,
      stockInicial: 0,
      stockMinimo: 0
    };
  }

  private subirArriba(): void {
    setTimeout(() => {
      window.scrollTo({
        top: 0,
        left: 0,
        behavior: 'auto'
      });
    }, 0);
  }
}
