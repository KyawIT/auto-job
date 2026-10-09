import { HttpClient, httpResource } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { silent } from '../../core/error.interceptor';
import { Batch } from './batches.models';

@Service()
export class BatchesApi {
  private readonly http = inject(HttpClient);

  batchResource(id: () => number | undefined) {
    return httpResource<Batch>(() => {
      const value = id();
      return value ? { url: `/api/batches/${value}`, context: silent() } : undefined;
    });
  }

  /** Language null = each application keeps its own (new ones get the posting's language or the default). */
  start(jobPostingIds: number[], language: string | null): Promise<Batch> {
    return firstValueFrom(this.http.post<Batch>('/api/batches', { jobPostingIds, language }));
  }

  cancel(id: number): Promise<Batch> {
    return firstValueFrom(this.http.post<Batch>(`/api/batches/${id}/cancel`, null));
  }

  retryFailed(id: number): Promise<Batch> {
    return firstValueFrom(this.http.post<Batch>(`/api/batches/${id}/retry`, null));
  }
}
