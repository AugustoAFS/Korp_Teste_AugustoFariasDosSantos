import '@angular/compiler';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PainelComponent } from './painel.component';

describe('PainelComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PainelComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();
  });

  it('deve criar o componente', () => {
    const fixture = TestBed.createComponent(PainelComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
