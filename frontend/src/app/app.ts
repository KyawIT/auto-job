import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastOutlet } from './layout/toast-outlet';
import { ConfirmDialog } from './shared/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastOutlet, ConfirmDialog],
  template: `
    <router-outlet />
    <app-toast-outlet />
    <app-confirm-dialog />
  `,
})
export class App {}
