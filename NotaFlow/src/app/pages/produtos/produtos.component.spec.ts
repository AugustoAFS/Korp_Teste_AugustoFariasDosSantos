import '@angular/compiler';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ProdutosComponent } from './produtos.component';

describe('ProdutosComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProdutosComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();
  });

  it('deve criar o componente', () => {
    const fixture = TestBed.createComponent(ProdutosComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
