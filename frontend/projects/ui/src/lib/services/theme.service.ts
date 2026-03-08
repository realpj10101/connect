import { Injectable, signal } from '@angular/core';
import { themes } from '../theme';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private _selectedThemeSig = signal(themes[0]);

  getThemes(){
    return themes;
  }

  getSelectedTheme() {
    return this._selectedThemeSig();
  }

  setSelectedTheme(themeName: string) {
    const theme = themes.find(t => t.name === themeName);
    if (theme) {
      this._selectedThemeSig.set(theme);
    }
  }
}
