import { Injectable } from '@angular/core';
import { MatPaginatorIntl } from '@angular/material/paginator';

@Injectable()
export class KorpPaginatorIntl extends MatPaginatorIntl {
  override itemsPerPageLabel = 'Itens por página:';
  override nextPageLabel = 'Próxima página';
  override previousPageLabel = 'Página anterior';
  override firstPageLabel = 'Primeira página';
  override lastPageLabel = 'Última página';

  override getRangeLabel = (page: number, pageSize: number, length: number): string => {
    const total = Math.max(length, 0);

    if (total === 0 || pageSize === 0) {
      return `0 de ${total}`;
    }

    const start = page * pageSize;
    const end = start < total ? Math.min(start + pageSize, total) : start + pageSize;

    return `${start + 1} – ${end} de ${total}`;
  };
}
