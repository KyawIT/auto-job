import { HttpClient, httpResource } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { silent } from '../../core/error.interceptor';
import { Profile, ProfileSection, SaveProfileRequest } from './profile.models';

@Service()
export class ProfileApi {
  private readonly http = inject(HttpClient);

  /** 404 means "no profile yet" — a normal state, not an error toast. */
  profileResource() {
    return httpResource<Profile>(() => ({ url: '/api/profile', context: silent() }));
  }

  save(request: SaveProfileRequest): Promise<Profile> {
    return firstValueFrom(this.http.put<Profile>('/api/profile', request));
  }

  /** Fills empty fields and adds missing entries from an uploaded CV (sends the text to the AI). */
  importCv(documentId: number): Promise<Profile> {
    return firstValueFrom(this.http.post<Profile>('/api/profile/import-cv', { documentId }));
  }

  addEntry<T>(section: ProfileSection, request: T): Promise<unknown> {
    return firstValueFrom(this.http.post(`/api/profile/${section}`, request));
  }

  updateEntry<T>(section: ProfileSection, id: number, request: T): Promise<unknown> {
    return firstValueFrom(this.http.put(`/api/profile/${section}/${id}`, request));
  }

  deleteEntry(section: ProfileSection, id: number): Promise<unknown> {
    return firstValueFrom(this.http.delete(`/api/profile/${section}/${id}`));
  }
}
