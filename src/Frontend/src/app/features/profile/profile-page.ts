import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
import { ProfileService } from './profile.service';

type DialogKind = 'details' | 'skill' | 'experience' | 'education';

/** Form model: technologies is edited as comma separated text and an empty end date means "current". */
type ExperienceDraft = Omit<ExperienceInput, 'technologies' | 'endDate'> & {
  technologies: string;
  endDate: string;
};

const upsert = <T extends { id: string }>(list: readonly T[], item: T): T[] =>
  list.some((i) => i.id === item.id)
    ? list.map((i) => (i.id === item.id ? item : i))
    : [...list, item];

@Component({
  selector: 'app-profile-page',
  imports: [FormsModule, DatePipe],
  templateUrl: './profile-page.html',
})
export class ProfilePage {
  private readonly api = inject(ProfileService);
  private readonly id = environment.currentProfessionalId;

  protected readonly profile = signal<Professional | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly saving = signal(false);

  protected readonly dialog = signal<DialogKind | null>(null);
  protected readonly editingId = signal<string | null>(null);
  protected readonly categoryFilter = signal('');

  protected details: ProfessionalDetails = this.blankDetails();
  protected skillDraft: SkillInput = this.blankSkill();
  protected experienceDraft: ExperienceDraft = this.blankExperience();
  protected educationDraft: EducationInput = this.blankEducation();

  protected readonly categories = computed(() =>
    [...new Set(this.profile()?.skills.map((s) => s.category) ?? [])].sort(),
  );

  protected readonly skills = computed(() => {
    const filter = this.categoryFilter();
    return (this.profile()?.skills ?? [])
      .filter((s) => !filter || s.category === filter)
      .sort(
        (a, b) =>
          Number(b.isPrimary) - Number(a.isPrimary) || b.yearsOfExperience - a.yearsOfExperience,
      );
  });

  protected readonly experiences = computed(() =>
    [...(this.profile()?.experiences ?? [])].sort((a, b) => b.startDate.localeCompare(a.startDate)),
  );

  protected readonly education = computed(() =>
    [...(this.profile()?.education ?? [])].sort((a, b) => b.startYear - a.startYear),
  );

  protected readonly initials = computed(() =>
    (this.profile()?.fullName ?? '')
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((n) => n[0])
      .join('')
      .toUpperCase(),
  );

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.get(this.id).subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Não foi possível carregar o perfil.');
        this.loading.set(false);
      },
    });
  }

  protected dots(level: number): string {
    return '●'.repeat(level) + '○'.repeat(Math.max(0, 5 - level));
  }

  protected closeDialog(): void {
    this.dialog.set(null);
    this.editingId.set(null);
  }

  // ---- details & photo

  protected openDetails(): void {
    const { fullName, headline, email, location, summary } = this.profile()!;
    this.details = { fullName, headline, email, location, summary };
    this.dialog.set('details');
  }

  protected saveDetails(): void {
    this.run(this.api.updateDetails(this.id, this.details), (updated) => this.profile.set(updated));
  }

  protected uploadPhoto(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.run(
      this.api.uploadPhoto(this.id, file),
      ({ photoUrl }) => this.profile.update((p) => p && { ...p, photoUrl }),
      false,
    );
  }

  // ---- skills

  protected openSkill(skill?: Skill): void {
    this.skillDraft = skill ? { ...skill } : this.blankSkill();
    this.editingId.set(skill?.id ?? null);
    this.dialog.set('skill');
  }

  protected saveSkill(): void {
    const id = this.editingId();
    const request = id
      ? this.api.updateSkill(this.id, id, this.skillDraft)
      : this.api.addSkill(this.id, this.skillDraft);
    this.run(request, (skill) =>
      this.profile.update((p) => p && { ...p, skills: upsert(p.skills, skill) }),
    );
  }

  protected deleteSkill(skill: Skill): void {
    if (!confirm(`Remover "${skill.name}"?`)) return;
    this.run(
      this.api.deleteSkill(this.id, skill.id),
      () =>
        this.profile.update(
          (p) => p && { ...p, skills: p.skills.filter((s) => s.id !== skill.id) },
        ),
      false,
    );
  }

  // ---- experiences

  protected openExperience(experience?: Experience): void {
    this.experienceDraft = experience
      ? {
          ...experience,
          endDate: experience.endDate ?? '',
          technologies: experience.technologies.join(', '),
        }
      : this.blankExperience();
    this.editingId.set(experience?.id ?? null);
    this.dialog.set('experience');
  }

  protected saveExperience(): void {
    const draft = this.experienceDraft;
    const input: ExperienceInput = {
      ...draft,
      endDate: draft.endDate || null,
      technologies: draft.technologies
        .split(',')
        .map((t) => t.trim())
        .filter(Boolean),
    };
    const id = this.editingId();
    const request = id
      ? this.api.updateExperience(this.id, id, input)
      : this.api.addExperience(this.id, input);
    this.run(request, (experience) =>
      this.profile.update((p) => p && { ...p, experiences: upsert(p.experiences, experience) }),
    );
  }

  protected deleteExperience(experience: Experience): void {
    if (!confirm(`Remover a experiência em "${experience.company}"?`)) return;
    this.run(
      this.api.deleteExperience(this.id, experience.id),
      () =>
        this.profile.update(
          (p) => p && { ...p, experiences: p.experiences.filter((e) => e.id !== experience.id) },
        ),
      false,
    );
  }

  // ---- education

  protected openEducation(education?: Education): void {
    this.educationDraft = education ? { ...education } : this.blankEducation();
    this.editingId.set(education?.id ?? null);
    this.dialog.set('education');
  }

  protected saveEducation(): void {
    const id = this.editingId();
    const request = id
      ? this.api.updateEducation(this.id, id, this.educationDraft)
      : this.api.addEducation(this.id, this.educationDraft);
    this.run(request, (education) =>
      this.profile.update((p) => p && { ...p, education: upsert(p.education, education) }),
    );
  }

  protected deleteEducation(education: Education): void {
    if (!confirm(`Remover "${education.degree}"?`)) return;
    this.run(
      this.api.deleteEducation(this.id, education.id),
      () =>
        this.profile.update(
          (p) => p && { ...p, education: p.education.filter((e) => e.id !== education.id) },
        ),
      false,
    );
  }

  /** Runs a request, applies its result and (optionally) closes the dialog. */
  private run<T>(request: Observable<T>, apply: (result: T) => void, closeDialog = true): void {
    this.saving.set(true);
    this.error.set(null);
    request.subscribe({
      next: (result) => {
        apply(result);
        this.saving.set(false);
        if (closeDialog) this.closeDialog();
      },
      error: () => {
        this.error.set('Não foi possível salvar. Tente novamente.');
        this.saving.set(false);
      },
    });
  }

  private blankDetails(): ProfessionalDetails {
    return { fullName: '', headline: '', email: '', location: '', summary: '' };
  }

  private blankSkill(): SkillInput {
    return { name: '', category: '', yearsOfExperience: 1, level: 3, isPrimary: false };
  }

  private blankExperience(): ExperienceDraft {
    return {
      company: '',
      role: '',
      startDate: '',
      endDate: '',
      description: '',
      technologies: '',
    };
  }

  private blankEducation(): EducationInput {
    return { institution: '', degree: '', startYear: new Date().getFullYear(), endYear: null };
  }
}
