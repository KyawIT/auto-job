import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Notifications } from '../../../core/notifications';
import { Confirm } from '../../../shared/confirm-dialog/confirm-dialog';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { PageHeader } from '../../../shared/page-header/page-header';
import { Sheet } from '../../../shared/sheet/sheet';
import { EducationForm } from '../components/education-form';
import { LanguageForm } from '../components/language-form';
import { PersonalDataForm } from '../components/personal-data-form';
import { SkillForm } from '../components/skill-form';
import { WorkExperienceForm } from '../components/work-experience-form';
import { ProfileApi } from '../profile.api';
import { Education, Language, ProfileSection, Skill, WorkExperience } from '../profile.models';

type Editing =
  | { section: 'work-experiences'; entry: WorkExperience | null }
  | { section: 'educations'; entry: Education | null }
  | { section: 'skills'; entry: Skill | null }
  | { section: 'languages'; entry: Language | null };

const sectionNames: Record<ProfileSection, string> = {
  'work-experiences': 'work experience',
  educations: 'education',
  skills: 'skill',
  languages: 'language',
};

@Component({
  selector: 'app-profile-page',
  imports: [
    DatePipe,
    RouterLink,
    PageHeader,
    EmptyState,
    Sheet,
    PersonalDataForm,
    WorkExperienceForm,
    EducationForm,
    SkillForm,
    LanguageForm,
  ],
  templateUrl: './profile-page.html',
  styleUrl: './profile-page.scss',
})
export default class ProfilePage {
  private readonly api = inject(ProfileApi);
  private readonly notifications = inject(Notifications);
  private readonly confirm = inject(Confirm);

  protected readonly profile = this.api.profileResource();
  protected readonly editing = signal<Editing | null>(null);
  protected readonly sheetHeading = computed(() => {
    const e = this.editing();
    return e ? `${e.entry ? 'Edit' : 'Add'} ${sectionNames[e.section]}` : '';
  });

  protected edit(editing: Editing): void {
    this.editing.set(editing);
  }

  protected closeSheet(open: boolean): void {
    if (!open) this.editing.set(null);
  }

  protected saved(): void {
    this.editing.set(null);
    this.profile.reload();
  }

  protected personalSaved(): void {
    this.notifications.success('Profile saved.');
    this.profile.reload();
  }

  protected async remove(section: ProfileSection, id: number, name: string): Promise<void> {
    const confirmed = await this.confirm.ask({
      title: `Delete ${sectionNames[section]}?`,
      message: `"${name}" will be removed from your profile.`,
      confirmLabel: 'Delete',
      danger: true,
    });
    if (!confirmed) return;
    await this.api.deleteEntry(section, id);
    this.profile.reload();
  }
}
