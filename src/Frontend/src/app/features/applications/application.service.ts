import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { JobApplication, JobApplicationInput } from '../../models/job-application.model';

/**
 * The backend is expected to append a history entry whenever `stage` or `stageDetail`
 * changes on an update.
 */
@Injectable({ providedIn: 'root' })
export class ApplicationService {
  private readonly http = inject(HttpClient);

  list(professionalId: string): Observable<JobApplication[]> {
    return this.http.get<JobApplication[]>(this.url(professionalId));
  }

  create(professionalId: string, application: JobApplicationInput): Observable<JobApplication> {
    return this.http.post<JobApplication>(this.url(professionalId), application);
  }

  update(
    professionalId: string,
    id: string,
    application: JobApplicationInput,
  ): Observable<JobApplication> {
    return this.http.put<JobApplication>(`${this.url(professionalId)}/${id}`, application);
  }

  delete(professionalId: string, id: string): Observable<void> {
    return this.http.delete<void>(`${this.url(professionalId)}/${id}`);
  }

  private url(professionalId: string): string {
    return `${environment.apiBaseUrl}/professionals/${professionalId}/applications`;
  }
}
