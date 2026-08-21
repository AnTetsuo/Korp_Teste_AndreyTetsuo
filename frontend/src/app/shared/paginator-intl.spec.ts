import { KorpPaginatorIntl } from './paginator-intl';

describe('KorpPaginatorIntl', () => {
  const intl = new KorpPaginatorIntl();

  it('translates every label', () => {
    expect(intl.itemsPerPageLabel).toBe('Itens por página:');
    expect(intl.nextPageLabel).toBe('Próxima página');
    expect(intl.previousPageLabel).toBe('Página anterior');
    expect(intl.firstPageLabel).toBe('Primeira página');
    expect(intl.lastPageLabel).toBe('Última página');
  });

  it.each([
    [0, 10, 47, '1 – 10 de 47'],
    [1, 10, 47, '11 – 20 de 47'],
    [4, 10, 47, '41 – 47 de 47'],
    [0, 10, 3, '1 – 3 de 3'],
  ])('renders page %i of size %i over %i as "%s"', (page, size, length, expected) => {
    expect(intl.getRangeLabel(page, size, length)).toBe(expected);
  });

  it.each([
    [0, 10, 0, '0 de 0'],
    [0, 0, 47, '0 de 47'],
  ])('renders an empty range as "%s"', (page, size, length, expected) => {
    expect(intl.getRangeLabel(page, size, length)).toBe(expected);
  });
});
