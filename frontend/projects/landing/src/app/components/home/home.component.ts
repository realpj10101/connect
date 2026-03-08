import { Component, inject, OnInit, PLATFORM_ID } from '@angular/core';
import { ChatMessage } from '../../../models/chat-message.model';
import { ThemeService } from '../../../../../ui/src/public-api';
import { FeatureCard } from '../../../models/feature-card.model';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { RouterLink, RouterOutlet } from '@angular/router';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

@Component({
  selector: 'app-home',
  imports: [CommonModule, RouterLink, RouterOutlet],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  private _platfromId = inject(PLATFORM_ID);
  private _sanitizer = inject(DomSanitizer);

  themeService = inject(ThemeService);
  menuOpen = false;
  messages: ChatMessage[] = [];
  features: { logo: SafeHtml, title: string, desc: string }[] = [];
  systemUser = 'Alex';

  selectedTheme = '';

  ngOnInit(): void {
    this.loadDummyData();
  }

  loadDummyData(): void {
    this.messages = [
      {
        user: 'Sara',
        message: 'Hey everyone! How is your day going?',
        timeStamp: '10:30 AM'
      },
      {
        user: 'Mike',
        message: 'Pretty good! Just finished a big feature at work.',
        timeStamp: '10:32 AM'
      },
      {
        user: 'Alex',
        message: 'Nice! What where you working on?',
        timeStamp: '10:33 AM'
      }
    ];

    this.features = [
      {
        logo: this._sanitizer.bypassSecurityTrustHtml(`
        <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none"
        stroke="currentColor" stroke-width="2">
          <circle cx="12" cy="12" r="10" />
          <line x1="2" y1="12" x2="22" y2="12" />
          <path d="M12 2a15 15 0 0 1 0 20" />
          <path d="M12 2a15 15 0 0 0 0 20" />
        </svg>
      `),
        title: 'Public Rooms',
        desc: 'Open rooms where anyone can join the conversation. Share ideas and connect with the community.'
      },
      {
        logo: this._sanitizer.bypassSecurityTrustHtml(`
        <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none"
        stroke="currentColor" stroke-width="2">
          <rect x="3" y="11" width="18" height="11" rx="2" />
          <path d="M7 11V7a5 5 0 0 1 10 0v4" />
        </svg>
      `),
        title: 'Private Rooms',
        desc: 'Invite-only spaces with join requests. Keep your conversations secure and exclusive.'
      },
      {
        logo: this._sanitizer.bypassSecurityTrustHtml(`
       <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none"
stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
  <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/>
  <circle cx="9" cy="7" r="4"/>
  <path d="M22 21v-2a4 4 0 0 0-3-3.87"/>
  <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
</svg>
      `),
        title: 'Room Management',
        desc: 'Full control over your rooms. Manage members, approve requests, and customize settings.'
      },
      {
        logo: this._sanitizer.bypassSecurityTrustHtml(`
       <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none"
stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
  <path d="M21 15a4 4 0 0 1-4 4H8l-5 3V7a4 4 0 0 1 4-4h10a4 4 0 0 1 4 4z"/>
</svg>
      `),
        title: 'Real-time Chat',
        desc: 'Instant messaging with a clean, distraction-free interface built for focus.'
      },
      {
        logo: this._sanitizer.bypassSecurityTrustHtml(`
        <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none"
stroke="currentColor" stroke-width="2">
  <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"/>
</svg>
      `),
        title: 'Lightning Fast',
        desc: 'Built for speed. No lag, no delays, just seamless conversations.'
      },
    ];
  }

  getMessagesClasses(user: string): string {
    if (user == this.systemUser) {
      return 'system-message';
    }

    return 'other-user';
  }

  goToLogin(): void {
    if (isPlatformBrowser(this._platfromId))
      window.location.href = 'http://localhost:4200/sign-in'
  }

  goToDashboard(): void {
    if (isPlatformBrowser(this._platfromId))
      window.location.href = 'http://localhost:4200/'
  }
}
