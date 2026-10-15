export interface Skill {
  id: string;
  name: string;
  category: string;
  yearsOfExperience: number;
  /** 1 (basic) to 5 (expert). */
  level: number;
  isPrimary: boolean;
}

export interface Experience {
  id: string;
  company: string;
  role: string;
  /** Format: yyyy-MM */
  startDate: string;
  /** Format: yyyy-MM. Null when it is the current position. */
  endDate: string | null;
  description: string;
  technologies: string[];
}

export interface Education {
  id: string;
  institution: string;
  degree: string;
  startYear: number;
  endYear: number | null;
}

export interface Professional {
  id: string;
  fullName: string;
  headline: string;
  email: string;
  location: string;
  summary: string;
  photoUrl: string | null;
  skills: Skill[];
  experiences: Experience[];
  education: Education[];
}

export type ProfessionalDetails = Pick<
  Professional,
  'fullName' | 'headline' | 'email' | 'location' | 'summary'
>;
export type SkillInput = Omit<Skill, 'id'>;
export type ExperienceInput = Omit<Experience, 'id'>;
export type EducationInput = Omit<Education, 'id'>;
