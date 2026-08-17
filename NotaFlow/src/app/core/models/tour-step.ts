export interface TourStep {
  readonly id: string;
  readonly title: string;
  readonly text: string;
  readonly anchor?: string;
  readonly route?: string;
}
