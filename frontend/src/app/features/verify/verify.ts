import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DocumentService } from '../../core/document.service';
import { VerifyResponse } from '../../core/models';
import { I18n } from '../../core/i18n.service';
import { DocIllustration } from '../../shared/doc-illustration';

@Component({
  selector: 'app-verify',
  imports: [FormsModule, DocIllustration],
  template: `
    <section class="hero">
      <h1>{{ i18n.t('verify.title') }}</h1>
      <p>{{ i18n.t('verify.intro') }}</p>
    </section>

    <section class="stage">
      <div class="illus"><app-doc-illustration></app-doc-illustration></div>

      <div class="card">
        <label class="field">
          <span>{{ i18n.t('verify.docId') }}</span>
          <input type="text" [(ngModel)]="docId" name="docId"
                 placeholder="00000000-0000-0000-0000-000000000000" />
        </label>

        <button class="btn primary block" [disabled]="!docId || busy()" (click)="checkStored()">
          {{ i18n.t('verify.checkStored') }}
        </button>

        <label class="filepick mt">
          <input type="file" (change)="onFile($event)" />
          <span class="pick">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#1f7fc2"
                 stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M12 16V4M7 9l5-5 5 5"/><path d="M5 20h14"/>
            </svg>
            <span class="name">{{ fileName() || i18n.t('verify.checkUpload') }}</span>
          </span>
        </label>

        <button class="btn subtle block sm mt" (click)="showKey()">{{ i18n.t('verify.publicKey') }}</button>

        @if (result(); as r) {
          <div class="result" [class.good]="r.valid" [class.bad]="!r.valid">
            <span class="badge" [class.good]="r.valid" [class.bad]="!r.valid">
              <span class="dot"></span>{{ r.valid ? i18n.t('verify.valid') : i18n.t('verify.invalid') }}
            </span>
            <div class="r-sub">{{ r.message }}</div>
            @if (r.certThumbprint) {
              <div class="r-meta">{{ i18n.t('common.thumbprint') }}: <code>{{ r.certThumbprint }}</code></div>
            }
          </div>
        }

        @if (pubKey()) { <pre class="pem">{{ pubKey() }}</pre> }
      </div>
    </section>
  `
})
export class Verify {
  private docService = inject(DocumentService);
  i18n = inject(I18n);

  docId = '';
  fileName = signal('');
  busy = signal(false);
  result = signal<VerifyResponse | null>(null);
  pubKey = signal<string>('');

  checkStored(): void {
    if (!this.docId) return;
    this.busy.set(true);
    this.docService.verifyStored(this.docId.trim()).subscribe({
      next: (r) => { this.result.set(r); this.busy.set(false); },
      error: (e) => { this.result.set(e?.error ?? { valid: false, message: 'Not found' } as VerifyResponse); this.busy.set(false); }
    });
  }

  onFile(e: Event): void {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.fileName.set(file.name);
    if (!this.docId) { this.result.set({ valid: false, message: 'Enter a document ID first.' } as VerifyResponse); return; }
    this.busy.set(true);
    this.docService.verifyUpload(this.docId.trim(), file).subscribe({
      next: (r) => { this.result.set(r); this.busy.set(false); },
      error: (e) => { this.result.set(e?.error ?? { valid: false, message: 'Not found' } as VerifyResponse); this.busy.set(false); }
    });
  }

  showKey(): void {
    this.docService.publicKey().subscribe(k => this.pubKey.set(k.publicKeyPem));
  }
}
