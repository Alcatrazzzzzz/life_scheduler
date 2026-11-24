import { SelectField, type SelectOption } from './SelectField'

interface HabitExtraFieldsProps {
  reward: string
  stackAfterId: string
  habits: ReadonlyArray<{ id: string; title: string }>
  excludeId?: string
  onChange: (patch: { reward?: string; stackAfterId?: string }) => void
  disabled?: boolean
}

export function HabitExtraFields({
  reward,
  stackAfterId,
  habits,
  excludeId,
  onChange,
  disabled,
}: HabitExtraFieldsProps) {
  const options: SelectOption<string>[] = [
    { value: '', label: 'Без цепочки' },
    ...habits
      .filter((habit) => habit.id !== excludeId)
      .map((habit) => ({ value: habit.id, label: habit.title })),
  ]

  return (
    <fieldset className="habit-extras" disabled={disabled}>
      <legend>Цепочка и награда</legend>

      <SelectField
        label="После привычки"
        value={stackAfterId}
        options={options}
        onChange={(value) => onChange({ stackAfterId: value })}
      />

      <label>
        Награда после отметки
        <input
          value={reward}
          maxLength={200}
          placeholder="Например, чашка кофе"
          onChange={(event) => onChange({ reward: event.target.value })}
        />
      </label>

      <p className="habit-extras__hint">
        Награда необязательна и показывается только после выполнения.
      </p>
    </fieldset>
  )
}
