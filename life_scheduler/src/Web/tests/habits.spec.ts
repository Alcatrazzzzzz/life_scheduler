import { test, expect, type Page } from '@playwright/test'

function uniqueTitle(prefix: string): string {
  return `${prefix} ${Date.now()}`
}

async function openHabits(page: Page) {
  await page.goto('/')
  await page.getByRole('link', { name: 'Привычки', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Привычки' })).toBeVisible()
  await expect(page).toHaveURL(/\/habits$/)
}

async function createHabit(page: Page, title: string, stackAfter?: string) {
  await page.getByRole('button', { name: 'Новая привычка', exact: true }).click()
  const dialog = page.getByRole('dialog')
  await expect(dialog).toBeVisible()

  await dialog.getByLabel('Название').fill(title)

  if (stackAfter) {
    await dialog.getByRole('button', { name: /^После привычки:/ }).click()
    await page.getByRole('option', { name: stackAfter, exact: true }).click()
  }

  await dialog.getByRole('button', { name: 'Сохранить' }).click()
  await expect(dialog).toBeHidden()
}

test('habit stacking chain, streak toggle and progress', async ({ page }) => {
  const first = uniqueTitle('Зарядка')
  const second = uniqueTitle('Душ')

  page.on('dialog', (dialog) => dialog.accept())

  await openHabits(page)
  await createHabit(page, first)
  await createHabit(page, second, first)

  const chain = page.locator('.habit-chain').first()
  await expect(chain.getByText(first, { exact: true })).toBeVisible()
  await expect(chain.getByText(second, { exact: true })).toBeVisible()
  await expect(chain.getByText(`после «${first}»`)).toBeVisible()

  await page.getByRole('button', { name: `Отметить «${first}»` }).click()

  const secondItem = chain.locator('.habit-chain__item').filter({ hasText: second })
  await expect(secondItem.getByText('пора')).toBeVisible()

  await expect(page.locator('.habit-progress .habit-heatmap').first()).toBeVisible()
  await expect(page.locator('.habit-progress .habit-bars')).toBeVisible()

  const secondCard = page.locator('.habit-card').filter({ hasText: second })
  await expect(secondCard.getByText(`После: ${first}`)).toBeVisible()

  for (const title of [second, first]) {
    const card = page.locator('.habit-card').filter({ hasText: title })
    await card.getByRole('button', { name: 'Удалить' }).click()
    await expect(page.locator('.habit-card').filter({ hasText: title })).toHaveCount(0)
  }
})
