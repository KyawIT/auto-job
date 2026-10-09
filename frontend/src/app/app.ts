import { Component } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { RouterOutlet } from '@angular/router';

@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly health = httpResource<{ status: string }>(() => '/health');
}
