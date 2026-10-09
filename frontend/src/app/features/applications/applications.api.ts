import { HttpClient, httpResource, HttpParams } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { silent, withRetry } from '../../core/error.interceptor';
import {
  ApplicationDetails,
  ApplicationStatus,
  ApplicationSummary,
  EmailDraft,
  ExportFormat,
  GeneratedText,
  GeneratedTextKind,
  UpdateApplicationRequest,
} from './applications.models';

@Service()
export class ApplicationsApi {
  private readonly http = inject(HttpClient);

  applicationsResource(status: () => ApplicationStatus | null) {
    return httpResource<ApplicationSummary[]>(
      () => {
        const s = status();
        return {
          url: '/api/applications',
          params: s ? new HttpParams().set('status', s) : undefined,
        };
      },
      { defaultValue: [] },
    );
  }

  applicationResource(id: () => number | undefined) {
    return httpResource<ApplicationDetails>(() => {
      const value = id();
      return value ? { url: `/api/applications/${value}`, context: silent() } : undefined;
    });
  }

  versionsResource(id: () => number | undefined, kind: () => GeneratedTextKind | null) {
    return httpResource<GeneratedText[]>(
      () => {
        const value = id();
        const k = kind();
        return value && k
          ? { url: `/api/applications/${value}/texts`, params: { kind: k } }
          : undefined;
      },
      { defaultValue: [] },
    );
  }

  /** Idempotent: returns the existing application if the job already has one. */
  start(jobPostingId: number, language: string | null = null): Promise<ApplicationDetails> {
    return firstValueFrom(
      this.http.post<ApplicationDetails>('/api/applications', { jobPostingId, language }),
    );
  }

  update(id: number, request: UpdateApplicationRequest): Promise<ApplicationDetails> {
    return firstValueFrom(this.http.put<ApplicationDetails>(`/api/applications/${id}`, request));
  }

  changeStatus(
    id: number,
    status: ApplicationStatus,
    note: string | null,
  ): Promise<ApplicationDetails> {
    return firstValueFrom(
      this.http.put<ApplicationDetails>(`/api/applications/${id}/status`, { status, note }),
    );
  }

  setAttachments(id: number, documentIds: number[]): Promise<ApplicationDetails> {
    return firstValueFrom(
      this.http.put<ApplicationDetails>(`/api/applications/${id}/attachments`, { documentIds }),
    );
  }

  addNote(id: number, note: string): Promise<ApplicationDetails> {
    return firstValueFrom(
      this.http.post<ApplicationDetails>(`/api/applications/${id}/notes`, { note }),
    );
  }

  delete(id: number): Promise<unknown> {
    return firstValueFrom(this.http.delete(`/api/applications/${id}`));
  }

  /** Sends the profile and posting to the AI. A failed attempt offers "Retry" on its toast. */
  generate(id: number, retry: () => void): Promise<ApplicationDetails> {
    return firstValueFrom(
      this.http.post<ApplicationDetails>(`/api/applications/${id}/generate`, null, {
        context: withRetry(retry),
      }),
    );
  }

  editText(textId: number, content: string): Promise<GeneratedText> {
    return firstValueFrom(this.http.put<GeneratedText>(`/api/texts/${textId}`, { content }));
  }

  selectVersion(textId: number): Promise<ApplicationDetails> {
    return firstValueFrom(this.http.post<ApplicationDetails>(`/api/texts/${textId}/select`, null));
  }

  export(id: number, format: ExportFormat): Promise<Blob> {
    return firstValueFrom(
      this.http.get(`/api/applications/${id}/export`, { params: { format }, responseType: 'blob' }),
    );
  }

  createEmailDraft(id: number): Promise<EmailDraft> {
    return firstValueFrom(this.http.post<EmailDraft>(`/api/applications/${id}/email-draft`, null));
  }
}
