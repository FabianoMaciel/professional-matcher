import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable, forkJoin } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Professional } from '../../models/profile.model';
import { Resume, ResumeInput } from '../../models/resume.model';
import { ProfileService } from '../profile/profile.service';
import { ResumeService } from './resume.service';

/** A profile item that can be toggled on/off and reordered inside the resume editor. */
interface Selectable {
  id: string;
  label: string;
  detail: string;
  included: boolean;
}

/** Included items come first, in the order stored on the resume; the rest follow. */
const orderItems = (items: Selectable[], includedIds: string[]): Selectable[] => {
  const included = includedIds
    .map((id) => items.find((i) => i.id === id))
    .filter((i): i is Selectable => !!i)
    .map((i) => ({ ...i, included: true }));
  const rest = items
    .filter((i) => !includedIds.includes(i.id))
    .map((i) => ({ ...i, included: false }));
  return [...included, ...rest];
};

const move = (items: Selectable[], index: number, offset: -1 | 1): Selectable[] => {
  const target = index + offset;
  if (target < 0 || target >= items.length) return items;
  const copy = [...items];
  [copy[index], copy[target]] = [copy[target], copy[index]];
  return copy;
};

@Component({
  selector: 'app-resumes-page',
  imports: [FormsModule, DatePipe],
  templateUrl: './resumes-page.html',
  styleUrl: './resumes-page.scss',
})
export class ResumesPage {
  private readonly resumeApi = inject(ResumeService);
  private readonly profileApi = inject(ProfileService);
  private readonly professionalId = environment.currentProfessionalId;

  protected readonly profile = signal<Professional | null>(null);
  protected readonly resumes = signal<Resume[]>([]);
  protected readonly selectedId = signal<string | null>(null);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly dirty = signal(false);

  // Editor draft
  protected readonly name = signal('');
  protected readonly headline = signal('');
  protected readonly summary = signal('');
  protected readonly experienceItems = signal<Selectable[]>([]);
  protected readonly skillItems = signal<Selectable[]>([]);

  protected readonly selected = computed(
    () => this.resumes().find((r) => r.id === this.selectedId()) ?? null,
  );

  protected readonly previewExperiences = computed(() => {
    const experiences = this.profile()?.experiences ?? [];
    return this.experienceItems()
      .filter((i) => i.included)
      .map((i) => experiences.find((e) => e.id === i.id))
      .filter((e) => !!e);
  });

  protected readonly previewSkills = computed(() => this.skillItems().filter((i) => i.included));

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);
    forkJoin({
      profile: this.profileApi.get(this.professionalId),
      resumes: this.resumeApi.list(this.professionalId),
    }).subscribe({
      next: ({ profile, resumes }) => {
        this.profile.set(profile);
        this.resumes.set(resumes);
        this.loading.set(false);
        if (resumes.length) this.select(resumes[0]);
      },
      error: () => {
        this.error.set('Não foi possível carregar os currículos.');
        this.loading.set(false);
      },
    });
  }

  protected select(resume: Resume): void {
    const profile = this.profile()!;
    this.selectedId.set(resume.id);
    this.name.set(resume.name);
    this.headline.set(resume.headline);
    this.summary.set(resume.summary);
    this.experienceItems.set(
      orderItems(
        profile.experiences.map((e) => ({
          id: e.id,
          label: e.company,
          detail: e.role,
          included: false,
        })),
        resume.experienceIds,
      ),
    );
    this.skillItems.set(
      orderItems(
        profile.skills.map((s) => ({
          id: s.id,
          label: s.name,
          detail: s.category,
          included: false,
        })),
        resume.skillIds,
      ),
    );
    this.dirty.set(false);
  }

  protected selectById(id: string): void {
    const resume = this.resumes().find((r) => r.id === id);
    if (resume && (!this.dirty() || confirm('Descartar alterações não salvas?'))) {
      this.select(resume);
    }
  }

  protected setField(field: 'name' | 'headline' | 'summary', value: string): void {
    this[field].set(value);
    this.dirty.set(true);
  }

  protected toggle(list: 'experience' | 'skill', index: number): void {
    this.change(list, (items) =>
      items.map((item, i) => (i === index ? { ...item, included: !item.included } : item)),
    );
  }

  protected reorder(list: 'experience' | 'skill', index: number, offset: -1 | 1): void {
    this.change(list, (items) => move(items, index, offset));
  }

  protected create(): void {
    const name = prompt('Nome do novo currículo (ex.: Fabiano_CV_Backend):')?.trim();
    if (!name) return;
    const input: ResumeInput = { name, headline: '', summary: '', experienceIds: [], skillIds: [] };
    this.request(this.resumeApi.create(this.professionalId, input), (created) => {
      this.resumes.update((list) => [...list, created]);
      this.select(created);
    });
  }

  protected save(): void {
    const id = this.selectedId();
    if (!id) return;
    const input: ResumeInput = {
      name: this.name(),
      headline: this.headline(),
      summary: this.summary(),
      experienceIds: this.experienceItems()
        .filter((i) => i.included)
        .map((i) => i.id),
      skillIds: this.skillItems()
        .filter((i) => i.included)
        .map((i) => i.id),
    };
    this.request(this.resumeApi.update(this.professionalId, id, input), (updated) => {
      this.resumes.update((list) => list.map((r) => (r.id === updated.id ? updated : r)));
      this.dirty.set(false);
    });
  }

  protected duplicate(): void {
    const id = this.selectedId();
    if (!id) return;
    this.request(this.resumeApi.duplicate(this.professionalId, id), (copy) => {
      this.resumes.update((list) => [...list, copy]);
      this.select(copy);
    });
  }

  protected remove(): void {
    const resume = this.selected();
    if (!resume || !confirm(`Remover "${resume.name}"?`)) return;
    this.request(this.resumeApi.delete(this.professionalId, resume.id), () => {
      const remaining = this.resumes().filter((r) => r.id !== resume.id);
      this.resumes.set(remaining);
      if (remaining.length) {
        this.select(remaining[0]);
      } else {
        this.selectedId.set(null);
      }
    });
  }

  protected exportPdf(): void {
    const id = this.selectedId();
    if (!id) return;
    if (environment.useMocks) {
      window.print();
    } else {
      window.open(this.resumeApi.pdfUrl(this.professionalId, id), '_blank');
    }
  }

  private change(
    list: 'experience' | 'skill',
    update: (items: Selectable[]) => Selectable[],
  ): void {
    (list === 'experience' ? this.experienceItems : this.skillItems).update(update);
    this.dirty.set(true);
  }

  private request<T>(request: Observable<T>, apply: (result: T) => void): void {
    this.saving.set(true);
    this.error.set(null);
    request.subscribe({
      next: (result) => {
        apply(result);
        this.saving.set(false);
      },
      error: () => {
        this.error.set('Não foi possível concluir a operação. Tente novamente.');
        this.saving.set(false);
      },
    });
  }
}
