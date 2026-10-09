/** Display labels for backend enum values that don't read well when split ("BundesagenturFuerArbeit"). */
const overrides: Record<string, string> = {
  BundesagenturFuerArbeit: 'Bundesagentur für Arbeit',
  Cv: 'CV',
  Din5008: 'DIN 5008 (German)',
  NotSupported: 'Not supported',
  OnSite: 'On-site',
  FullTime: 'Full-time',
  PartTime: 'Part-time',
  EmailOpened: 'Email draft opened',
  ResponseReceived: 'Response received',
  StatusChanged: 'Status changed',
  CoverLetter: 'Cover letter',
  EmailSubject: 'Email subject',
  EmailBody: 'Email body',
  DegreeCertificate: 'Degree certificate',
  EmploymentReference: 'Employment reference',
};

/** "NotSupported" → "Not supported" */
export function label(value: string | null | undefined): string {
  if (!value) return '';
  if (overrides[value]) return overrides[value];
  const words = value.replace(/([a-z])([A-Z])/g, '$1 $2');
  return words[0] + words.slice(1).toLowerCase();
}

/** Languages offered for letters; the backend stores the ISO code. */
export const LETTER_LANGUAGES = [
  { code: 'de', name: 'German' },
  { code: 'en', name: 'English' },
] as const;

export function languageName(code: string | null | undefined): string {
  return LETTER_LANGUAGES.find((l) => l.code === code)?.name ?? (code ?? '').toUpperCase();
}

/** '' → null, trimmed otherwise. Text inputs work with strings; the API wants null for "not set". */
export function orNull(value: string | null | undefined): string | null {
  const trimmed = value?.trim();
  return trimmed ? trimmed : null;
}

export function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}
