import { HttpClient, httpResource } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AppDocument, DocumentType, UpdateDocumentRequest } from './documents.models';

@Service()
export class DocumentsApi {
  private readonly http = inject(HttpClient);

  documentsResource() {
    return httpResource<AppDocument[]>(() => '/api/documents', { defaultValue: [] });
  }

  upload(file: File, type: DocumentType, attachByDefault = false): Promise<AppDocument> {
    const body = new FormData();
    body.append('file', file, file.name);
    body.append('type', type);
    body.append('attachByDefault', String(attachByDefault));
    return firstValueFrom(this.http.post<AppDocument>('/api/documents', body));
  }

  update(id: number, request: UpdateDocumentRequest): Promise<AppDocument> {
    return firstValueFrom(this.http.put<AppDocument>(`/api/documents/${id}`, request));
  }

  download(id: number): Promise<Blob> {
    return firstValueFrom(this.http.get(`/api/documents/${id}/file`, { responseType: 'blob' }));
  }

  /** 409 when the document is still attached to an application (shown as a toast). */
  delete(id: number): Promise<unknown> {
    return firstValueFrom(this.http.delete(`/api/documents/${id}`));
  }
}
