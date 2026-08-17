import { Routes } from '@angular/router';
import { authGuard, managerGuard } from './core/guards/auth.guard';
import { ErroComponent } from './pages/erro/erro.component';
import { LoginComponent } from './pages/auth/login/login.component';
import { LayoutComponent } from './layout/layout.component';

export const routes: Routes = [
  { path: 'entrar', component: LoginComponent },

  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'painel' },

      {
        path: 'painel',
        loadComponent: () => import('./pages/painel/painel.component').then(m => m.PainelComponent)
      },
      {
        path: 'produtos',
        data: { dialeto: 'estoque' },
        loadComponent: () => import('./pages/produtos/produtos.component').then(m => m.ProdutosComponent)
      },
      {
        path: 'notas',
        data: { dialeto: 'faturamento' },
        loadComponent: () => import('./pages/notas/notas.component').then(m => m.NotasComponent)
      },
      {
        path: 'notas/:id',
        data: { dialeto: 'faturamento' },
        loadComponent: () => import('./pages/notas/nota-detalhe/nota-detalhe.component').then(m => m.NotaDetalheComponent)
      },
      {
        path: 'usuarios',
        canActivate: [managerGuard],
        loadComponent: () => import('./pages/usuarios/usuarios.component').then(m => m.UsuariosComponent)
      },
      {
        path: 'sem-acesso',
        component: ErroComponent,
        data: {
          codigo: '403',
          titulo: 'Seu perfil não alcança esta tela',
          texto: 'Esta área exige perfil Administrador ou Gerente. Fale com quem administra o sistema.',
          acao: 'Ir para as notas',
          destino: '/notas'
        }
      },
      { path: '**', component: ErroComponent }
    ]
  },

  { path: '**', redirectTo: '' }
];
