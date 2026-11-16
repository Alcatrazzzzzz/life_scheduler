import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { SchedulePage } from './features/schedule/SchedulePage'

const queryClient = new QueryClient()

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <SchedulePage />
    </QueryClientProvider>
  )
}

export default App
