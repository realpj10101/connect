import { Component, inject, OnInit } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ThemeService } from '../../../ui/src/public-api';
import { ChatMessage } from '../models/chat-message.model';
import { FeatureCard } from '../models/feature-card.model';
import { RegisterComponent } from "../../../dashboard/src/app/components/account/register/register.component";

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CommonModule, RouterLink, RegisterComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
}


// toggleMenu() {
//   this.menuOpen = !this.menuOpen;
// }

// ngOnInit(): void {
//   const theme = this.themeService.getSelectedTheme();
//   this.selectedTheme = theme.name;
// }

// changeTheme(themeName: string) {
//   this.themeService.setSelectedTheme(themeName);
//   this.selectedTheme = themeName;
// }
