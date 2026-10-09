import { HttpClient, httpResource, HttpParams } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { silent } from '../../core/error.interceptor';
import {
  CreateJobRequest,
  Job,
  JobFilter,
  JobPostingState,
  JobSummary,
  SavedSearch,
  SaveSearchRequest,
  SearchJobsRequest,
  SearchResult,
} from './jobs.models';

const filterParams: Record<JobFilter, Record<string, string>> = {
  all: {},
  new: { state: 'New' },
  bookmarked: { state: 'Bookmarked' },
  dismissed: { state: 'Dismissed' },
};

@Service()
export class JobsApi {
  private readonly http = inject(HttpClient);

  jobsResource(filter: () => JobFilter, search: () => string) {
    return httpResource<JobSummary[]>(
      () => {
        let params = new HttpParams({ fromObject: filterParams[filter()] });
        if (search().trim()) params = params.set('search', search().trim());
        return { url: '/api/jobs', params };
      },
      { defaultValue: [] },
    );
  }

  /** Not-found is a page state, not a toast. */
  jobResource(id: () => number | undefined) {
    return httpResource<Job>(() => {
      const value = id();
      return value ? { url: `/api/jobs/${value}`, context: silent() } : undefined;
    });
  }

  create(request: CreateJobRequest): Promise<Job> {
    return firstValueFrom(this.http.post<Job>('/api/jobs', request));
  }

  setState(id: number, state: JobPostingState): Promise<Job> {
    return firstValueFrom(this.http.put<Job>(`/api/jobs/${id}/state`, { state }));
  }

  /** 409 when the job has an application (shown as a toast). */
  delete(id: number): Promise<unknown> {
    return firstValueFrom(this.http.delete(`/api/jobs/${id}`));
  }

  search(request: SearchJobsRequest): Promise<SearchResult> {
    return firstValueFrom(this.http.post<SearchResult>('/api/jobs/search', request));
  }

  savedSearchesResource() {
    return httpResource<SavedSearch[]>(() => '/api/saved-searches', { defaultValue: [] });
  }

  createSavedSearch(request: SaveSearchRequest): Promise<SavedSearch> {
    return firstValueFrom(this.http.post<SavedSearch>('/api/saved-searches', request));
  }

  updateSavedSearch(id: number, request: SaveSearchRequest): Promise<SavedSearch> {
    return firstValueFrom(this.http.put<SavedSearch>(`/api/saved-searches/${id}`, request));
  }

  deleteSavedSearch(id: number): Promise<unknown> {
    return firstValueFrom(this.http.delete(`/api/saved-searches/${id}`));
  }
}
