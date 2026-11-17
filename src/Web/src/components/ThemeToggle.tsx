import { Moon, Sun } from 'lucide-react'
import { useRef, useState } from 'react'

type Theme = 'light' | 'dark'

// Дольше самой длинной анимации (--duration-slow), чтобы успеть убрать класс
const SWITCH_FADE_MS = 320

// Тему на <html> выставляет инлайн-скрипт в index.html до первой отрисовки
function readTheme(): Theme {
  return document.documentElement.dataset.theme === 'dark' ? 'dark' : 'light'
}

function iconClass(active: boolean) {
  return active ? 'theme-toggle__icon is-active' : 'theme-toggle__icon'
}

export function ThemeToggle() {
  const [theme, setTheme] = useState<Theme>(readTheme)
  const fadeTimer = useRef<number | undefined>(undefined)

  const nextTheme: Theme = theme === 'dark' ? 'light' : 'dark'
  const label = nextTheme === 'dark' ? 'Включить тёмную тему' : 'Включить светлую тему'

  function toggle() {
    const root = document.documentElement

    // Цвета анимируются только на время смены темы — иначе тормозят hover-переходы
    root.dataset.themeSwitching = ''
    window.clearTimeout(fadeTimer.current)
    fadeTimer.current = window.setTimeout(() => {
      delete root.dataset.themeSwitching
    }, SWITCH_FADE_MS)

    root.dataset.theme = nextTheme
    localStorage.setItem('theme', nextTheme)
    setTheme(nextTheme)
  }

  return (
    <button
      type="button"
      className="btn btn--ghost btn--icon"
      onClick={toggle}
      aria-label={label}
      title={label}
    >
      <span className="theme-toggle__icons" aria-hidden="true">
        <Sun size={16} strokeWidth={1.5} className={iconClass(theme === 'dark')} />
        <Moon size={16} strokeWidth={1.5} className={iconClass(theme === 'light')} />
      </span>
    </button>
  )
}
