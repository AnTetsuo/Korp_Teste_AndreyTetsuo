import { InvoiceStatusPipe } from './invoice-status.pipe';

describe('InvoiceStatusPipe', () => {
  const pipe = new InvoiceStatusPipe();

  it.each([
    ['Open', 'Aberta'],
    ['Processing', 'Processando'],
    ['Closed', 'Fechada'],
  ])('translates %s to %s', (value, expected) => {
    expect(pipe.transform(value)).toBe(expected);
  });

  it('passes an unknown status through rather than blanking the cell', () => {
    expect(pipe.transform('Cancelled')).toBe('Cancelled');
  });

  it.each([null, undefined, ''])('renders nothing for %s', (value) => {
    expect(pipe.transform(value)).toBe('');
  });
});
