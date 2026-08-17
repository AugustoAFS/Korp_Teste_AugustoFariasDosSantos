import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { ProblemDetails } from '../models/problem-details';
import { ToastService } from '../services/toast.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((erro: HttpErrorResponse) => {
      const problem = erro.error as ProblemDetails | null;
      const detalhe = problem?.detail ?? problem?.title;

      if (erro.status === 0) {
        toast.bad('Sem conexão com o servidor. Verifique se os serviços estão no ar.');
      } else if (erro.status === 401) {
        if (!req.url.includes('/auth/login')) {
          toast.warn('Sua sessão expirou. Entre novamente.');
          void router.navigate(['/entrar']);
        }
      } else if (erro.status === 403) {
        toast.bad(detalhe ?? 'Seu perfil não permite esta ação.');
      } else if (erro.status === 503) {
        toast.bad(detalhe ?? 'Serviço indisponível. Sua nota continua aberta.');
      } else if (erro.status === 429) {
        toast.warn(detalhe ?? 'Muitas requisições. Aguarde alguns instantes.');
      } else if (erro.status >= 500) {
        toast.bad(`Erro interno. Código ${problem?.traceId ?? 'desconhecido'}.`);
      } else if (detalhe !== undefined) {
        toast.warn(detalhe);
      }

      return throwError(() => erro);
    })
  );
};
