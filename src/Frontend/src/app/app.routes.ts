import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'profile' },
  {
    path: 'profile',
    title: 'Perfil',
    loadComponent: () => import('./features/profile/profile-page').then((m) => m.ProfilePage),
  },
  {
    path: 'resumes',
    title: 'Currículos',
    loadComponent: () => import('./features/resumes/resumes-page').then((m) => m.ResumesPage),
  },
  {
    path: 'applications',
    title: 'Aplicações',
    loadComponent: () =>
      import('./features/applications/applications-page').then((m) => m.ApplicationsPage),
  },
  {
    path: 'market',
    title: 'Análise de Mercado',
    loadComponent: () => import('./features/market/market-page').then((m) => m.MarketPage),
  },
  { path: '**', redirectTo: 'profile' },
];
