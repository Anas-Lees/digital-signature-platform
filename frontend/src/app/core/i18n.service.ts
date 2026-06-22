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
      'docs.subtitle': 'Upload a document, sign it as the authority, then verify it anytime.',
      'docs.uploadTitle': 'Upload & sign',
      'docs.uploadHint': 'PDF, image, or any file up to 25 MB',
      'docs.total': 'Documents',
      'docs.signedTotal': 'Signed',
      'docs.upload': 'Upload & store',
      'docs.choose': 'Choose a file',
      'docs.sign': 'Sign',
      'docs.download': 'Download',
      'docs.verify': 'Verify',
      'docs.share': 'Copy link',
      'docs.copied': 'Copied',
      'docs.certificate': 'Certificate',
      'docs.delete': 'Delete',
      'docs.confirmDelete': 'Delete this document permanently? This cannot be undone.',
      'docs.signedByLabel': 'Signed by',
      'docs.empty': 'No documents yet. Upload one to get started.',
      'docs.col.file': 'File',
      'docs.col.status': 'Status',
      'docs.col.hash': 'SHA-256',
      'docs.col.actions': 'Actions',
      'docs.signed': 'Signed',
      'docs.uploaded': 'Uploaded',
      'verify.title': 'Check a document',
      'verify.intro': 'See who signed a document and whether it is genuine. No account needed.',
      'verify.dropTitle': 'Check a signed file',
      'verify.dropHint': 'Drag a file here, or click to choose.',
      'verify.byIdTitle': 'Or check by document ID',
      'verify.docId': 'Document ID',
      'verify.checkById': 'Check ID',
      'verify.checkUpload': 'Choose a file to check',
      'verify.another': 'Check another file',
      'verify.signedByLabel': 'Signed by',
      'verify.valid': 'Authentic & untampered',
      'verify.invalid': 'Invalid or modified',
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
      'docs.subtitle': 'ارفع مستنداً، وقّعه بصفتك السلطة، ثم تحقّق منه في أي وقت.',
      'docs.uploadTitle': 'رفع وتوقيع',
      'docs.uploadHint': 'PDF أو صورة أو أي ملف حتى ٢٥ ميغابايت',
      'docs.total': 'المستندات',
      'docs.signedTotal': 'موقّعة',
      'docs.upload': 'رفع وحفظ',
      'docs.choose': 'اختر ملفاً',
      'docs.sign': 'توقيع',
      'docs.download': 'تنزيل',
      'docs.verify': 'تحقّق',
      'docs.share': 'نسخ الرابط',
      'docs.copied': 'تم النسخ',
      'docs.certificate': 'الشهادة',
      'docs.delete': 'حذف',
      'docs.confirmDelete': 'حذف هذا المستند نهائياً؟ لا يمكن التراجع.',
      'docs.signedByLabel': 'وقّعه',
      'docs.empty': 'لا توجد مستندات بعد. ارفع ملفاً للبدء.',
      'docs.col.file': 'الملف',
      'docs.col.status': 'الحالة',
      'docs.col.hash': 'البصمة SHA-256',
      'docs.col.actions': 'إجراءات',
      'docs.signed': 'موقّع',
      'docs.uploaded': 'مرفوع',
      'verify.title': 'تحقّق من مستند',
      'verify.intro': 'اطّلع على من وقّع المستند وما إذا كان أصلياً. لا حاجة لحساب.',
      'verify.dropTitle': 'تحقّق من ملف موقّع',
      'verify.dropHint': 'اسحب ملفاً هنا أو انقر للاختيار.',
      'verify.byIdTitle': 'أو تحقّق بمعرّف المستند',
      'verify.docId': 'معرّف المستند',
      'verify.checkById': 'تحقّق من المعرّف',
      'verify.checkUpload': 'اختر ملفاً للتحقق',
      'verify.another': 'تحقّق من ملف آخر',
      'verify.signedByLabel': 'وقّعه',
      'verify.valid': 'أصلي وغير معدّل',
      'verify.invalid': 'غير صالح أو معدّل',
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
