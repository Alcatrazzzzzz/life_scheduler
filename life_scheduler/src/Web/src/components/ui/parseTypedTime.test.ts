import { describe, expect, it } from 'vitest'
import { parseTypedTime } from './parseTypedTime'

describe('parseTypedTime', () => {
  it.each([
    ['9:30', '09:30'],
    ['09:30', '09:30'],
    ['9.30', '09:30'],
    ['9 30', '09:30'],
    ['930', '09:30'],
    ['0930', '09:30'],
    ['1730', '17:30'],
    ['9', '09:00'],
    ['15', '15:00'],
    ['0', '00:00'],
    ['23:59', '23:59'],
  ])('разбирает «%s» как %s', (raw, expected) => {
    expect(parseTypedTime(raw)).toBe(expected)
  })

  it.each(['24:00', '12:60', '12345', 'abc', '', '10:3:0'])(
    'отклоняет «%s»',
    (raw) => {
      expect(parseTypedTime(raw)).toBeNull()
    },
  )
})
