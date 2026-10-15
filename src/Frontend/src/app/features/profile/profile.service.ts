import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Education,
  EducationInput,
  Experience,
  ExperienceInput,
  Professional,
  ProfessionalDetails,
  Skill,
  SkillInput,
} from '../../models/profile.model';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);
  private readonly root = `${environment.apiBaseUrl}/professionals`;

  get(id: string): Observable<Professional> {
    return this.http.get<Professional>(`${this.root}/${id}`);
  }

  updateDetails(id: string, details: ProfessionalDetails): Observable<Professional> {
    return this.http.put<Professional>(`${this.root}/${id}`, details);
  }

  uploadPhoto(id: string, file: File): Observable<{ photoUrl: string }> {
    const body = new FormData();
    body.append('file', file);
    return this.http.post<{ photoUrl: string }>(`${this.root}/${id}/photo`, body);
  }

  addSkill(id: string, skill: SkillInput): Observable<Skill> {
    return this.http.post<Skill>(`${this.root}/${id}/skills`, skill);
  }

  updateSkill(id: string, skillId: string, skill: SkillInput): Observable<Skill> {
    return this.http.put<Skill>(`${this.root}/${id}/skills/${skillId}`, skill);
  }

  deleteSkill(id: string, skillId: string): Observable<void> {
    return this.http.delete<void>(`${this.root}/${id}/skills/${skillId}`);
  }

  addExperience(id: string, experience: ExperienceInput): Observable<Experience> {
    return this.http.post<Experience>(`${this.root}/${id}/experiences`, experience);
  }

  updateExperience(
    id: string,
    experienceId: string,
    experience: ExperienceInput,
  ): Observable<Experience> {
    return this.http.put<Experience>(`${this.root}/${id}/experiences/${experienceId}`, experience);
  }

  deleteExperience(id: string, experienceId: string): Observable<void> {
    return this.http.delete<void>(`${this.root}/${id}/experiences/${experienceId}`);
  }

  addEducation(id: string, education: EducationInput): Observable<Education> {
    return this.http.post<Education>(`${this.root}/${id}/education`, education);
  }

  updateEducation(
    id: string,
    educationId: string,
    education: EducationInput,
  ): Observable<Education> {
    return this.http.put<Education>(`${this.root}/${id}/education/${educationId}`, education);
  }

  deleteEducation(id: string, educationId: string): Observable<void> {
    return this.http.delete<void>(`${this.root}/${id}/education/${educationId}`);
  }
}
