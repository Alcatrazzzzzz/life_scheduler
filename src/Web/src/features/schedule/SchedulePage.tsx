import { CreateScheduleForm } from './components/CreateScheduleForm'
import { ScheduleList } from './components/ScheduleList'
import './schedule.css'

export function SchedulePage() {
  return (
    <main className="schedule-page">
      <h1>Планировщик</h1>

      <div className="schedule-layout">
        <CreateScheduleForm />
        <ScheduleList />
      </div>
    </main>
  )
}
