// Mirrors backend Services/Settings/SettingsDtos.cs. Keys never come back, only whether they are set.

export type LetterLayout = 'Din5008' | 'International';
export const LETTER_LAYOUTS: readonly LetterLayout[] = ['Din5008', 'International'];

export interface UpdateSettingsRequest {
  defaultLetterLanguage: string;
  nimModel: string | null;
  letterLayout: LetterLayout;
  maxParallelGenerations: number;
  defaultSearchLocation: string | null;
  defaultSearchRadiusKm: number | null;
}

export interface Settings extends UpdateSettingsRequest {
  nimApiKeyConfigured: boolean;
  adzunaConfigured: boolean;
}

export const MAX_PARALLEL_GENERATIONS = 5;
