export type ApplicationStage =
  'applied' | 'hr-call' | 'technical' | 'final-interview' | 'offer' | 'rejected';

export const APPLICATION_STAGES: readonly { key: ApplicationStage; label: string }[] = [
  { key: 'applied', label: 'Aplicado' },
  { key: 'hr-call', label: 'Stage 1 · RH Call' },
  { key: 'technical', label: 'Stage 2 · Code assessment' },
  { key: 'final-interview', label: 'Entrevista final' },
  { key: 'offer', label: 'Oferta' },
  { key: 'rejected', label: 'Recusado' },
];

export interface StageHistoryEntry {
  stage: ApplicationStage;
  detail: string;
  date: string;
}

export interface JobApplication {
  id: string;
  company: string;
  position: string;
  jobUrl: string | null;
  resumeId: string | null;
  stage: ApplicationStage;
  /** Free text about the current stage, e.g. "Code assessment submitted". */
  stageDetail: string;
  awaitingResponse: boolean;
  notes: string;
  appliedAt: string;
  updatedAt: string;
  history: StageHistoryEntry[];
}

export type JobApplicationInput = Pick<
  JobApplication,
  | 'company'
  | 'position'
  | 'jobUrl'
  | 'resumeId'
  | 'stage'
  | 'stageDetail'
  | 'awaitingResponse'
  | 'notes'
>;
