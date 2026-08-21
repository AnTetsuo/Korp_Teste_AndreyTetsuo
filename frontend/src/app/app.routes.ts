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
    loadComponent: () => import('./features/invoices/invoice-list').then((m) => m.InvoiceList),
  },
  { path: '**', redirectTo: 'produtos' },
];
