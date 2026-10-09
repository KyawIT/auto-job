import { Component, inject, input, linkedSignal, output } from '@angular/core';
import { email, form, FormRoot, required } from '@angular/forms/signals';
import { serverErrors } from '../../../core/api-error';
import { FIELD_DIRECTIVES } from '../../../shared/field-errors/field-errors';
import { orNull } from '../../../shared/labels';
import { ProfileApi } from '../profile.api';
import { Profile, SaveProfileRequest } from '../profile.models';

type PersonalData = { [K in keyof SaveProfileRequest]: string };

function toModel(profile: Profile | null | undefined): PersonalData {
  return {
    firstName: profile?.firstName ?? '',
    lastName: profile?.lastName ?? '',
    email: profile?.email ?? '',
    phone: profile?.phone ?? '',
    street: profile?.street ?? '',
    postalCode: profile?.postalCode ?? '',
    city: profile?.city ?? '',
    country: profile?.country ?? '',
    dateOfBirth: profile?.dateOfBirth ?? '',
    summary: profile?.summary ?? '',
    desiredPosition: profile?.desiredPosition ?? '',
  };
}

function toRequest(model: PersonalData): SaveProfileRequest {
  return {
    firstName: model.firstName.trim(),
    lastName: model.lastName.trim(),
    email: model.email.trim(),
    phone: orNull(model.phone),
    street: orNull(model.street),
    postalCode: orNull(model.postalCode),
    city: orNull(model.city),
    country: orNull(model.country),
    dateOfBirth: orNull(model.dateOfBirth),
    summary: orNull(model.summary),
    desiredPosition: orNull(model.desiredPosition),
  };
}

/** Personal data of the profile. Used by the setup wizard (step 1) and the profile page. */
@Component({
  selector: 'app-personal-data-form',
  imports: [FormRoot, ...FIELD_DIRECTIVES],
  templateUrl: './personal-data-form.html',
})
export class PersonalDataForm {
  private readonly api = inject(ProfileApi);

  readonly profile = input<Profile | null>();
  readonly submitLabel = input('Save');
  readonly saved = output<Profile>();

  protected readonly model = linkedSignal(() => toModel(this.profile()));
  protected readonly form = form(
    this.model,
    (p) => {
      required(p.firstName);
      required(p.lastName);
      required(p.email);
      email(p.email);
    },
    {
      submission: {
        action: async (f) => {
          try {
            const profile = await this.api.save(toRequest(f().value()));
            f().reset(toModel(profile));
            this.saved.emit(profile);
            return undefined;
          } catch (error) {
            return serverErrors(f, error);
          }
        },
      },
    },
  );
}
