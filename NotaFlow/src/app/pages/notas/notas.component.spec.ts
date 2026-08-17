import '@angular/compiler';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NotasComponent } from './notas.component';

describe('NotasComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NotasComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();
  });

  it('deve criar o componente', () => {
    const fixture = TestBed.createComponent(NotasComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
