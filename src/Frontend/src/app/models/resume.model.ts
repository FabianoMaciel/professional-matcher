export interface Resume {
  id: string;
  name: string;
  headline: string;
  summary: string;
  /** Ids of the profile experiences included, in display order. */
  experienceIds: string[];
  /** Ids of the profile skills included, in display order. */
  skillIds: string[];
  updatedAt: string;
}

export type ResumeInput = Omit<Resume, 'id' | 'updatedAt'>;
