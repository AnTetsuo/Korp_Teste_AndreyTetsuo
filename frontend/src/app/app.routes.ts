import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'produtos' },
  {
    path: 'produtos',
    title: 'Produtos',
    loadComponent: () => import('./features/products/product-list').then((m) => m.ProductList),
  },
  {
    path: 'produtos/novo',
    title: 'Novo produto',
    loadComponent: () => import('./features/products/product-create').then((m) => m.ProductCreate),
  },
  {
    path: 'notas',
    title: 'Notas fiscais',
    loadComponent: () => import('./features/invoicing/list/invoice-list').then((m) => m.InvoiceList),
  },
  {
    path: 'notas/nova',
    title: 'Nova nota fiscal',
    loadComponent: () => import('./features/invoicing/create/invoice-create').then((m) => m.InvoiceCreate),
  },
  { path: '**', redirectTo: 'produtos' },
];
