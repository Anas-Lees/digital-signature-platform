import { I18n } from './i18n.service';

describe('I18n', () => {
  beforeEach(() => localStorage.removeItem('sv_lang'));

  it('defaults to English and LTR', () => {
    const i18n = new I18n();
    expect(i18n.lang()).toBe('en');
    expect(i18n.t('nav.documents')).toBe('My Documents');
    expect(document.documentElement.dir).toBe('ltr');
  });

  it('toggles to Arabic and flips direction to RTL', () => {
    const i18n = new I18n();
    i18n.toggle();
    expect(i18n.lang()).toBe('ar');
    expect(i18n.t('nav.documents')).toBe('مستنداتي');
    expect(document.documentElement.dir).toBe('rtl');
  });

  it('falls back to English for unknown keys', () => {
    const i18n = new I18n();
    expect(i18n.t('does.not.exist')).toBe('does.not.exist');
  });
});
