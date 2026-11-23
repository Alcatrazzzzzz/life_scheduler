import { test, expect, type Locator, type Page } from '@playwright/test'

const MONTHS_GENITIVE = [
  'января',
  'февраля',
  'марта',
  'апреля',
  'мая',
  'июня',
  'июля',
  'августа',
  'сентября',
  'октября',
  'ноября',
  'декабря',
]

function localDateString(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

function displayDate(iso: string): string {
  const [year, month, day] = iso.split('-').map(Number)
  return `${day} ${MONTHS_GENITIVE[month - 1]} ${year}`
}

async function openCreateDialog(page: Page) {
  await page.getByRole('button', { name: 'Новая запись', exact: true }).click()
  const dialog = page.getByRole('dialog')
  await expect(dialog).toBeVisible()
  return dialog
}

async function pickDate(dialog: Locator, iso: string) {
  await dialog.getByRole('button', { name: /^Дата:/ }).click()
  const panel = dialog.getByRole('dialog', { name: 'Дата' })
  const day = panel.locator(`[data-date="${iso}"]`)

  // Пикер открывается на выбранной дате; соседний месяц листаем назад.
  for (let attempt = 0; attempt < 2 && (await day.count()) === 0; attempt += 1) {
    await panel.getByRole('button', { name: 'Предыдущий месяц' }).click()
  }

  await day.click()
  await expect(panel).toBeHidden()
}

async function pickTime(dialog: Locator, label: string, value: string) {
  await dialog.getByRole('button', { name: new RegExp(`^${label}:`) }).click()
  const panel = dialog.getByRole('dialog', { name: label })
  const [hours, minutes] = value.split(':')

  await panel
    .getByRole('listbox', { name: 'Часы' })
    .getByRole('option', { name: hours, exact: true })
    .click()
  await panel
    .getByRole('listbox', { name: 'Минуты' })
    .getByRole('option', { name: minutes, exact: true })
    .click()

  await expect(panel).toBeHidden()
}

test('create schedule entry without time', async ({ page }) => {
  const title = `Разобрать почту ${Date.now()}`
  const today = localDateString(new Date())

  await page.goto('/schedule')

  const dialog = await openCreateDialog(page)
  await dialog.getByLabel('Название').fill(title)
  await pickDate(dialog, today)
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
  await pickDate(dialog, today)
  await dialog.getByRole('textbox', { name: 'Начало' }).fill('10:00')
  await pickTime(dialog, 'Окончание', '10:30')
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
  await expect(dialog.getByRole('button', { name: `Дата: ${displayDate(today)}` })).toBeVisible()
  await expect(dialog.getByRole('textbox', { name: 'Начало' })).toHaveValue('14:00')
  await expect(dialog.getByRole('textbox', { name: 'Окончание' })).toHaveValue('15:00')

  await dialog.getByRole('button', { name: 'Отмена' }).click()
  await expect(dialog).toBeHidden()
})

test('create schedule entry with a file from the form', async ({ page }) => {
  const title = `С файлом ${Date.now()}`
  const today = localDateString(new Date())

  await page.goto('/schedule')

  const dialog = await openCreateDialog(page)
  await dialog.getByLabel('Название').fill(title)
  await pickDate(dialog, today)
  await dialog.getByLabel('Без времени').check()
  await dialog.locator('input[type="file"]').setInputFiles({
    name: 'план.txt',
    mimeType: 'text/plain',
    buffer: Buffer.from('план на день'),
  })
  await expect(dialog.getByText('план.txt')).toBeVisible()

  await dialog.getByRole('button', { name: 'Сохранить' }).click()
  await expect(dialog).toBeHidden()

  const chip = page.getByRole('button', { name: new RegExp(title) })
  await chip.click()

  const details = page.getByRole('dialog')
  await expect(details.getByRole('link', { name: 'план.txt' })).toBeVisible()

  await details.getByRole('button', { name: 'Удалить план.txt' }).click()
  await expect(details.getByRole('link', { name: 'план.txt' })).toHaveCount(0)
  await details.getByRole('button', { name: 'Удалить', exact: true }).click()
  await expect(page.getByRole('button', { name: new RegExp(title) })).toHaveCount(0)
})

test('history keeps past entries and lets you attach files', async ({ page }) => {
  const title = `Смета ${Date.now()}`
  const yesterday = localDateString(new Date(Date.now() - 24 * 60 * 60 * 1000))

  await page.goto('/schedule')

  const dialog = await openCreateDialog(page)
  await dialog.getByLabel('Название').fill(title)
  await pickDate(dialog, yesterday)
  await dialog.getByLabel('Без времени').check()
  await dialog.getByRole('button', { name: 'Сохранить' }).click()
  await expect(dialog).toBeHidden()

  await page.getByRole('button', { name: 'История', exact: true }).click()

  const row = page.getByRole('button', { name: new RegExp(title) })
  await expect(row).toBeVisible()

  await row.click()
  const details = page.getByRole('dialog')

  await details.locator('input[type="file"]').setInputFiles({
    name: 'смета.txt',
    mimeType: 'text/plain',
    buffer: Buffer.from('данные сметы'),
  })

  const fileLink = details.getByRole('link', { name: 'смета.txt' })
  await expect(fileLink).toBeVisible()

  const downloadPromise = page.waitForEvent('download')
  await fileLink.click()
  const download = await downloadPromise
  expect(download.suggestedFilename()).toBe('смета.txt')

  await details.getByRole('button', { name: 'Удалить смета.txt' }).click()
  await expect(fileLink).toHaveCount(0)

  await details.getByRole('button', { name: 'Удалить', exact: true }).click()
  await expect(page.getByRole('button', { name: new RegExp(title) })).toHaveCount(0)
})

