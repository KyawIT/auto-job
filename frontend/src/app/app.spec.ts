import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('should render title and backend status', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    TestBed.inject(HttpTestingController).expectOne('/health').flush({ status: 'ok' });
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('AutoJob');
    expect(compiled.querySelector('[role="status"]')?.textContent).toContain('Backend connected');
  });
});
