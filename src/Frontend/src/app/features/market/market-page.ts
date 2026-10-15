import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Country, MarketInsight } from '../../models/market.model';
import { Resume } from '../../models/resume.model';
import { ResumeService } from '../resumes/resume.service';
import { MarketService } from './market.service';

const STORAGE_KEY = 'market.selectedCountries';
const DEFAULT_COUNTRIES = ['BR', 'IT', 'PT', 'ES', 'LU'];

@Component({
  selector: 'app-market-page',
  imports: [FormsModule, CurrencyPipe, DatePipe, DecimalPipe],
  templateUrl: './market-page.html',
  styleUrl: './market-page.scss',
})
export class MarketPage {
  private readonly api = inject(MarketService);
  private readonly resumeApi = inject(ResumeService);
  private readonly professionalId = environment.currentProfessionalId;

  protected readonly countries = signal<Country[]>([]);
  protected readonly resumes = signal<Resume[]>([]);
  protected readonly selectedCodes = signal<string[]>(this.restoreSelection());
  protected readonly resumeId = signal<string | null>(null);
  protected readonly insights = signal<MarketInsight[]>([]);
  protected readonly loading = signal(true);
  protected readonly refreshing = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly selectedCountries = computed(() =>
    this.selectedCodes().map((code) => ({
      code,
      name: this.countries().find((c) => c.code === code)?.name ?? code,
    })),
  );

  protected readonly availableCountries = computed(() =>
    this.countries().filter((c) => !this.selectedCodes().includes(c.code)),
  );

  protected readonly ranking = computed(() =>
    [...this.insights()].sort((a, b) => b.openJobs - a.openJobs),
  );

  protected readonly maxJobs = computed(() =>
    Math.max(1, ...this.insights().map((i) => i.openJobs)),
  );

  protected readonly lastSync = computed(
    () =>
      this.insights()
        .map((i) => i.lastSyncedAt)
        .sort()
        .at(-1) ?? null,
  );

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);
    forkJoin({
      countries: this.api.countries(),
      resumes: this.resumeApi.list(this.professionalId),
    }).subscribe({
      next: ({ countries, resumes }) => {
        this.countries.set(countries);
        this.resumes.set(resumes);
        this.loading.set(false);
        this.refresh();
      },
      error: () => {
        this.error.set('Não foi possível carregar os dados de mercado.');
        this.loading.set(false);
      },
    });
  }

  protected addCountry(code: string): void {
    if (!code) return;
    this.setSelection([...this.selectedCodes(), code]);
  }

  protected removeCountry(code: string): void {
    this.setSelection(this.selectedCodes().filter((c) => c !== code));
  }

  protected selectResume(id: string): void {
    this.resumeId.set(id || null);
    this.refresh();
  }

  protected refresh(): void {
    if (this.selectedCodes().length === 0) {
      this.insights.set([]);
      return;
    }
    this.refreshing.set(true);
    this.error.set(null);
    this.api.insights(this.professionalId, this.selectedCodes(), this.resumeId()).subscribe({
      next: (insights) => {
        this.insights.set(insights);
        this.refreshing.set(false);
      },
      error: () => {
        this.error.set('Não foi possível buscar os dados de mercado.');
        this.refreshing.set(false);
      },
    });
  }

  private setSelection(codes: string[]): void {
    this.selectedCodes.set(codes);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(codes));
    this.refresh();
  }

  private restoreSelection(): string[] {
    try {
      const stored: unknown = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? 'null');
      if (Array.isArray(stored) && stored.every((c) => typeof c === 'string')) return stored;
    } catch {
      // Corrupt value: fall back to the defaults.
    }
    return DEFAULT_COUNTRIES;
  }
}
