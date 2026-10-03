import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

// Una sola familia SVG de trazo para toda la administración. Iconos decorativos:
// el texto y las etiquetas accesibles permanecen en el control que los contiene.
const PATHS: Record<string, string[]> = {
  home: ['M3 10 12 3l9 7v10a1 1 0 0 1-1 1h-5v-7H9v7H4a1 1 0 0 1-1-1Z'],
  box: ['m12 3 9 5v8l-9 5-9-5V8Z', 'm3 8 9 5 9-5M12 13v8M7.5 5.5l9 5'],
  tool: ['M14 5a5 5 0 0 0-6 6L3 16a3 3 0 0 0 5 5l5-5a5 5 0 0 0 6-6l-3 3-4-4 3-3'],
  file: ['M14 3H5v18h14V8Z', 'M14 3v5h5M8 12h8M8 16h5'],
  people: ['M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M6 7a3 3 0 1 0 6 0 3 3 0 0 0-6 0M17 4a3 3 0 0 1 0 6M22 21v-2a4 4 0 0 0-3-3.87'],
  receipt: ['M5 3v18l3-2 4 2 4-2 3 2V3l-3 2-4-2-4 2ZM9 9h6M9 13h6'],
  building: ['M4 21V3h12v18M16 10h4v11M2 21h20M8 7h4M8 11h4M8 15h4M9 21v-3h2v3'],
  cart: ['M2 3h3l3 12h11l3-9H6M10 20h.01M18 20h.01'],
  layers: ['m12 3 10 5-10 5L2 8ZM2 12l10 5 10-5M2 16l10 5 10-5'],
  wallet: ['M20 7V4H5a3 3 0 0 0 0 6h17v10H5a3 3 0 0 1-3-3V7M22 13h-6v4h6'],
  shield: ['M12 3 3 7v5c0 5 9 9 9 9s9-4 9-9V7ZM8 12l3 3 5-6'],
  activity: ['M3 3v18h18M6 13h3l3-7 3 11 3-5h3'],
  search: ['M10 3a7 7 0 1 0 0 14 7 7 0 0 0 0-14M15 15l6 6'],
  edit: ['m15 4 5 5M4 15 16 3a2 2 0 0 1 5 5L9 20l-6 1ZM13 21h8'],
  trash: ['M3 6h18M9 6V3h6v3M5 6l1 15h12l1-15M10 10v7M14 10v7'],
  plus: ['M12 5v14M5 12h14'],
  check: ['m5 12 4 4L19 6'],
  close: ['m6 6 12 12M6 18 18 6'],
  arrow: ['M4 12h16m-6-6 6 6-6 6'],
  left: ['m15 6-6 6 6 6'],
  right: ['m9 6 6 6-6 6'],
  menu: ['M4 6h16M4 12h16M4 18h16'],
  logout: ['M9 3H3v18h6M9 12h12m-5-5 5 5-5 5'],
  refresh: ['M20 7a9 9 0 1 0 1 8M20 2v6h-6'],
  upload: ['M12 16V3m-5 5 5-5 5 5M4 16v5h16v-5'],
  download: ['M12 3v13m-5-5 5 5 5-5M4 16v5h16v-5'],
  mail: ['M3 5h18v14H3ZM3 5l9 7 9-7'],
  eye: ['M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12ZM9 12a3 3 0 1 0 6 0 3 3 0 0 0-6 0'],
  pin: ['M19 10c0 5-7 11-7 11S5 15 5 10a7 7 0 0 1 14 0ZM9 10a3 3 0 1 0 6 0 3 3 0 0 0-6 0'],
  truck: ['M2 5h12v12H2ZM14 9h5l3 4v4h-8M5 17a2 2 0 1 0 4 0M16 17a2 2 0 1 0 4 0'],
  key: ['M8 3a5 5 0 1 0 0 10 5 5 0 0 0 0-10M12 12l9 9M16 16l3-3M19 19l3-3'],
  info: ['M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20M12 11v6M12 7h.01'],
  minus: ['M5 12h14']
};
const ALIASES: Record<string, string> = {
  IN: 'home', PR: 'box', SV: 'tool', CT: 'file', CL: 'people', VT: 'receipt',
  PV: 'building', CP: 'cart', ST: 'layers', CJ: 'wallet', UR: 'shield', AU: 'activity'
};

@Component({
  selector: 's3s-icon',
  standalone: true,
  imports: [CommonModule],
  template: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"
    stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false">
    <path *ngFor="let path of paths" [attr.d]="path" />
  </svg>`,
  styles: [':host{display:inline-flex;width:1.125rem;height:1.125rem;flex:0 0 auto;vertical-align:middle;pointer-events:none}svg{width:100%;height:100%}']
})
export class UiIconComponent {
  @Input() name = 'info';
  get paths(): string[] { return PATHS[ALIASES[this.name] || this.name] || PATHS['info']; }
}
