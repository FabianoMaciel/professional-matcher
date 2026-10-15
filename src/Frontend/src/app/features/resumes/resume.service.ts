import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Resume, ResumeInput } from '../../models/resume.model';

@Injectable({ providedIn: 'root' })
export class ResumeService {
  private readonly http = inject(HttpClient);

  list(professionalId: string): Observable<Resume[]> {
    return this.http.get<Resume[]>(this.url(professionalId));
  }

  create(professionalId: string, resume: ResumeInput): Observable<Resume> {
    return this.http.post<Resume>(this.url(professionalId), resume);
  }

  update(professionalId: string, id: string, resume: ResumeInput): Observable<Resume> {
    return this.http.put<Resume>(`${this.url(professionalId)}/${id}`, resume);
  }

  duplicate(professionalId: string, id: string): Observable<Resume> {
    return this.http.post<Resume>(`${this.url(professionalId)}/${id}/duplicate`, null);
  }

  delete(professionalId: string, id: string): Observable<void> {
    return this.http.delete<void>(`${this.url(professionalId)}/${id}`);
  }

  pdfUrl(professionalId: string, id: string): string {
    return `${this.url(professionalId)}/${id}/pdf`;
  }

  private url(professionalId: string): string {
    return `${environment.apiBaseUrl}/professionals/${professionalId}/resumes`;
  }
}
