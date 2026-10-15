import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable, forkJoin } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  APPLICATION_STAGES,
  ApplicationStage,
  JobApplication,
  JobApplicationInput,
} from '../../models/job-application.model';
import { Resume } from '../../models/resume.model';
import { ResumeService } from '../resumes/resume.service';
import { ApplicationService } from './application.service';

type ViewMode = 'kanban' | 'table';

@Component({
  selector: 'app-applications-page',
  imports: [FormsModule, DatePipe],
  templateUrl: './applications-page.html',
  styleUrl: './applications-page.scss',
})
export class ApplicationsPage {
  private readonly api = inject(ApplicationService);
  private readonly resumeApi = inject(ResumeService);
  private readonly professionalId = environment.currentProfessionalId;

  protected readonly stages = APPLICATION_STAGES;

  protected readonly applications = signal<JobApplication[]>([]);
  protected readonly resumes = signal<Resume[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly view = signal<ViewMode>('kanban');
  protected readonly stageFilter = signal<ApplicationStage | ''>('');
  protected readonly onlyAwaiting = signal(false);

  protected readonly dialogOpen = signal(false);
  protected readonly editing = signal<JobApplication | null>(null);
  protected draft: JobApplicationInput = this.blank();

  private draggedId: string | null = null;

  protected readonly filtered = computed(() =>
    this.applications()
      .filter((a) => !this.stageFilter() || a.stage === this.stageFilter())
      .filter((a) => !this.onlyAwaiting() || a.awaitingResponse)
      .sort((a, b) => b.updatedAt.localeCompare(a.updatedAt)),
  );

  protected readonly columns = computed(() =>
    this.stages.map((stage) => ({
      ...stage,
      items: this.filtered().filter((a) => a.stage === stage.key),
    })),
  );

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);
    forkJoin({
      applications: this.api.list(this.professionalId),
      resumes: this.resumeApi.list(this.professionalId),
    }).subscribe({
      next: ({ applications, resumes }) => {
        this.applications.set(applications);
        this.resumes.set(resumes);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Não foi possível carregar as aplicações.');
        this.loading.set(false);
      },
    });
  }

  protected resumeName(id: string | null): string {
    return this.resumes().find((r) => r.id === id)?.name ?? '—';
  }

  protected stageLabel(stage: ApplicationStage): string {
    return this.stages.find((s) => s.key === stage)?.label ?? stage;
  }

  protected openNew(): void {
    this.draft = this.blank();
    this.editing.set(null);
    this.dialogOpen.set(true);
  }

  protected open(application: JobApplication): void {
    const { company, position, jobUrl, resumeId, stage, stageDetail, awaitingResponse, notes } =
      application;
    this.draft = {
      company,
      position,
      jobUrl,
      resumeId,
      stage,
      stageDetail,
      awaitingResponse,
      notes,
    };
    this.editing.set(application);
    this.dialogOpen.set(true);
  }

  protected close(): void {
    this.dialogOpen.set(false);
    this.editing.set(null);
  }

  protected save(): void {
    const current = this.editing();
    const request = current
      ? this.api.update(this.professionalId, current.id, this.draft)
      : this.api.create(this.professionalId, this.draft);
    this.run(request, (saved) => {
      this.applications.update((list) =>
        list.some((a) => a.id === saved.id)
          ? list.map((a) => (a.id === saved.id ? saved : a))
          : [...list, saved],
      );
      this.close();
    });
  }

  protected remove(): void {
    const current = this.editing();
    if (!current || !confirm(`Remover a aplicação em "${current.company}"?`)) return;
    this.run(this.api.delete(this.professionalId, current.id), () => {
      this.applications.update((list) => list.filter((a) => a.id !== current.id));
      this.close();
    });
  }

  // ---- kanban drag & drop

  protected dragStart(application: JobApplication): void {
    this.draggedId = application.id;
  }

  protected drop(stage: ApplicationStage): void {
    const application = this.applications().find((a) => a.id === this.draggedId);
    this.draggedId = null;
    if (!application || application.stage === stage) return;

    const { company, position, jobUrl, resumeId, awaitingResponse, notes } = application;
    const input: JobApplicationInput = {
      company,
      position,
      jobUrl,
      resumeId,
      awaitingResponse,
      notes,
      stage,
      stageDetail: '',
    };
    this.run(this.api.update(this.professionalId, application.id, input), (saved) =>
      this.applications.update((list) => list.map((a) => (a.id === saved.id ? saved : a))),
    );
  }

  private run<T>(request: Observable<T>, apply: (result: T) => void): void {
    this.saving.set(true);
    this.error.set(null);
    request.subscribe({
      next: (result) => {
        apply(result);
        this.saving.set(false);
      },
      error: () => {
        this.error.set('Não foi possível salvar. Tente novamente.');
        this.saving.set(false);
      },
    });
  }

  private blank(): JobApplicationInput {
    return {
      company: '',
      position: '',
      jobUrl: null,
      resumeId: null,
      stage: 'applied',
      stageDetail: '',
      awaitingResponse: false,
      notes: '',
    };
  }
}
