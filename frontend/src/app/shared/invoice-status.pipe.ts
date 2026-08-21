import { Pipe, PipeTransform } from '@angular/core';

const LABELS: Readonly<Record<string, string>> = {
  Open: 'Aberta',
  Processing: 'Processando',
  Closed: 'Fechada',
};

@Pipe({ name: 'invoiceStatus' })
export class InvoiceStatusPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    if (value === null || value === undefined || value.length === 0) {
      return '';
    }

    return LABELS[value] ?? value;
  }
}
