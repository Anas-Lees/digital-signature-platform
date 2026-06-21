import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DocumentService } from '../../core/document.service';
import { VerifyResponse } from '../../core/models';
import { I18n } from '../../core/i18n.service';

@Component({
  selector: 'app-verify',
  imports: [FormsModule],
  template: `
    <div class="card">
      <h2>{{ i18n.t('verify.title') }}</h2>
      <p class="muted">{{ i18n.t('verify.intro') }}</p>

      <label>{{ i18n.t('verify.docId') }}
        <input type="text" [(ngModel)]="docId" name="docId" placeholder="00000000-0000-0000-0000-000000000000" />
      </label>

      <div class="actions">
        <button class="btn primary" [disabled]="!docId || busy()" (click)="checkStored()">
          {{ i18n.t('verify.checkStored') }}
        </button>
        <label class="btn file-btn">
          {{ i18n.t('verify.checkUpload') }}
          <input type="file" hidden (change)="onFile($event)" />
        </label>
        <button class="btn" (click)="showKey()">{{ i18n.t('verify.publicKey') }}</button>
      </div>

      @if (result(); as r) {
        <div class="result" [class.ok]="r.valid" [class.bad]="!r.valid">
          <strong>{{ r.valid ? '✅ ' + i18n.t('verify.valid') : '❌ ' + i18n.t('verify.invalid') }}</strong>
          <div class="muted small">{{ r.message }}</div>
          @if (r.certThumbprint) {
            <div class="muted small">{{ i18n.t('common.thumbprint') }}: <code>{{ r.certThumbprint }}</code></div>
          }
        </div>
      }

      @if (pubKey()) {
        <pre class="pem">{{ pubKey() }}</pre>
      }
    </div>
  `
})
export class Verify {
  private docService = inject(DocumentService);
  i18n = inject(I18n);

  docId = '';
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
    if (!file || !this.docId) return;
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
