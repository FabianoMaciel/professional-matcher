export interface Country {
  code: string;
  name: string;
}

export interface MarketInsight {
  countryCode: string;
  countryName: string;
  openJobs: number;
  /** Yearly or monthly, depending on the country; always in `currency`. */
  averageSalary: number | null;
  currency: string;
  sources: string[];
  lastSyncedAt: string;
}
