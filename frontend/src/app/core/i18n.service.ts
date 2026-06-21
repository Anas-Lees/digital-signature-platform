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
      'docs.title': 'My Documents',
      'docs.upload': 'Upload & store',
      'docs.choose': 'Choose a file',
      'docs.sign': 'Sign',
      'docs.download': 'Download',
      'docs.verify': 'Verify',
      'docs.empty': 'No documents yet. Upload one to get started.',
      'docs.col.file': 'File',
      'docs.col.status': 'Status',
      'docs.col.hash': 'SHA-256',
      'docs.col.actions': 'Actions',
      'docs.signed': 'Signed',
      'docs.uploaded': 'Uploaded',
      'verify.title': 'Verify a document',
      'verify.intro': 'Confirm a signed document is authentic and untampered.',
      'verify.docId': 'Document ID',
      'verify.checkStored': 'Verify stored copy',
      'verify.checkUpload': 'Verify an uploaded file',
      'verify.publicKey': 'Show signing public key',
      'verify.valid': 'Authentic & untampered',
      'verify.invalid': 'Invalid or modified',
      'common.signedBy': 'Signed by SignVault authority',
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
      'docs.title': 'مستنداتي',
      'docs.upload': 'رفع وحفظ',
      'docs.choose': 'اختر ملفاً',
      'docs.sign': 'توقيع',
      'docs.download': 'تنزيل',
      'docs.verify': 'تحقّق',
      'docs.empty': 'لا توجد مستندات بعد. ارفع ملفاً للبدء.',
      'docs.col.file': 'الملف',
      'docs.col.status': 'الحالة',
      'docs.col.hash': 'البصمة SHA-256',
      'docs.col.actions': 'إجراءات',
      'docs.signed': 'موقّع',
      'docs.uploaded': 'مرفوع',
      'verify.title': 'التحقق من مستند',
      'verify.intro': 'تأكّد أنّ المستند الموقّع أصلي ولم يُعدّل.',
      'verify.docId': 'معرّف المستند',
      'verify.checkStored': 'تحقّق من النسخة المخزّنة',
      'verify.checkUpload': 'تحقّق من ملف مرفوع',
      'verify.publicKey': 'عرض المفتاح العام للتوقيع',
      'verify.valid': 'أصلي وغير معدّل',
      'verify.invalid': 'غير صالح أو معدّل',
      'common.signedBy': 'موقّع من سلطة SignVault',
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
