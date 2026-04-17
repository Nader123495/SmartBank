import { Injectable, signal, computed } from '@angular/core';

const STORAGE_KEY = 'smartbank_theme';
export type Theme = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private theme = signal<Theme>(this.loadInitial());

  isDark = computed(() => this.theme() === 'dark');

  constructor() {
    this.apply(this.theme());
  }

  private loadInitial(): Theme {
    if (typeof localStorage === 'undefined') return 'light';
    const stored = localStorage.getItem(STORAGE_KEY) as Theme | null;
    if (stored === 'dark' || stored === 'light') return stored;
    if (typeof window !== 'undefined' && window.matchMedia('(prefers-color-scheme: dark)').matches)
      return 'dark';
    return 'light';
  }

  private apply(theme: Theme) {
    if (typeof document === 'undefined') return;
    const root = document.documentElement;
    const body = document.body;
    
    root.setAttribute('data-theme', theme);
    if (theme === 'dark') {
      body.classList.add('dark');
    } else {
      body.classList.remove('dark');
    }
  }

  setTheme(theme: Theme) {
    this.theme.set(theme);
    this.apply(theme);
    try {
      localStorage.setItem(STORAGE_KEY, theme);
    } catch {}
  }

  toggle() {
    this.setTheme(this.theme() === 'dark' ? 'light' : 'dark');
  }
}
