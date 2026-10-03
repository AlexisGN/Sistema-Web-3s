import { Directive, ElementRef, forwardRef, HostListener } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export function numeroNacionalPeru(value: string | null | undefined): string {
  const digits = (value || '').replace(/\D/g, '');
  return digits.startsWith('51') && digits.length > 9 ? digits.slice(2) : digits === '51' && (value || '').startsWith('+') ? '' : digits;
}

export function telefonoCompletoPeru(value: string | null | undefined): string {
  const number = numeroNacionalPeru(value);
  return number ? '+51' + number : '';
}

/** El campo muestra el número nacional; el modelo conserva el prefijo internacional. */
@Directive({
  selector: 'input[s3sTelefonoPe]', standalone: true,
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => TelefonoPeDirective), multi: true }],
  host: { type: 'tel', inputmode: 'numeric', autocomplete: 'tel-national', 'aria-label': 'Teléfono de Perú, prefijo +51' }
})
export class TelefonoPeDirective implements ControlValueAccessor {
  private changed: (value: string) => void = () => {};
  private touched: () => void = () => {};
  constructor(private readonly element: ElementRef<HTMLInputElement>) {}
  writeValue(value: string | null): void { this.element.nativeElement.value = numeroNacionalPeru(value); }
  registerOnChange(fn: (value: string) => void): void { this.changed = fn; }
  registerOnTouched(fn: () => void): void { this.touched = fn; }
  setDisabledState(disabled: boolean): void { this.element.nativeElement.disabled = disabled; }
  @HostListener('input') onInput(): void {
    const number = numeroNacionalPeru(this.element.nativeElement.value);
    this.element.nativeElement.value = number;
    this.changed(number ? '+51' + number : '');
  }
  @HostListener('blur') onBlur(): void { this.touched(); }
}
