import {
  HttpErrorResponse,
  HttpInterceptorFn,
  HttpRequest,
  HttpResponse,
} from '@angular/common/http';
import { Observable, delay, of, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApplicationStage, JobApplication } from '../../models/job-application.model';
import { MarketInsight } from '../../models/market.model';
import { Resume } from '../../models/resume.model';
import {
  mockApplications,
  mockCountries,
  mockMarketBase,
  mockProfessional,
  mockResumes,
} from './mock-data';

/**
 * Answers the API contract with in-memory data so the UI can be developed without the backend.
 * Only registered when `environment.useMocks` is true. State resets on page reload.
 */
const db = {
  professional: structuredClone(mockProfessional),
  resumes: structuredClone(mockResumes),
  applications: structuredClone(mockApplications),
};

type Body = Record<string, unknown>;

export const mockApiInterceptor: HttpInterceptorFn = (req, next) => {
  if (!environment.useMocks || !req.url.startsWith(environment.apiBaseUrl)) {
    return next(req);
  }

  const segments = req.url.slice(environment.apiBaseUrl.length).split('/').filter(Boolean);
  const result = route(req, segments);
  if (result === undefined) {
    return respond(new HttpErrorResponse({ status: 404, url: req.url }));
  }
  return respond(result);
};

function respond(result: unknown): Observable<never> | Observable<HttpResponse<unknown>> {
  if (result instanceof HttpErrorResponse) {
    return throwError(() => result).pipe(delay(200));
  }
  return of(new HttpResponse({ status: 200, body: result })).pipe(delay(250));
}

function route(req: HttpRequest<unknown>, segments: string[]): unknown {
  const [root, second, sub, subId, action] = segments;
  const body = req.body as Body;

  if (root === 'market' && second === 'countries') {
    return mockCountries;
  }
  if (root !== 'professionals') {
    return undefined;
  }

  switch (sub) {
    case undefined:
      if (req.method === 'GET') return db.professional;
      if (req.method === 'PUT') return Object.assign(db.professional, body);
      return undefined;
    case 'photo': {
      const photoUrl = URL.createObjectURL((body as unknown as FormData).get('file') as File);
      db.professional.photoUrl = photoUrl;
      return { photoUrl };
    }
    case 'skills':
      return collection(db.professional.skills, req.method, subId, body, (b) => b);
    case 'experiences':
      return collection(db.professional.experiences, req.method, subId, body, (b) => b);
    case 'education':
      return collection(db.professional.education, req.method, subId, body, (b) => b);
    case 'resumes':
      return resumes(req.method, subId, action, body);
    case 'applications':
      return applications(req.method, subId, body);
    case 'market-insights':
      return insights(req);
    default:
      return undefined;
  }
}

function collection<T extends { id: string }>(
  items: T[],
  method: string,
  id: string | undefined,
  body: Body,
  build: (body: Body, existing?: T) => Body,
): T | T[] | null | undefined {
  const index = items.findIndex((i) => i.id === id);
  switch (method) {
    case 'GET':
      return items;
    case 'POST': {
      const created = { ...build(body), id: crypto.randomUUID() } as unknown as T;
      items.push(created);
      return created;
    }
    case 'PUT': {
      if (index < 0) return undefined;
      items[index] = { ...build(body, items[index]), id: items[index].id } as unknown as T;
      return items[index];
    }
    case 'DELETE':
      if (index < 0) return undefined;
      items.splice(index, 1);
      return null;
    default:
      return undefined;
  }
}

function resumes(method: string, id: string | undefined, action: string | undefined, body: Body) {
  const touch = (b: Body) => ({ ...b, updatedAt: new Date().toISOString() });

  if (method === 'POST' && action === 'duplicate') {
    const source = db.resumes.find((r) => r.id === id);
    if (!source) return undefined;
    const copy: Resume = {
      ...structuredClone(source),
      id: crypto.randomUUID(),
      name: `${source.name}_copy`,
      updatedAt: new Date().toISOString(),
    };
    db.resumes.push(copy);
    return copy;
  }
  return collection(db.resumes, method, id, body, touch);
}

function applications(method: string, id: string | undefined, body: Body) {
  const now = new Date().toISOString();
  return collection(db.applications, method, id, body, (b, existing?: JobApplication) => {
    const stage = b['stage'] as ApplicationStage;
    const detail = (b['stageDetail'] as string) ?? '';
    const history = existing?.history ?? [];
    const changed = !existing || existing.stage !== stage || existing.stageDetail !== detail;
    return {
      ...b,
      appliedAt: existing?.appliedAt ?? now,
      updatedAt: changed ? now : existing.updatedAt,
      history: changed ? [...history, { stage, detail, date: now }] : history,
    };
  });
}

function insights(req: HttpRequest<unknown>): MarketInsight[] {
  const codes = (req.params.get('countries') ?? '').split(',').filter(Boolean);
  const resumeId = req.params.get('resumeId');
  // Gives each resume a stable, slightly different result so the picker visibly does something.
  const factor = resumeId ? 0.85 + (hash(resumeId) % 30) / 100 : 1;

  return codes
    .filter((code) => mockMarketBase[code])
    .map((code) => {
      const base = mockMarketBase[code];
      return {
        countryCode: code,
        countryName: mockCountries.find((c) => c.code === code)?.name ?? code,
        openJobs: Math.round(base.openJobs * factor),
        averageSalary: base.averageSalary,
        currency: base.currency,
        sources: ['LinkedIn', 'Glassdoor', 'Levels.fyi'],
        lastSyncedAt: new Date().toISOString(),
      };
    });
}

function hash(value: string): number {
  return [...value].reduce((acc, ch) => acc + ch.charCodeAt(0), 0);
}
