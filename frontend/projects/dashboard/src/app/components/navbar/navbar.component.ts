import { Component, ElementRef, HostListener, inject, OnInit, Renderer2, ViewChild } from '@angular/core';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { AccountService } from '../../services/account.service';
import { LoggedInUser } from '../../models/logged-in-user.model';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-navbar',
  imports: [
    RouterModule, CommonModule
  ],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.scss'
})
export class NavbarComponent implements OnInit {
  @ViewChild('menuBtn') menuBtn!: ElementRef;
  @ViewChild('desktopBtn') desktopBtn!: ElementRef;
  @ViewChild('menuContainer') menuContainer!: ElementRef;

  accountService = inject(AccountService);
  router = inject(Router);

  isMenuOpen = false;
  isOpen = false;
  loggedInUser: LoggedInUser | undefined;

  ngOnInit(): void {
    let loggedInUserStr: string | null = localStorage.getItem('loggedInUser');

    if (loggedInUserStr)
      this.loggedInUser = JSON.parse(loggedInUserStr);

    this.router.events.subscribe(event => {
      if (event instanceof NavigationEnd) {
        this.isMenuOpen = false;
      }
    })
  }

  toggleMenu(): void {
    this.isMenuOpen = !this.isMenuOpen;
  }

  toggleDesktopMenuOpen(): void {
    this.isOpen = !this.isOpen;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    const clickedInsideMenu =
      this.menuContainer?.nativeElement.contains(event.target);

    const clickedMenuBtn =
      this.menuBtn?.nativeElement.contains(event.target);

    const clickedDesktopBtn =
      this.desktopBtn?.nativeElement.contains(event.target);

    if (clickedInsideMenu || clickedMenuBtn || clickedDesktopBtn) return;

    this.isMenuOpen = false;
  }
}
