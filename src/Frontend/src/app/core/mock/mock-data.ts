import { JobApplication } from '../../models/job-application.model';
import { Country } from '../../models/market.model';
import { Professional } from '../../models/profile.model';
import { Resume } from '../../models/resume.model';

const skill = (
  id: string,
  name: string,
  category: string,
  yearsOfExperience: number,
  level: number,
  isPrimary: boolean,
) => ({ id, name, category, yearsOfExperience, level, isPrimary });

export const mockProfessional: Professional = {
  id: '00000000-0000-0000-0000-000000000001',
  fullName: 'Fabiano Maciel',
  headline: 'Senior Software Engineer · .NET',
  email: 'fmaciel@glasslewis.com',
  location: 'Brasil',
  summary: 'Mais de 15 anos com .NET, Azure e bancos relacionais.',
  photoUrl: null,
  skills: [
    skill('s1', '.NET C# (MVC, WebAPI)', 'Backend', 15, 5, true),
    skill('s2', 'WebForms', 'Backend', 12, 4, true),
    skill('s3', 'Azure', 'Cloud', 7, 4, true),
    skill('s4', 'SQL Server', 'Banco', 15, 5, true),
    skill('s5', 'Oracle', 'Banco', 8, 4, true),
    skill('s6', 'PostgreSQL', 'Banco', 5, 4, true),
    skill('s7', 'MongoDB', 'Banco', 4, 3, true),
    skill('s8', 'Angular', 'Frontend', 3, 3, false),
    skill('s9', 'Knockout.js', 'Frontend', 4, 3, false),
    skill('s10', 'Python', 'Backend', 2, 2, false),
    skill('s11', 'Java', 'Backend', 2, 2, false),
    skill('s12', 'Microsserviços', 'Arquitetura', 6, 4, false),
    skill('s13', 'IIS', 'Outros', 12, 4, false),
    skill('s14', 'Reporting', 'Outros', 10, 4, false),
    skill('s15', 'Stored Procedures', 'Banco', 15, 4, false),
  ],
  experiences: [
    {
      id: 'e1',
      company: 'Glass Lewis',
      role: 'Software Engineer',
      startDate: '2020-01',
      endDate: null,
      description: 'APIs .NET em Azure com PostgreSQL.',
      technologies: ['.NET', 'Azure', 'PostgreSQL'],
    },
    {
      id: 'e2',
      company: 'Empresa anterior',
      role: 'Desenvolvedor Full Stack',
      startDate: '2012-03',
      endDate: '2019-12',
      description: 'Sistemas internos em WebForms e MVC.',
      technologies: ['WebForms', 'MVC', 'Oracle'],
    },
  ],
  education: [
    {
      id: 'ed1',
      institution: 'Universidade X',
      degree: 'Bacharelado em Sistemas de Informação',
      startYear: 2005,
      endYear: 2009,
    },
  ],
};

export const mockResumes: Resume[] = [
  {
    id: 'r1',
    name: 'Fabiano_CV_Backend',
    headline: 'Backend Engineer · .NET',
    summary: 'Engenheiro backend com foco em .NET e bancos relacionais.',
    experienceIds: ['e1', 'e2'],
    skillIds: ['s1', 's4', 's6', 's5'],
    updatedAt: '2026-09-20T12:00:00Z',
  },
  {
    id: 'r2',
    name: 'Fabiano_CV_BackendAzure',
    headline: 'Backend Engineer · Azure',
    summary: 'Engenheiro backend com foco em .NET e Azure.',
    experienceIds: ['e1'],
    skillIds: ['s1', 's3', 's6', 's4', 's12'],
    updatedAt: '2026-10-01T12:00:00Z',
  },
  {
    id: 'r3',
    name: 'Fabiano_CV_Fullstack',
    headline: 'Full Stack Engineer',
    summary: 'Engenheiro full stack com .NET e Angular.',
    experienceIds: ['e1', 'e2'],
    skillIds: ['s1', 's8', 's9', 's4'],
    updatedAt: '2026-08-11T12:00:00Z',
  },
];

