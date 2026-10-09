import type { ApplicationDetails } from '../app/features/applications/applications.models';
import type { Batch } from '../app/features/batches/batches.models';
import type { AppDocument } from '../app/features/documents/documents.models';
import type { Job, JobSummary } from '../app/features/jobs/jobs.models';
import type { Profile } from '../app/features/profile/profile.models';
import type { Settings } from '../app/features/settings/settings.models';

export function aProfile(overrides: Partial<Profile> = {}): Profile {
  return {
    id: 1,
    firstName: 'Ada',
    lastName: 'Lovelace',
    email: 'ada@example.com',
    phone: null,
    street: null,
    postalCode: null,
    city: 'London',
    country: null,
    dateOfBirth: null,
    summary: null,
    desiredPosition: 'Engineer',
    workExperiences: [
      {
        id: 7,
        company: 'Analytical Engines',
        role: 'Programmer',
        location: null,
        startDate: '2020-01-01',
        endDate: null,
        description: null,
      },
    ],
    educations: [],
    skills: [{ id: 3, name: 'Mathematics', level: 'Expert' }],
    languages: [],
    ...overrides,
  };
}

export function someSettings(overrides: Partial<Settings> = {}): Settings {
  return {
    defaultLetterLanguage: 'de',
    nimModel: null,
    letterLayout: 'Din5008',
    maxParallelGenerations: 2,
    defaultSearchLocation: 'Berlin',
    defaultSearchRadiusKm: 25,
    nimApiKeyConfigured: false,
    adzunaConfigured: false,
    ...overrides,
  };
}

export function aDocument(overrides: Partial<AppDocument> = {}): AppDocument {
  return {
    id: 5,
    type: 'Cv',
    displayName: 'My CV',
    originalFileName: 'cv.pdf',
    mimeType: 'application/pdf',
    sizeBytes: 120_000,
    extractionStatus: 'Done',
    attachByDefault: true,
    createdAt: '2026-10-01T10:00:00Z',
    ...overrides,
  };
}

export function aJobSummary(overrides: Partial<JobSummary> = {}): JobSummary {
  return {
    id: 11,
    title: 'Frontend Developer',
    companyName: 'Acme',
    location: 'Berlin',
    source: 'BundesagenturFuerArbeit',
    state: 'New',
    workMode: 'Hybrid',
    postedAt: '2026-10-05T00:00:00Z',
    createdAt: '2026-10-06T00:00:00Z',
    applicationId: null,
    applicationStatus: null,
    ...overrides,
  };
}

export function aJob(overrides: Partial<Job> = {}): Job {
  return {
    id: 11,
    title: 'Frontend Developer',
    companyName: 'Acme',
    location: 'Berlin',
    description: 'Build great UIs.',
    url: 'https://example.com/job/11',
    salaryMin: null,
    salaryMax: null,
    salaryCurrency: null,
    employmentType: 'FullTime',
    workMode: 'Hybrid',
    contactEmail: null,
    contactName: null,
    source: 'BundesagenturFuerArbeit',
    state: 'Seen',
    postedAt: null,
    applicationId: null,
    ...overrides,
  };
}

export function anApplication(overrides: Partial<ApplicationDetails> = {}): ApplicationDetails {
  return {
    id: 21,
    status: 'Draft',
    language: 'de',
    recipientEmail: null,
    recipientName: null,
    notes: null,
    sentAt: null,
    responseAt: null,
    createdAt: '2026-10-07T00:00:00Z',
    job: aJobSummary({ applicationId: 21, applicationStatus: 'Draft', state: 'Seen' }),
    attachments: [],
    texts: [],
    events: [{ type: 'Created', occurredAt: '2026-10-07T00:00:00Z', note: null }],
    ...overrides,
  };
}

export function withLetter(
  application: ApplicationDetails,
  letter = 'Dear Acme, …',
): ApplicationDetails {
  const base = {
    language: application.language,
    model: 'meta/llama',
    isEdited: false,
    isCurrent: true,
    createdAt: '2026-10-08T00:00:00Z',
  };
  return {
    ...application,
    texts: [
      { ...base, id: 101, kind: 'CoverLetter', content: letter },
      { ...base, id: 102, kind: 'EmailSubject', content: 'Application' },
      { ...base, id: 103, kind: 'EmailBody', content: 'Please find attached…' },
    ],
  };
}

export function aBatch(overrides: Partial<Batch> = {}): Batch {
  return {
    id: 31,
    status: 'Running',
    createdAt: '2026-10-08T00:00:00Z',
    finishedAt: null,
    progress: { total: 3, queued: 1, running: 1, done: 1, failed: 0, cancelled: 0 },
    tasks: [
      {
        id: 1,
        applicationId: 21,
        jobTitle: 'Frontend Developer',
        companyName: 'Acme',
        status: 'Done',
        attempts: 1,
        error: null,
      },
      {
        id: 2,
        applicationId: 22,
        jobTitle: 'UI Engineer',
        companyName: 'Globex',
        status: 'Running',
        attempts: 1,
        error: null,
      },
      {
        id: 3,
        applicationId: 23,
        jobTitle: 'Designer',
        companyName: 'Initech',
        status: 'Queued',
        attempts: 0,
        error: null,
      },
    ],
    ...overrides,
  };
}
