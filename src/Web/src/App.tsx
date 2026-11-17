import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { CalendarDays } from 'lucide-react'
import { SchedulePage } from './features/schedule/SchedulePage'

const queryClient = new QueryClient()

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <header className="app-header">
        <a className="app-logo" href="/">
          <CalendarDays className="app-logo__mark" size={22} strokeWidth={1.5} aria-hidden="true" />
          <span>Планировщик</span>
        </a>
      </header>
      <SchedulePage />
    </QueryClientProvider>
  )
}

export default App
