import { useEffect, useState, type MouseEvent } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { CalendarDays } from 'lucide-react'
import { ThemeToggle } from './components/ThemeToggle'
import { VoiceAssistant } from './features/assistant/components/VoiceAssistant'
import { HabitsPage } from './features/habits/HabitsPage'
import { SchedulePage } from './features/schedule/SchedulePage'

const queryClient = new QueryClient()

type Section = 'calendar' | 'habits'

const HABITS_PATH = '/habits'

function readSection(): Section {
  return window.location.pathname.startsWith(HABITS_PATH) ? 'habits' : 'calendar'
}

function App() {
  const [section, setSection] = useState<Section>(readSection)

  useEffect(() => {
    const handlePopState = () => setSection(readSection())
    window.addEventListener('popstate', handlePopState)
    return () => window.removeEventListener('popstate', handlePopState)
  }, [])

  function navigate(event: MouseEvent<HTMLAnchorElement>, next: Section) {
    event.preventDefault()

    const path = next === 'habits' ? HABITS_PATH : '/'
    if (window.location.pathname !== path) {
      window.history.pushState(null, '', path)
    }

    setSection(next)
  }

  return (
    <QueryClientProvider client={queryClient}>
      <header className="app-header">
        <a className="app-logo" href="/" onClick={(event) => navigate(event, 'calendar')}>
          <CalendarDays className="app-logo__mark" size={22} strokeWidth={1.5} aria-hidden="true" />
          <span>Планировщик</span>
        </a>
        <nav className="app-nav" aria-label="Разделы">
          <a
            href="/"
            aria-current={section === 'calendar' ? 'page' : undefined}
            onClick={(event) => navigate(event, 'calendar')}
          >
            Календарь
          </a>
          <a
            href={HABITS_PATH}
            aria-current={section === 'habits' ? 'page' : undefined}
            onClick={(event) => navigate(event, 'habits')}
          >
            Привычки
          </a>
        </nav>
        <div className="app-header__actions">
          <ThemeToggle />
        </div>
      </header>
      {section === 'calendar' ? <SchedulePage /> : <HabitsPage />}
      <VoiceAssistant />
    </QueryClientProvider>
  )
}

export default App
