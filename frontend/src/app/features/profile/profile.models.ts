// Mirrors backend Services/Profiles/ProfileDtos.cs. Dates are ISO "yyyy-MM-dd" strings.

export type SkillLevel = 'Basic' | 'Intermediate' | 'Advanced' | 'Expert';
export const SKILL_LEVELS: readonly SkillLevel[] = ['Basic', 'Intermediate', 'Advanced', 'Expert'];

export type LanguageLevel = 'A1' | 'A2' | 'B1' | 'B2' | 'C1' | 'C2' | 'Native';
export const LANGUAGE_LEVELS: readonly LanguageLevel[] = [
  'A1',
  'A2',
  'B1',
  'B2',
  'C1',
  'C2',
  'Native',
];

export interface SaveProfileRequest {
  firstName: string;
  lastName: string;
  email: string;
  phone: string | null;
  street: string | null;
  postalCode: string | null;
  city: string | null;
  country: string | null;
  dateOfBirth: string | null;
  summary: string | null;
  desiredPosition: string | null;
}

export interface Profile extends SaveProfileRequest {
  id: number;
  workExperiences: WorkExperience[];
  educations: Education[];
  skills: Skill[];
  languages: Language[];
}

export interface WorkExperienceRequest {
  company: string;
  role: string;
  location: string | null;
  startDate: string;
  endDate: string | null;
  description: string | null;
}

export interface WorkExperience extends WorkExperienceRequest {
  id: number;
}

export interface EducationRequest {
  institution: string;
  degree: string;
  fieldOfStudy: string | null;
  grade: string | null;
  startDate: string | null;
  endDate: string | null;
}

export interface Education extends EducationRequest {
  id: number;
}

export interface SkillRequest {
  name: string;
  level: SkillLevel | null;
}

export interface Skill extends SkillRequest {
  id: number;
}

export interface LanguageRequest {
  name: string;
  level: LanguageLevel;
}

export interface Language extends LanguageRequest {
  id: number;
}

/** The four list sections of a profile and their API path segment. */
export type ProfileSection = 'work-experiences' | 'educations' | 'skills' | 'languages';
