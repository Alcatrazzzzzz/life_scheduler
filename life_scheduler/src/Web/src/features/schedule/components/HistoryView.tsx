import { useEffect, useMemo, useState } from 'react'
import { Check, Paperclip, X } from 'lucide-react'
import type { HistoryFilters, ScheduleEntry, ScheduleKind, ScheduleResult } from '../api'
import {
  formatFullDate,
  formatTimeRange,
  groupByDate,
  parseDateString,
} from '../calendar'
import { useScheduleHistory } from '../queries'
import { Loader } from './Loader'

const KIND_OPTIONS: { value: ScheduleKind; label: string }[] = [
  { value: 'event', label: 'Событие' },
  { value: 'task', label: 'Задача' },
  { value: 'habit', label: 'Привычка' },
]

const RESULT_OPTIONS: { value: ScheduleResult; label: string }[] = [
  { value: 'completed', label: 'Выполнено' },
  { value: 'missed', label: 'Пропущено' },
]

const SEARCH_DEBOUNCE_MS = 300

interface HistoryViewProps {
  onEntryClick: (entry: ScheduleEntry) => void
}

function useDebouncedValue<T>(value: T, delay: number): T {
  const [debounced, setDebounced] = useState(value)

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delay)
    return () => clearTimeout(timer)
  }, [value, delay])

  return debounced
}

export function HistoryView({ onEntryClick }: HistoryViewProps) {
  const [query, setQuery] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [kind, setKind] = useState<ScheduleKind | ''>('')
  const [result, setResult] = useState<ScheduleResult | ''>('')
  const [onlyFiles, setOnlyFiles] = useState(false)

  const debouncedQuery = useDebouncedValue(query, SEARCH_DEBOUNCE_MS)

  const filters = useMemo<HistoryFilters>(
    () => ({
      from: from || undefined,
      to: to || undefined,
      query: debouncedQuery.trim() || undefined,
      kind: kind || undefined,
      result: result || undefined,
      hasAttachments: onlyFiles || undefined,
    }),
    [from, to, debouncedQuery, kind, result, onlyFiles],
  )

  const history = useScheduleHistory(filters)
  const filtered = Object.values(filters).some(Boolean)

  function resetFilters() {
    setQuery('')
    setFrom('')
    setTo('')
    setKind('')
    setResult('')
    setOnlyFiles(false)
  }

  function renderContent() {
    if (history.isPending) {
      return <Loader label="Загрузка истории…" />
    }

    if (history.isError) {
      return <p className="schedule-error">{history.error.message}</p>
    }

    const entries = history.data ?? []

    if (entries.length === 0) {
      return (
        <p className="calendar-status">
          {filtered ? 'Ничего не найдено по заданным фильтрам.' : 'Пока нет прошедших записей.'}
        </p>
      )
    }

    const grouped = groupByDate(entries)

    return [...grouped.entries()].map(([date, dayEntries]) => (
      <section className="history-day" key={date}>
        <h2 className="history-day__title">{formatFullDate(parseDateString(date))}</h2>

        <ul className="history-list">
          {dayEntries.map((entry) => (
            <li key={`${entry.id}-${entry.date}`}>
              <button
                type="button"
                className="history-row"
                onClick={() => onEntryClick(entry)}
              >
                <span className="history-row__time">
                  {formatTimeRange(entry.start, entry.end)}
                </span>
                <span className="history-row__title">{entry.title}</span>
                {entry.result === 'completed' && (
                  <span className="history-row__result history-row__result--completed">
                    <Check size={13} strokeWidth={1.5} aria-hidden="true" />
                    Выполнено
                  </span>
                )}
                {entry.result === 'missed' && (
                  <span className="history-row__result history-row__result--missed">
                    <X size={13} strokeWidth={1.5} aria-hidden="true" />
                    Не выполнена
                  </span>
                )}
                {entry.attachmentCount > 0 && (
                  <span className="history-row__files">
                    <Paperclip size={13} strokeWidth={1.5} aria-hidden="true" />
                    {entry.attachmentCount}
                  </span>
                )}
              </button>
            </li>
          ))}
        </ul>
      </section>
    ))
  }

  return (
    <div className="history-view">
      <div className="history-filters" role="search">
        <label className="history-filters__field history-filters__field--grow">
          <span>Поиск</span>
          <input
            type="search"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Заголовок или заметки"
          />
        </label>

        <label className="history-filters__field">
          <span>С даты</span>
          <input
            type="date"
            value={from}
            max={to || undefined}
            onChange={(event) => setFrom(event.target.value)}
          />
        </label>

        <label className="history-filters__field">
          <span>По дату</span>
          <input
            type="date"
            value={to}
            min={from || undefined}
            onChange={(event) => setTo(event.target.value)}
          />
        </label>

        <label className="history-filters__field">
          <span>Тип</span>
          <select
            value={kind}
            onChange={(event) => setKind(event.target.value as ScheduleKind | '')}
          >
            <option value="">Все типы</option>
            {KIND_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>

        <label className="history-filters__field">
          <span>Итог</span>
          <select
            value={result}
            onChange={(event) => setResult(event.target.value as ScheduleResult | '')}
          >
            <option value="">Любой</option>
            {RESULT_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>

        <label className="history-filters__check">
          <input
            type="checkbox"
            checked={onlyFiles}
            onChange={(event) => setOnlyFiles(event.target.checked)}
          />
          <span>С файлами</span>
        </label>

        {filtered && (
          <button
            type="button"
            className="btn btn--ghost history-filters__reset"
            onClick={resetFilters}
          >
            Сбросить
          </button>
        )}
      </div>

      <div className="history-view__list">{renderContent()}</div>
    </div>
  )
}
