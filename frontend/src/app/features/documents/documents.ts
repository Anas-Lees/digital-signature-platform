import { Component, OnInit, inject, signal } from '@angular/core';
import { DocumentService } from '../../core/document.service';
import { DocumentDto, VerifyResponse } from '../../core/models';
import { I18n } from '../../core/i18n.service';

@Component({
  selector: 'app-documents',
  imports: [],
  template: `
    <div class="stack">
      <div class="card">
        <h2>{{ i18n.t('docs.title') }}</h2>
        <div class="uploader">
          <input #fileInput type="file" (change)="onFile($event)" aria-label="Choose a file" />
          <button class="btn primary" [disabled]="!file || busy()" (click)="upload(fileInput)">
            {{ busy() ? i18n.t('common.loading') : i18n.t('docs.upload') }}
          </button>
        </div>
        @if (error()) { <p class="error">{{ error() }}</p> }
      </div>

      <div class="card">
        @if (docs().length === 0) {
          <p class="muted">{{ i18n.t('docs.empty') }}</p>
        } @else {
          <table class="grid">
            <thead>
              <tr>
                <th>{{ i18n.t('docs.col.file') }}</th>
                <th>{{ i18n.t('docs.col.status') }}</th>
                <th>{{ i18n.t('docs.col.hash') }}</th>
                <th>{{ i18n.t('docs.col.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              @for (d of docs(); track d.id) {
                <tr>
                  <td>
                    <strong>{{ d.fileName }}</strong>
                    <div class="muted small">{{ formatBytes(d.sizeBytes) }}</div>
                  </td>
                  <td>
                    <span class="badge" [class.ok]="d.status === 'Signed'">
                      {{ d.status === 'Signed' ? i18n.t('docs.signed') : i18n.t('docs.uploaded') }}
                    </span>
                  </td>
                  <td><code class="hash">{{ d.contentHash.slice(0, 16) }}…</code></td>
                  <td class="actions">
                    @if (d.status !== 'Signed') {
                      <button class="btn" [disabled]="busy()" (click)="sign(d.id)">{{ i18n.t('docs.sign') }}</button>
                    }
                    <button class="btn" (click)="download(d)">{{ i18n.t('docs.download') }}</button>
                    @if (d.status === 'Signed') {
                      <button class="btn" (click)="verify(d.id)">{{ i18n.t('docs.verify') }}</button>
                    }
                    @if (results()[d.id]; as r) {
                      <span class="verify" [class.ok]="r.valid" [class.bad]="!r.valid">
                        {{ r.valid ? '✅ ' + i18n.t('verify.valid') : '❌ ' + i18n.t('verify.invalid') }}
                      </span>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        }
      </div>
    </div>
  `
})
export class Documents implements OnInit {
  private docService = inject(DocumentService);
  i18n = inject(I18n);

  docs = signal<DocumentDto[]>([]);
  file: File | null = null;
  busy = signal(false);
  error = signal('');
  results = signal<Record<string, VerifyResponse>>({});

  ngOnInit(): void { this.load(); }

  load(): void {
    this.docService.list().subscribe({
      next: (d) => this.docs.set(d),
      error: () => this.error.set('Failed to load documents.')
    });
  }

  onFile(e: Event): void {
    this.file = (e.target as HTMLInputElement).files?.[0] ?? null;
  }

  upload(input: HTMLInputElement): void {
    if (!this.file) return;
    this.busy.set(true);
    this.error.set('');
    this.docService.upload(this.file).subscribe({
      next: () => { this.file = null; input.value = ''; this.busy.set(false); this.load(); },
      error: (e) => { this.error.set(e?.error?.message ?? 'Upload failed.'); this.busy.set(false); }
    });
  }

  sign(id: string): void {
    this.busy.set(true);
    this.docService.sign(id).subscribe({
      next: () => { this.busy.set(false); this.load(); },
      error: (e) => { this.error.set(e?.error?.message ?? 'Sign failed.'); this.busy.set(false); }
    });
  }

  verify(id: string): void {
    this.docService.verifyStored(id).subscribe({
      next: (r) => this.results.update(m => ({ ...m, [id]: r }))
    });
  }

  download(d: DocumentDto): void {
    this.docService.download(d.id).subscribe(blob => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = d.fileName;
      a.click();
      URL.revokeObjectURL(url);
    });
  }

  formatBytes(n: number): string {
    if (n < 1024) return `${n} B`;
    if (n < 1024 * 1024) return `${(n / 1024).toFixed(1)} KB`;
    return `${(n / 1024 / 1024).toFixed(1)} MB`;
  }
}
