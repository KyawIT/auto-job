// Mirrors backend Services/Generation/BatchDtos.cs and Core/Models/BatchProgress.cs.

export type BatchStatus = 'Running' | 'Completed' | 'Cancelled';
export type GenerationTaskStatus = 'Queued' | 'Running' | 'Done' | 'Failed' | 'Cancelled';

export interface BatchProgress {
  total: number;
  queued: number;
  running: number;
  done: number;
  failed: number;
  cancelled: number;
}

export interface BatchTask {
  id: number;
  applicationId: number;
  jobTitle: string;
  companyName: string;
  status: GenerationTaskStatus;
  attempts: number;
  error: string | null;
}

export interface Batch {
  id: number;
  status: BatchStatus;
  createdAt: string;
  finishedAt: string | null;
  progress: BatchProgress;
  tasks: BatchTask[];
}
