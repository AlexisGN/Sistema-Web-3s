import { UiIconComponent } from '../../shared/ui-icon/ui-icon';
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { ServicioService } from '../../core/services/servicio';
import { CotizacionService } from '../../core/services/cotizacion';
import { ProductoService } from '../../core/services/producto';
import { ProveedorService } from '../../core/services/proveedor';
import { InventarioService } from '../../core/services/inventario';
import { ClienteService } from '../../core/services/cliente';
import { CompraService } from '../../core/services/compra';
import { VentaService } from '../../core/services/venta';
import { CajaService } from '../../core/services/caja.service';
import { SessionService } from '../../core/services/session.service';

import { MovimientoStockListado } from '../../core/models/inventario.model';
import { CompraListado } from '../../core/models/compra.model';
import { VentaListado } from '../../core/models/venta.model';

interface DashboardCard {
  label: string;
  value: string;
  description: string;
  tag: string;
  status: 'ready' | 'pending' | 'warning' | 'money';
}

interface QuickAction {
  label: string;
  description: string;
  route?: string;
  disabled?: boolean;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [UiIconComponent, CommonModule, RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardComponent implements OnInit {
  cargando = false;

  movimientosRecientes: MovimientoStockListado[] = [];

  cards: DashboardCard[] = [
    {
      label: 'Productos activos',
      value: '0',
      description: 'Catálogo industrial disponible',
      tag: 'PR',
      status: 'ready'
    },
    {
      label: 'Servicios activos',
      value: '0',
      description: 'Servicios publicados para clientes',
      tag: 'SV',
      status: 'ready'
    },
    {
      label: 'Clientes activos',
      value: '0',
      description: 'Clientes disponibles para ventas',
      tag: 'CL',
      status: 'ready'
    },
    {
      label: 'Proveedores activos',
      value: '0',
      description: 'Proveedores disponibles para compras',
      tag: 'PV',
      status: 'ready'
    },
    {
      label: 'Cotizaciones pendientes',
      value: '0',
      description: 'Solicitudes por atender',
      tag: 'CT',
      status: 'warning'
    },
    {
      label: 'Ventas del día',
      value: 'S/ 0.00',
      description: 'Ingresos comerciales del día',
      tag: 'VT',
      status: 'money'
    },
    {
      label: 'Productos con stock bajo',
      value: '0',
      description: 'Productos por debajo del mínimo',
      tag: 'ST',
      status: 'ready'
    },
    {
      label: 'Saldo de caja',
      value: 'S/ 0.00',
      description: 'Saldo operativo disponible',
      tag: 'CJ',
      status: 'money'
    },
    {
      label: 'Compras pendientes de pago',
      value: '0',
      description: 'Compras con saldo pendiente',
      tag: 'CP',
      status: 'ready'
    }
  ];

  quickActions: QuickAction[] = [
    {
      label: 'Nuevo producto',
      description: 'Registrar producto industrial',
      route: '/admin/productos'
    },
    {
      label: 'Nuevo servicio',
      description: 'Registrar servicio de 3S',
      route: '/admin/servicios'
    },
    {
      label: 'Registrar cliente',
      description: 'Agregar cliente natural o empresa',
      route: '/admin/clientes'
    },
    {
      label: 'Registrar proveedor',
      description: 'Consultar RUC por SUNAT y registrar proveedor',
      route: '/admin/proveedores'
    },
    {
      label: 'Ver stock bajo',
      description: 'Revisar productos con stock bajo o sin stock',
      route: '/admin/inventario'
    },
    {
      label: 'Registrar venta',
      description: 'Crear venta directa o desde cotización',
      route: '/admin/ventas'
    },
    {
      label: 'Registrar compra',
      description: 'Ingresar compra a proveedor',
      route: '/admin/compras'
    }
  ];

  constructor(
    private productoService: ProductoService,
    private servicioService: ServicioService,
    private cotizacionService: CotizacionService,
    private proveedorService: ProveedorService,
    private inventarioService: InventarioService,
    private clienteService: ClienteService,
    private compraService: CompraService,
    private ventaService: VentaService,
    private cajaService: CajaService,
    public sessionService: SessionService,
    private cdr: ChangeDetectorRef
  ) { }

  get cardsVisibles(): DashboardCard[] {
    const permisos: Record<string, string> = {
      'Productos activos': 'PRODUCTOS_VER',
      'Servicios activos': 'SERVICIOS_VER',
      'Clientes activos': 'CLIENTES_VER',
      'Proveedores activos': 'PROVEEDORES_VER',
      'Cotizaciones pendientes': 'COTIZACIONES_VER',
      'Ventas del día': 'VENTAS_VER',
      'Productos con stock bajo': 'INVENTARIO_VER',
      'Saldo de caja': 'CAJA_VER',
      'Compras pendientes de pago': 'COMPRAS_VER'
    };
    return this.cards.filter(c => this.sessionService.tienePermiso(permisos[c.label]));
  }

  get quickActionsVisibles(): QuickAction[] {
    const permisos = ['PRODUCTOS_CREAR', 'SERVICIOS_CREAR', 'CLIENTES_CREAR', 'PROVEEDORES_CREAR', 'INVENTARIO_VER', 'VENTAS_CREAR', 'COMPRAS_CREAR'];
    const vistas = ['PRODUCTOS_VER','SERVICIOS_VER','CLIENTES_VER','PROVEEDORES_VER','INVENTARIO_VER','VENTAS_VER','COMPRAS_VER'];
    return this.quickActions.filter((_, i) => this.sessionService.tienePermiso(permisos[i]) && this.sessionService.tienePermiso(vistas[i]));
  }

  ngOnInit(): void {
    this.cargarResumenProductos();
    this.cargarResumenServicios();
    this.cargarResumenClientes();
    this.cargarResumenCotizaciones();
    this.cargarResumenProveedores();
    this.cargarResumenInventario();
    this.cargarResumenComprasPendientesPago();
    this.cargarResumenVentasDia();
    this.cargarResumenCaja();
    this.cargarMovimientosRecientes();
  }

  cargarResumenProductos(): void {
    if (!this.sessionService.tienePermiso('PRODUCTOS_VER')) return;
    this.cargando = true;

    this.productoService.contarActivos().subscribe({
      next: (data) => {
        this.actualizarCard('Productos activos', String(data.total));
        this.cargando = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error cargando resumen de productos:', err);
        this.actualizarCard('Productos activos', '0');
        this.cargando = false;
        this.cdr.detectChanges();
      }
    });
  }

  cargarResumenServicios(): void {
    if (!this.sessionService.tienePermiso('SERVICIOS_VER')) return;
    this.servicioService.contarActivos().subscribe({
      next: (data) => {
        this.actualizarCard('Servicios activos', String(data.total));
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error cargando resumen de servicios:', err);
        this.actualizarCard('Servicios activos', '0');
        this.cdr.detectChanges();
      }
    });
  }

  cargarResumenClientes(): void {
    if (!this.sessionService.tienePermiso('CLIENTES_VER')) return;
    this.clienteService.contarActivos().subscribe({
      next: (data) => {
        this.actualizarCard('Clientes activos', String(data.total));
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error cargando resumen de clientes:', err);
        this.actualizarCard('Clientes activos', '0');
        this.cdr.detectChanges();
      }
    });
  }

  cargarResumenCotizaciones(): void {
    if (!this.sessionService.tienePermiso('COTIZACIONES_VER')) return;
    this.cotizacionService.contarPendientes().subscribe({
      next: (data) => {
        this.actualizarCard('Cotizaciones pendientes', String(data.total));
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error cargando resumen de cotizaciones:', err);
        this.actualizarCard('Cotizaciones pendientes', '0');
        this.cdr.detectChanges();
      }
    });
  }

  cargarResumenProveedores(): void {
    if (!this.sessionService.tienePermiso('PROVEEDORES_VER')) return;
    this.proveedorService.contarActivos().subscribe({
      next: (data) => {
        this.actualizarCard('Proveedores activos', String(data.total));
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error cargando resumen de proveedores:', err);
        this.actualizarCard('Proveedores activos', '0');
        this.cdr.detectChanges();
      }
    });
  }

  cargarResumenInventario(): void {
    if (!this.sessionService.tienePermiso('INVENTARIO_VER')) return;
    this.inventarioService.resumen().subscribe({
      next: (data) => {
        const totalCriticos = data.totalStockBajo + data.totalSinStock;

        this.actualizarCard('Productos con stock bajo', String(totalCriticos));
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error cargando resumen de inventario:', err);
        this.actualizarCard('Productos con stock bajo', '0');
        this.cdr.detectChanges();
      }
    });
  }

  cargarResumenComprasPendientesPago(): void {
    if (!this.sessionService.tienePermiso('COMPRAS_VER')) return;
    const tamanioPagina = 100;

    this.compraService
      .listar('', '', '', '', 1, tamanioPagina)
      .subscribe({
        next: (primeraPagina) => {
          const totalPaginas = primeraPagina.totalPaginas ?? 1;

          if (totalPaginas <= 1) {
            const totalPendientes = this.contarComprasPendientesPago(primeraPagina.items ?? []);
            this.actualizarCard('Compras pendientes de pago', String(totalPendientes));
            this.cdr.detectChanges();
            return;
          }

          const solicitudes = [];

          for (let pagina = 2; pagina <= totalPaginas; pagina++) {
            solicitudes.push(
              this.compraService.listar('', '', '', '', pagina, tamanioPagina)
            );
          }

          forkJoin(solicitudes).subscribe({
            next: (paginasRestantes) => {
              let compras: CompraListado[] = [...(primeraPagina.items ?? [])];

              paginasRestantes.forEach((pagina) => {
                compras = compras.concat(pagina.items ?? []);
              });

              const totalPendientes = this.contarComprasPendientesPago(compras);

              this.actualizarCard('Compras pendientes de pago', String(totalPendientes));
              this.cdr.detectChanges();
            },
            error: (err) => {
              console.error('Error cargando páginas de compras:', err);

              const totalPendientes = this.contarComprasPendientesPago(primeraPagina.items ?? []);
              this.actualizarCard('Compras pendientes de pago', String(totalPendientes));
              this.cdr.detectChanges();
            }
          });
        },
        error: (err) => {
          console.error('Error cargando resumen de compras pendientes:', err);
          this.actualizarCard('Compras pendientes de pago', '0');
          this.cdr.detectChanges();
        }
      });
  }

  cargarResumenVentasDia(): void {
    if (!this.sessionService.tienePermiso('VENTAS_VER')) return;
    const fechaHoy = this.obtenerFechaActualIso();
    const tamanioPagina = 100;

    this.ventaService
      .listar('', '', '', '', fechaHoy, fechaHoy, 1, tamanioPagina)
      .subscribe({
        next: (primeraPagina) => {
          const totalPaginas = primeraPagina.totalPaginas ?? 1;

          if (totalPaginas <= 1) {
            const totalVentas = this.sumarVentasDia(primeraPagina.items ?? []);

            this.actualizarCard('Ventas del día', this.formatearSoles(totalVentas));
            this.cdr.detectChanges();
            return;
          }

          const solicitudes = [];

          for (let pagina = 2; pagina <= totalPaginas; pagina++) {
            solicitudes.push(
              this.ventaService.listar('', '', '', '', fechaHoy, fechaHoy, pagina, tamanioPagina)
            );
          }

          forkJoin(solicitudes).subscribe({
            next: (paginasRestantes) => {
              let ventas: VentaListado[] = [...(primeraPagina.items ?? [])];

              paginasRestantes.forEach((pagina) => {
                ventas = ventas.concat(pagina.items ?? []);
              });

              const totalVentas = this.sumarVentasDia(ventas);

              this.actualizarCard('Ventas del día', this.formatearSoles(totalVentas));
              this.cdr.detectChanges();
            },
            error: (err) => {
              console.error('Error cargando páginas de ventas del día:', err);

              const totalVentas = this.sumarVentasDia(primeraPagina.items ?? []);

              this.actualizarCard('Ventas del día', this.formatearSoles(totalVentas));
              this.cdr.detectChanges();
            }
          });
        },
        error: (err) => {
          console.error('Error cargando ventas del día:', err);
          this.actualizarCard('Ventas del día', 'S/ 0.00');
          this.cdr.detectChanges();
        }
      });
  }

  cargarResumenCaja(): void {
    if (!this.sessionService.tienePermiso('CAJA_VER')) return;
    const idUsuarioSesion = this.obtenerIdUsuarioSesion();

    const idsUsuarios = Array.from(
      new Set(
        [idUsuarioSesion, 1]
          .map(id => Number(id || 0))
          .filter(id => id > 0)
      )
    );

    this.consultarCajaPorUsuarios(idsUsuarios, 0);
  }

  private consultarCajaPorUsuarios(idsUsuarios: number[], indice: number): void {
    if (indice >= idsUsuarios.length) {
      this.actualizarCard('Saldo de caja', 'S/ 0.00');
      this.cdr.detectChanges();
      return;
    }

    const idUsuario = idsUsuarios[indice];

    this.cajaService.obtenerCajaActiva(idUsuario).subscribe({
      next: (cajaActiva) => {
        if (!cajaActiva) {
          this.consultarCajaPorUsuarios(idsUsuarios, indice + 1);
          return;
        }

        const idCaja = this.obtenerIdCajaDesdeRespuesta(cajaActiva);

        this.cajaService.obtenerResumen(idUsuario, idCaja).subscribe({
          next: (resumen) => {
            const saldoResumen = this.obtenerSaldoCajaDesdeRespuesta(resumen);
            const saldoCajaActiva = this.obtenerSaldoCajaDesdeRespuesta(cajaActiva);

            const saldo = saldoResumen !== 0 ? saldoResumen : saldoCajaActiva;

            this.actualizarCard('Saldo de caja', this.formatearSoles(saldo));
            this.cdr.detectChanges();
          },
          error: (err) => {
            console.error('Error cargando resumen de caja:', err);

            const saldoCajaActiva = this.obtenerSaldoCajaDesdeRespuesta(cajaActiva);

            if (saldoCajaActiva !== 0) {
              this.actualizarCard('Saldo de caja', this.formatearSoles(saldoCajaActiva));
              this.cdr.detectChanges();
              return;
            }

            this.consultarCajaPorUsuarios(idsUsuarios, indice + 1);
          }
        });
      },
      error: (err) => {
        console.error(`Error cargando caja activa para usuario ${idUsuario}:`, err);
        this.consultarCajaPorUsuarios(idsUsuarios, indice + 1);
      }
    });
  }

  cargarMovimientosRecientes(): void {
    if (!this.sessionService.tienePermiso('INVENTARIO_REPORTE')) return;
    this.inventarioService.listarMovimientosRecientes(3).subscribe({
      next: (data) => {
        this.movimientosRecientes = data;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error cargando movimientos recientes:', err);
        this.movimientosRecientes = [];
        this.cdr.detectChanges();
      }
    });
  }

  obtenerEstadoCard(card: DashboardCard): string {
    if (
      card.label === 'Productos con stock bajo' ||
      card.label === 'Saldo de caja' ||
      card.label === 'Compras pendientes de pago'
    ) {
      return 'Activo';
    }

    if (card.status === 'ready') {
      return 'Activo';
    }

    if (card.status === 'money') {
      return 'Operativo';
    }

    return 'Pendiente';
  }

  obtenerSignoMovimiento(tipoMovimiento: string): string {
    const tipo = tipoMovimiento.toLowerCase();

    if (tipo === 'entrada') {
      return '+';
    }

    if (tipo === 'salida') {
      return '-';
    }

    return '±';
  }

  private obtenerIdUsuarioSesion(): number {
    const idUsuario = Number(this.sessionService.obtenerIdUsuario() || 0);

    return idUsuario > 0 ? idUsuario : 1;
  }

  private contarComprasPendientesPago(compras: CompraListado[]): number {
    return compras.filter((compra) => {
      const estadoCompra = (compra.estadoCompra || '').toLowerCase();
      const saldoPendiente = Number(compra.saldoPendiente || 0);

      return !estadoCompra.includes('anulada') && saldoPendiente > 0;
    }).length;
  }

  private sumarVentasDia(ventas: VentaListado[]): number {
    return ventas
      .filter((venta) => {
        const item = venta as any;

        const estadoVenta = String(
          item.estadoVenta ??
          item.EstadoVenta ??
          item.estado ??
          item.Estado ??
          ''
        ).toLowerCase();

        return !estadoVenta.includes('anulada') && !estadoVenta.includes('cancelada');
      })
      .reduce((total, venta) => total + this.obtenerTotalVenta(venta), 0);
  }

  private obtenerTotalVenta(venta: VentaListado): number {
    const item = venta as any;

    const total =
      item.total ??
      item.Total ??
      item.totalVenta ??
      item.TotalVenta ??
      item.importeTotal ??
      item.ImporteTotal ??
      item.montoTotal ??
      item.MontoTotal ??
      item.totalPagado ??
      item.TotalPagado ??
      0;

    return Number(total || 0);
  }

  private obtenerIdCajaDesdeRespuesta(caja: any): number | null {
    if (!caja) {
      return null;
    }

    const idCaja =
      caja.idCaja ??
      caja.IdCaja ??
      caja.id ??
      caja.Id ??
      null;

    const valor = Number(idCaja || 0);

    return valor > 0 ? valor : null;
  }

  private obtenerSaldoCajaDesdeRespuesta(data: any): number {
    if (!data) {
      return 0;
    }

    const saldoDirecto = this.obtenerNumeroDesdeRespuesta(data, [
      'saldoEsperado',
      'SaldoEsperado',
      'saldoEsperadoCaja',
      'SaldoEsperadoCaja',
      'saldoActual',
      'SaldoActual',
      'saldoCaja',
      'SaldoCaja',
      'saldoDisponible',
      'SaldoDisponible',
      'saldoFinal',
      'SaldoFinal',
      'saldoOperativo',
      'SaldoOperativo',
      'saldoCalculado',
      'SaldoCalculado',
      'totalSaldo',
      'TotalSaldo',
      'montoActual',
      'MontoActual'
    ]);

    if (saldoDirecto !== 0) {
      return saldoDirecto;
    }

    const saldoInicial = this.obtenerNumeroDesdeRespuesta(data, [
      'saldoInicial',
      'SaldoInicial',
      'montoInicial',
      'MontoInicial',
      'montoApertura',
      'MontoApertura',
      'saldoApertura',
      'SaldoApertura'
    ]);

    const totalIngresos = this.obtenerNumeroDesdeRespuesta(data, [
      'totalIngresos',
      'TotalIngresos'
    ]);

    const ingresosDetalle =
      this.obtenerNumeroDesdeRespuesta(data, [
        'ingresosPorVenta',
        'IngresosPorVenta',
        'ingresosVenta',
        'IngresosVenta',
        'totalIngresosVenta',
        'TotalIngresosVenta'
      ]) +
      this.obtenerNumeroDesdeRespuesta(data, [
        'ingresosManuales',
        'IngresosManuales',
        'ingresosManual',
        'IngresosManual',
        'totalIngresosManuales',
        'TotalIngresosManuales'
      ]);

    const ingresos = totalIngresos !== 0 ? totalIngresos : ingresosDetalle;

    const totalEgresos = this.obtenerNumeroDesdeRespuesta(data, [
      'totalEgresos',
      'TotalEgresos'
    ]);

    const egresosDetalle =
      this.obtenerNumeroDesdeRespuesta(data, [
        'egresosPorCompra',
        'EgresosPorCompra',
        'egresosCompra',
        'EgresosCompra',
        'totalEgresosCompra',
        'TotalEgresosCompra'
      ]) +
      this.obtenerNumeroDesdeRespuesta(data, [
        'egresosManuales',
        'EgresosManuales',
        'egresosManual',
        'EgresosManual',
        'totalEgresosManuales',
        'TotalEgresosManuales'
      ]);

    const egresos = totalEgresos !== 0 ? totalEgresos : egresosDetalle;

    const ajustes = this.obtenerNumeroDesdeRespuesta(data, [
      'ajustes',
      'Ajustes',
      'totalAjustes',
      'TotalAjustes'
    ]);

    if (saldoInicial !== 0 || ingresos !== 0 || egresos !== 0 || ajustes !== 0) {
      return saldoInicial + ingresos - egresos + ajustes;
    }

    return 0;
  }

  private obtenerNumeroDesdeRespuesta(data: any, campos: string[]): number {
    for (const campo of campos) {
      const valor = data?.[campo];

      if (valor === null || valor === undefined || valor === '') {
        continue;
      }

      if (typeof valor === 'number') {
        return Number(valor || 0);
      }

      const texto = String(valor)
        .replace(/S\/?/gi, '')
        .replace(/\s/g, '')
        .replace(/,/g, '')
        .trim();

      const numero = Number(texto);

      if (!Number.isNaN(numero)) {
        return numero;
      }
    }

    return 0;
  }

  private obtenerFechaActualIso(): string {
    const hoy = new Date();
    const anio = hoy.getFullYear();
    const mes = String(hoy.getMonth() + 1).padStart(2, '0');
    const dia = String(hoy.getDate()).padStart(2, '0');

    return `${anio}-${mes}-${dia}`;
  }

  private formatearSoles(valor: number): string {
    const numero = Number(valor || 0);

    return `S/ ${numero.toLocaleString('es-PE', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    })}`;
  }

  private actualizarCard(label: string, value: string): void {
    this.cards = this.cards.map((card) => {
      if (card.label === label) {
        return {
          ...card,
          value
        };
      }

      return card;
    });
  }
}