import { Injectable, signal } from '@angular/core';

export type Lang = 'en' | 'ar';

/**
 * Lightweight runtime i18n with RTL/LTR direction switching.
 * Flipping the language also flips document.dir, so the whole UI mirrors.
 */
@Injectable({ providedIn: 'root' })
export class I18n {
  readonly lang = signal<Lang>((localStorage.getItem('sv_lang') as Lang) || 'en');

  private dict: Record<Lang, Record<string, string>> = {
    en: {
      'app.title': 'SignVault',
      'app.tagline': 'Digital signature platform',
      'nav.documents': 'My Documents',
      'nav.verify': 'Verify',
      'nav.login': 'Login',
      'nav.register': 'Register',
      'nav.logout': 'Logout',
      'auth.email': 'Email',
      'auth.password': 'Password',
      'auth.displayName': 'Full name',
      'auth.loginTitle': 'Sign in',
      'auth.registerTitle': 'Create account',
      'auth.loginBtn': 'Sign in',
      'auth.registerBtn': 'Create account',
      'auth.demoHint': 'Demo account: demo@signvault.local / Demo1234!',
      'auth.noAccount': 'No account?',
      'auth.haveAccount': 'Already have an account?',
      'docs.title': 'Your documents',
      'docs.subtitle': 'Upload a PDF, sign it, then verify it anytime. The signature is embedded in the PDF.',
      'docs.uploadTitle': 'Upload & sign a PDF',
      'docs.uploadHint': 'PDF only · up to 25 MB',
      'docs.total': 'Documents',
      'docs.signedTotal': 'Signed',
      'docs.upload': 'Upload & store',
      'docs.choose': 'Choose a PDF',
      'docs.sign': 'Sign',
      'docs.download': 'Original',
      'docs.downloadSigned': 'Download signed PDF',
      'docs.verify': 'Verify',
      'docs.share': 'Copy link',
      'docs.copied': 'Copied',
      'docs.signedByLabel': 'Signed by',
      'docs.empty': 'No documents yet. Upload one to get started.',
      'docs.col.file': 'File',
      'docs.col.status': 'Status',
      'docs.col.hash': 'SHA-256',
      'docs.col.actions': 'Actions',
      'docs.signed': 'Signed',
      'docs.uploaded': 'Uploaded',
      'verify.title': 'Check a signed PDF',
      'verify.intro': 'See who signed a PDF and whether it is unaltered. No account needed.',
      'verify.dropTitle': 'Check a signed PDF',
      'verify.dropHint': 'Drag a PDF here, or click to choose.',
      'verify.byIdTitle': 'Or check by document ID',
      'verify.docId': 'Document ID',
      'verify.checkById': 'Check ID',
      'verify.checkUpload': 'Choose a PDF to check',
      'verify.another': 'Check another PDF',
      'verify.signedByLabel': 'Signed by',
      'verify.valid': 'Valid & unaltered',
      'verify.invalid': 'Invalid or modified',
      'verify.openAdobe': 'Open the PDF in Adobe Acrobat to see the signature panel.',
      'verify.trustCert': 'Download our certificate',
      'verify.trustNote': 'to trust the signer’s identity in Adobe.',
      'verify.noLogin': 'Anyone can verify — no sign-in required.',
      'common.signedBy': 'Signed by the SignVault signing authority',
      'common.authority': 'SignVault Signing Authority',
      'common.thumbprint': 'Certificate thumbprint',
      'common.loading': 'Working…'
    },
    ar: {
      'app.title': 'سِجن فولت',
      'app.tagline': 'منصة التوقيع الرقمي',
      'nav.documents': 'مستنداتي',
      'nav.verify': 'تحقّق',
      'nav.login': 'تسجيل الدخول',
      'nav.register': 'إنشاء حساب',
      'nav.logout': 'تسجيل الخروج',
      'auth.email': 'البريد الإلكتروني',
      'auth.password': 'كلمة المرور',
      'auth.displayName': 'الاسم الكامل',
      'auth.loginTitle': 'تسجيل الدخول',
      'auth.registerTitle': 'إنشاء حساب',
      'auth.loginBtn': 'دخول',
      'auth.registerBtn': 'إنشاء الحساب',
      'auth.demoHint': 'حساب تجريبي: demo@signvault.local / Demo1234!',
      'auth.noAccount': 'لا تملك حساباً؟',
      'auth.haveAccount': 'لديك حساب بالفعل؟',
      'docs.title': 'مستنداتك',
      'docs.subtitle': 'ارفع ملف PDF، وقّعه، ثم تحقّق منه في أي وقت. التوقيع مضمَّن داخل الملف.',
      'docs.uploadTitle': 'رفع وتوقيع ملف PDF',
      'docs.uploadHint': 'ملفات PDF فقط · حتى ٢٥ ميغابايت',
      'docs.total': 'المستندات',
      'docs.signedTotal': 'موقّعة',
      'docs.upload': 'رفع وحفظ',
      'docs.choose': 'اختر ملف PDF',
      'docs.sign': 'توقيع',
      'docs.download': 'الأصل',
      'docs.downloadSigned': 'تنزيل الملف الموقّع',
      'docs.verify': 'تحقّق',
      'docs.share': 'نسخ الرابط',
      'docs.copied': 'تم النسخ',
      'docs.signedByLabel': 'وقّعه',
      'docs.empty': 'لا توجد مستندات بعد. ارفع ملفاً للبدء.',
      'docs.col.file': 'الملف',
      'docs.col.status': 'الحالة',
      'docs.col.hash': 'البصمة SHA-256',
      'docs.col.actions': 'إجراءات',
      'docs.signed': 'موقّع',
      'docs.uploaded': 'مرفوع',
      'verify.title': 'تحقّق من ملف PDF موقّع',
      'verify.intro': 'اطّلع على من وقّع ملف PDF وما إذا كان غير معدّل. لا حاجة لحساب.',
      'verify.dropTitle': 'تحقّق من ملف PDF موقّع',
      'verify.dropHint': 'اسحب ملف PDF هنا أو انقر للاختيار.',
      'verify.byIdTitle': 'أو تحقّق بمعرّف المستند',
      'verify.docId': 'معرّف المستند',
      'verify.checkById': 'تحقّق من المعرّف',
      'verify.checkUpload': 'اختر ملف PDF للتحقق',
      'verify.another': 'تحقّق من ملف آخر',
      'verify.signedByLabel': 'وقّعه',
      'verify.valid': 'صالح وغير معدّل',
      'verify.invalid': 'غير صالح أو معدّل',
      'verify.openAdobe': 'افتح الملف في Adobe Acrobat لرؤية لوحة التوقيع.',
      'verify.trustCert': 'نزّل شهادتنا',
      'verify.trustNote': 'لتوثيق هوية المُوقِّع في Adobe.',
      'verify.noLogin': 'يمكن لأي شخص التحقق — دون تسجيل دخول.',
      'common.signedBy': 'موقّع من سلطة التوقيع SignVault',
      'common.authority': 'سلطة التوقيع SignVault',
      'common.thumbprint': 'بصمة الشهادة',
      'common.loading': 'جارٍ المعالجة…'
    }
  };

  constructor() {
    this.apply(this.lang());
  }

  t(key: string): string {
    return this.dict[this.lang()][key] ?? this.dict.en[key] ?? key;
  }

  toggle(): void {
    this.set(this.lang() === 'en' ? 'ar' : 'en');
  }

  set(l: Lang): void {
    this.lang.set(l);
    localStorage.setItem('sv_lang', l);
    this.apply(l);
  }

  private apply(l: Lang): void {
    const html = document.documentElement;
    html.lang = l;
    html.dir = l === 'ar' ? 'rtl' : 'ltr';
  }
}
