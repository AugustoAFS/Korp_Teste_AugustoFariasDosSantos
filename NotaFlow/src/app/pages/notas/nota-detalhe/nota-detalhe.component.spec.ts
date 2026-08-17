import '@angular/compiler';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NotaDetalheComponent } from './nota-detalhe.component';

describe('NotaDetalheComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NotaDetalheComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();
  });

  it('deve criar o componente', () => {
    const fixture = TestBed.createComponent(NotaDetalheComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
