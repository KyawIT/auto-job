// Mirrors backend Services/Jobs/JobDtos.cs.
import type { ApplicationStatus } from '../applications/applications.models';

export type JobSource = 'Manual' | 'Adzuna' | 'BundesagenturFuerArbeit' | 'Scraper';
/** Sources the search endpoint accepts. */
export const SEARCHABLE_SOURCES: readonly JobSource[] = ['BundesagenturFuerArbeit', 'Adzuna'];

export type JobPostingState = 'New' | 'Seen' | 'Bookmarked' | 'Dismissed';
export type WorkMode = 'Unknown' | 'OnSite' | 'Hybrid' | 'Remote';
export const WORK_MODES: readonly WorkMode[] = ['Unknown', 'OnSite', 'Hybrid', 'Remote'];
export type EmploymentType =
  'Unknown' | 'FullTime' | 'PartTime' | 'Contract' | 'Internship' | 'Apprenticeship';
export const EMPLOYMENT_TYPES: readonly EmploymentType[] = [
  'Unknown',
  'FullTime',
  'PartTime',
  'Contract',
  'Internship',
  'Apprenticeship',
];

export interface JobSummary {
  id: number;
  title: string;
  companyName: string;
  location: string | null;
  source: JobSource;
  state: JobPostingState;
  workMode: WorkMode;
  postedAt: string | null;
  createdAt: string;
  applicationId: number | null;
  applicationStatus: ApplicationStatus | null;
}

export interface Job {
  id: number;
  title: string;
  companyName: string;
  location: string | null;
  description: string;
  url: string | null;
  salaryMin: number | null;
  salaryMax: number | null;
  salaryCurrency: string | null;
  employmentType: EmploymentType;
  workMode: WorkMode;
  contactEmail: string | null;
  contactName: string | null;
  source: JobSource;
  state: JobPostingState;
  postedAt: string | null;
  applicationId: number | null;
}

export interface CreateJobRequest {
  title: string;
  companyName: string;
  description: string;
  location: string | null;
  url: string | null;
  contactEmail: string | null;
  contactName: string | null;
  employmentType: EmploymentType;
  workMode: WorkMode;
}

/** Filters of the job list; "all" = everything except dismissed. */
export type JobFilter = 'all' | 'new' | 'bookmarked' | 'dismissed';

export interface SearchJobsRequest {
  keywords: string;
  location: string | null;
  radiusKm: number | null;
  sources: JobSource[];
}

export interface SearchResult {
  jobs: JobSummary[];
  newCount: number;
  errors: { source: JobSource; message: string }[];
}

export interface SaveSearchRequest extends SearchJobsRequest {
  name: string;
  refreshIntervalMinutes: number;
  isActive: boolean;
}

export interface SavedSearch extends SaveSearchRequest {
  id: number;
  lastRunAt: string | null;
}
