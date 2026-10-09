// Mirrors backend Services/Documents/DocumentDtos.cs.

export type DocumentType =
  'Cv' | 'DegreeCertificate' | 'EmploymentReference' | 'Certification' | 'Other';
export const DOCUMENT_TYPES: readonly DocumentType[] = [
  'Cv',
  'DegreeCertificate',
  'EmploymentReference',
  'Certification',
  'Other',
];

export type ExtractionStatus = 'Pending' | 'Running' | 'Done' | 'Failed' | 'NotSupported';

export interface AppDocument {
  id: number;
  type: DocumentType;
  displayName: string;
  originalFileName: string;
  mimeType: string;
  sizeBytes: number;
  extractionStatus: ExtractionStatus;
  attachByDefault: boolean;
  createdAt: string;
}

export interface UpdateDocumentRequest {
  displayName: string;
  type: DocumentType;
  attachByDefault: boolean;
}

export const ACCEPTED_FILES = '.pdf,.docx,.txt';
export const MAX_UPLOAD_MB = 20;