export const mockApplications: JobApplication[] = [
  {
    id: 'a1',
    company: 'Empresa X',
    position: 'Sr Backend',
    jobUrl: null,
    resumeId: 'r1',
    stage: 'technical',
    stageDetail: 'Code assessment enviado',
    awaitingResponse: true,
    notes: '',
    appliedAt: '2026-09-25T12:00:00Z',
    updatedAt: '2026-10-10T12:00:00Z',
    history: [
      { stage: 'applied', detail: '', date: '2026-09-25T12:00:00Z' },
      { stage: 'hr-call', detail: 'Call com RH', date: '2026-09-30T12:00:00Z' },
      { stage: 'technical', detail: 'Code assessment enviado', date: '2026-10-10T12:00:00Z' },
    ],
  },
  {
    id: 'a2',
    company: 'Empresa Z',
    position: 'Fullstack',
    jobUrl: null,
    resumeId: 'r3',
    stage: 'hr-call',
    stageDetail: 'Call com RH realizada',
    awaitingResponse: true,
    notes: '',
    appliedAt: '2026-10-02T12:00:00Z',
    updatedAt: '2026-10-08T12:00:00Z',
    history: [
      { stage: 'applied', detail: '', date: '2026-10-02T12:00:00Z' },
      { stage: 'hr-call', detail: 'Call com RH realizada', date: '2026-10-08T12:00:00Z' },
    ],
  },
  {
    id: 'a3',
    company: 'Empresa A',
    position: 'Sr .NET Dev',
    jobUrl: null,
    resumeId: 'r1',
    stage: 'applied',
    stageDetail: '',
    awaitingResponse: false,
    notes: '',
    appliedAt: '2026-10-12T12:00:00Z',
    updatedAt: '2026-10-12T12:00:00Z',
    history: [{ stage: 'applied', detail: '', date: '2026-10-12T12:00:00Z' }],
  },
  {
    id: 'a4',
    company: 'Empresa C',
    position: 'Tech Lead',
    jobUrl: null,
    resumeId: 'r2',
    stage: 'rejected',
    stageDetail: 'Sem match de senioridade',
    awaitingResponse: false,
    notes: '',
    appliedAt: '2026-09-01T12:00:00Z',
    updatedAt: '2026-09-15T12:00:00Z',
    history: [
      { stage: 'applied', detail: '', date: '2026-09-01T12:00:00Z' },
      { stage: 'rejected', detail: 'Sem match de senioridade', date: '2026-09-15T12:00:00Z' },
    ],
  },
];

export const mockCountries: Country[] = [
  { code: 'BR', name: 'Brasil' },
  { code: 'IT', name: 'Itália' },
  { code: 'PT', name: 'Portugal' },
  { code: 'ES', name: 'Espanha' },
  { code: 'LU', name: 'Luxemburgo' },
  { code: 'DE', name: 'Alemanha' },
  { code: 'NL', name: 'Países Baixos' },
  { code: 'IE', name: 'Irlanda' },
  { code: 'FR', name: 'França' },
  { code: 'GB', name: 'Reino Unido' },
  { code: 'CA', name: 'Canadá' },
  { code: 'US', name: 'Estados Unidos' },
];

export const mockMarketBase: Record<
  string,
  { openJobs: number; averageSalary: number; currency: string }
> = {
  BR: { openJobs: 5248, averageSalary: 14500, currency: 'BRL' },
  IT: { openJobs: 489, averageSalary: 45000, currency: 'EUR' },
  PT: { openJobs: 321, averageSalary: 42000, currency: 'EUR' },
  ES: { openJobs: 593, averageSalary: 48000, currency: 'EUR' },
  LU: { openJobs: 101, averageSalary: 70000, currency: 'EUR' },
  DE: { openJobs: 2310, averageSalary: 68000, currency: 'EUR' },
  NL: { openJobs: 860, averageSalary: 65000, currency: 'EUR' },
  IE: { openJobs: 540, averageSalary: 70000, currency: 'EUR' },
  FR: { openJobs: 1480, averageSalary: 52000, currency: 'EUR' },
  GB: { openJobs: 3120, averageSalary: 62000, currency: 'GBP' },
  CA: { openJobs: 1750, averageSalary: 105000, currency: 'CAD' },
  US: { openJobs: 9800, averageSalary: 135000, currency: 'USD' },
};
