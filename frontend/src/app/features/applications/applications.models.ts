// Mirrors backend Services/Applications/ApplicationDtos.cs and Services/Generation/BatchDtos.cs.
import type { DocumentType } from '../documents/documents.models';
import type { JobSummary } from '../jobs/jobs.models';

export type ApplicationStatus =
  'Draft' | 'Ready' | 'Sent' | 'Interview' | 'Offer' | 'Rejected' | 'Withdrawn';
export const APPLICATION_STATUSES: readonly ApplicationStatus[] = [
  'Draft',
  'Ready',
  'Sent',
  'Interview',
  'Offer',
  'Rejected',
  'Withdrawn',
];

export type GeneratedTextKind = 'CoverLetter' | 'EmailSubject' | 'EmailBody';

export type ApplicationEventType =
  | 'Created'
  | 'Generated'
  | 'Edited'
  | 'Exported'
  | 'EmailOpened'
  | 'Sent'
  | 'ResponseReceived'
  | 'Interview'
  | 'StatusChanged'
  | 'Note';

export interface ApplicationSummary {
  id: number;
  jobPostingId: number;
  jobTitle: string;
  companyName: string;
  status: ApplicationStatus;
  language: string;
  sentAt: string | null;
  updatedAt: string;
}

export interface Attachment {
  documentId: number;
  displayName: string;
  type: DocumentType;
  fileName: string;
}

export interface GeneratedText {
  id: number;
  kind: GeneratedTextKind;
  language: string;
  content: string;
  model: string | null;
  isEdited: boolean;
  isCurrent: boolean;
  createdAt: string;
}

export interface ApplicationEvent {
  type: ApplicationEventType;
  occurredAt: string;
  note: string | null;
}

export interface ApplicationDetails {
  id: number;
  status: ApplicationStatus;
  language: string;
  recipientEmail: string | null;
  recipientName: string | null;
  notes: string | null;
  sentAt: string | null;
  responseAt: string | null;
  createdAt: string;
  job: JobSummary;
  attachments: Attachment[];
  texts: GeneratedText[];
  events: ApplicationEvent[];
}

export interface UpdateApplicationRequest {
  language: string;
  recipientEmail: string | null;
  recipientName: string | null;
  notes: string | null;
}

export type ExportFormat = 'Pdf' | 'Docx';

export interface EmailDraft {
  relativePath: string;
  fullPath: string;
}
