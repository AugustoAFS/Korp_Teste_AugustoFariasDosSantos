import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { CoachmarkComponent } from './design-system/coachmark/coachmark.component';
import { ConfirmComponent } from './design-system/confirm/confirm.component';
import { ToastComponent } from './design-system/toast/toast.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CoachmarkComponent, ConfirmComponent, ToastComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  private readonly router = inject(Router);

  private readonly navegou = toSignal(
    this.router.events.pipe(filter(e => e instanceof NavigationEnd), map(() => this.folha())),
    { initialValue: null }
  );

  protected readonly dialeto = computed(() => this.navegou()?.data['dialeto'] ?? null);

  private folha = () => {
    let no = this.router.routerState.snapshot.root;
    while (no.firstChild) no = no.firstChild;
    return no;
  };
}

export { AppComponent as App };
