import '@angular/compiler';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ErroComponent } from './erro.component';

describe('ErroComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ErroComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();
  });

  it('deve criar o componente', () => {
    const fixture = TestBed.createComponent(ErroComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
