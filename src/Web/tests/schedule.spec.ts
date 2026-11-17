import { test, expect, type Page } from '@playwright/test'

function localDateString(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

async function openCreateDialog(page: Page) {
  await page.getByRole('button', { name: 'Новая запись', exact: true }).click()
  const dialog = page.getByRole('dialog')
  await expect(dialog).toBeVisible()
  return dialog
}

test('create schedule entry without time', async ({ page }) => {
  const title = `Разобрать почту ${Date.now()}`
  const today = localDateString(new Date())

  await page.goto('/schedule')

  const dialog = await openCreateDialog(page)
  await dialog.getByLabel('Название').fill(title)
  await dialog.getByLabel('Дата').fill(today)
  await dialog.getByLabel('Без времени').check()
  await dialog.getByRole('button', { name: 'Сохранить' }).click()
  await expect(dialog).toBeHidden()

  const chip = page.getByRole('button', { name: title })
  await expect(chip).toBeVisible()

  await chip.click()
  const details = page.getByRole('dialog')
  await expect(details).toContainText('Без времени')
  await details.getByRole('button', { name: 'Удалить' }).click()
  await expect(page.getByRole('button', { name: title })).toHaveCount(0)
})

test('create schedule event', async ({ page }) => {
  const title = `Планёрка ${Date.now()}`
  const today = localDateString(new Date())

  await page.goto('/schedule')

  const dialog = await openCreateDialog(page)
  await dialog.getByLabel('Название').fill(title)
  await dialog.getByLabel('Дата').fill(today)
  await dialog.getByLabel('Начало').fill('10:00')
  await dialog.getByLabel('Окончание').fill('10:30')
  await dialog.getByRole('button', { name: 'Сохранить' }).click()
  await expect(dialog).toBeHidden()

  const event = page.getByRole('button', { name: new RegExp(title) })
  await expect(event).toBeVisible()
  await expect(event).toContainText('10:00–10:30')

  await event.click()
  const details = page.getByRole('dialog')
  await expect(details).toContainText('10:00–10:30')
  await details.getByRole('button', { name: 'Удалить' }).click()
  await expect(page.getByRole('button', { name: new RegExp(title) })).toHaveCount(0)
})

test('clicking a time slot prefills the form', async ({ page }) => {
  const today = localDateString(new Date())

  await page.goto('/schedule')

  await page
    .getByRole('button', { name: `Новая запись ${today} в 14:00`, exact: true })
    .click()

  const dialog = page.getByRole('dialog')
  await expect(dialog.getByLabel('Дата')).toHaveValue(today)
  await expect(dialog.getByLabel('Начало')).toHaveValue('14:00')
  await expect(dialog.getByLabel('Окончание')).toHaveValue('15:00')

  await dialog.getByRole('button', { name: 'Отмена' }).click()
  await expect(dialog).toBeHidden()
})
