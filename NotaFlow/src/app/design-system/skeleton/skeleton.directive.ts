import { Directive, input } from '@angular/core';

@Directive({
  selector: '[appSkeleton]',
  host: {
    '[class.esqueleto]': 'appSkeleton()',
    '[attr.aria-busy]': 'appSkeleton() ? "true" : null'
  }
})
export class SkeletonDirective {
  readonly appSkeleton = input.required<boolean>();
}

export { SkeletonDirective as Skeleton };
