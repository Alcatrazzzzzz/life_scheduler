import { test, expect } from '@playwright/test'

test('create schedule entry without time', async ({ page }) => {
  const title = `Разобрать почту ${Date.now()}`

  await page.goto('/schedule')

  const form = page.locator('form.schedule-form')
  await form.getByLabel('Название').fill(title)
  await form.getByLabel('Дата').fill('2026-11-17')
  await form.getByLabel('Без времени').check()

  await form.getByRole('button', { name: 'Сохранить' }).click()

  const row = page.getByRole('row').filter({ hasText: title })
  await expect(row).toContainText('2026-11-17')
  await expect(row).toContainText('Без времени')

  await row.getByRole('button', { name: 'Удалить' }).click()
  await expect(row).toHaveCount(0)
})

test('create schedule event', async ({ page }) => {
  const title = `Планёрка ${Date.now()}`

  await page.goto('/schedule')

  const form = page.locator('form.schedule-form')
  await form.getByLabel('Название').fill(title)
  await form.getByLabel('Дата').fill('2026-11-16')
  await form.getByLabel('Начало').fill('10:00')
  await form.getByLabel('Окончание').fill('10:30')

  await form.getByRole('button', { name: 'Сохранить' }).click()

  const row = page.getByRole('row').filter({ hasText: title })
  await expect(row).toContainText('2026-11-16')
  await expect(row).toContainText('10:00–10:30')

  await row.getByRole('button', { name: 'Удалить' }).click()
  await expect(row).toHaveCount(0)
})
