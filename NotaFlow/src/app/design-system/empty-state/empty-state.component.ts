import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  templateUrl: './empty-state.html',
  styleUrl: './empty-state.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EmptyStateComponent {
  readonly titulo = input.required<string>();
  readonly explicacao = input.required<string>();
  readonly acao = input<string>();

  readonly acionar = output<void>();
}

export { EmptyStateComponent as EmptyState };
