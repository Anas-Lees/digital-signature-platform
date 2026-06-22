import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DocumentService } from '../../core/document.service';
import { DocumentDto, VerifyResponse } from '../../core/models';
import { I18n } from '../../core/i18n.service';
import { DocIllustration } from '../../shared/doc-illustration';

@Component({
  selector: 'app-documents',
  imports: [DocIllustration],
  template: `
    <section class="hero">
      <h1>{{ i18n.t('docs.title') }}</h1>
      <p>{{ i18n.t('docs.subtitle') }}</p>
    </section>

    <section class="stage">
      <div class="illus"><app-doc-illustration></app-doc-illustration></div>

      <div class="card">
        <h2>{{ i18n.t('docs.uploadTitle') }}</h2>
        <p class="lead">{{ i18n.t('docs.uploadHint') }}</p>

        <label class="filepick mt" [class.drag]="dragging()"
               (dragover)="onDragOver($event)" (dragleave)="dragging.set(false)" (drop)="onDrop($event)">
          <input #fileInput type="file" (change)="onFile($event)" aria-label="Choose a file" />
          <span class="pick">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#1f7fc2"
                 stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M12 16V4M7 9l5-5 5 5"/><path d="M5 20h14"/>
            </svg>
            <span class="name truncate" [title]="fileName()">{{ fileName() || i18n.t('docs.choose') }}</span>
          </span>
        </label>

        <button class="btn primary block mt" [disabled]="!file || busy()" (click)="upload(fileInput)">
          {{ busy() ? i18n.t('common.loading') : i18n.t('docs.upload') }}
        </button>
        @if (error()) { <p class="error">{{ error() }}</p> }

        <div class="panel">
          <div class="kv">
            <span class="k">{{ i18n.t('docs.total') }}</span>
            <span class="v accent">{{ docs().length }}</span>
          </div>
          <div class="progress"><i [style.width.%]="signedPct()"></i></div>
          <div class="stat">
            <div class="n">{{ signedCount() }}</div>
            <div class="l">{{ i18n.t('docs.signedTotal') }}</div>
          </div>
        </div>
      </div>
    </section>

    <div class="card table-card mt">
      @if (docs().length === 0) {
        <div class="empty">{{ i18n.t('docs.empty') }}</div>
      } @else {
        <table class="table">
          <thead>
            <tr>
              <th>{{ i18n.t('docs.col.file') }}</th>
              <th>{{ i18n.t('docs.col.status') }}</th>
              <th>{{ i18n.t('docs.col.hash') }}</th>
              <th class="row-actions">{{ i18n.t('docs.col.actions') }}</th>
            </tr>
          </thead>
          <tbody>
            @for (d of docs(); track d.id) {
              <tr>
                <td class="file-cell">
                  <div class="fname truncate" [title]="d.fileName">{{ d.fileName }}</div>
                  <div class="fmeta truncate">
                    {{ formatBytes(d.sizeBytes) }}
                    @if (d.signature?.signerName) {
                      · {{ i18n.t('docs.signedByLabel') }} {{ d.signature?.signerName }}
                    }
                  </div>
                </td>
                <td>
                  @if (d.status === 'Signed') {
                    <span class="badge good"><span class="dot"></span>{{ i18n.t('docs.signed') }}</span>
                  } @else {
                    <span class="badge neutral"><span class="dot"></span>{{ i18n.t('docs.uploaded') }}</span>
                  }
                </td>
                <td><code class="hash">{{ d.contentHash.slice(0, 16) }}…</code></td>
                <td class="row-actions">
                  @if (d.status !== 'Signed') {
                    <button class="btn sm primary" [disabled]="busy()" (click)="sign(d.id)">{{ i18n.t('docs.sign') }}</button>
                  }
                  <button class="btn sm" (click)="download(d)">{{ i18n.t('docs.download') }}</button>
                  @if (d.status === 'Signed') {
                    <button class="btn sm" (click)="downloadCertificate(d)">{{ i18n.t('docs.certificate') }}</button>
                    <button class="btn sm" (click)="copyLink(d.id)">
                      {{ copied() === d.id ? i18n.t('docs.copied') : i18n.t('docs.share') }}
                    </button>
                  }
                  <button class="btn sm danger" [disabled]="busy()" (click)="remove(d)">{{ i18n.t('docs.delete') }}</button>
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>
  `
})
export class Documents implements OnInit {
  private docService = inject(DocumentService);
  i18n = inject(I18n);

  docs = signal<DocumentDto[]>([]);
  file: File | null = null;
  fileName = signal('');
  busy = signal(false);
  error = signal('');
  copied = signal('');
  dragging = signal(false);

  onDragOver(e: DragEvent): void { e.preventDefault(); this.dragging.set(true); }

  onDrop(e: DragEvent): void {
    e.preventDefault();
    this.dragging.set(false);
    const f = e.dataTransfer?.files?.[0];
    if (f) { this.file = f; this.fileName.set(f.name); }
  }

  signedCount = computed(() => this.docs().filter(d => d.status === 'Signed').length);
  signedPct = computed(() => {
    const total = this.docs().length;
    return total ? Math.round((this.signedCount() / total) * 100) : 0;
  });

  ngOnInit(): void { this.load(); }

  load(): void {
    this.docService.list().subscribe({
      next: (d) => this.docs.set(d),
      error: () => this.error.set('Failed to load documents.')
    });
  }

  onFile(e: Event): void {
    this.file = (e.target as HTMLInputElement).files?.[0] ?? null;
    this.fileName.set(this.file?.name ?? '');
  }

  upload(input: HTMLInputElement): void {
    if (!this.file) return;
    this.busy.set(true);
    this.error.set('');
    this.docService.upload(this.file).subscribe({
      next: () => {
        this.file = null; this.fileName.set(''); input.value = '';
        this.busy.set(false); this.load();
      },
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

  copyLink(id: string): void {
    const url = `${location.origin}/verify?doc=${id}`;
    navigator.clipboard.writeText(url).then(() => {
      this.copied.set(id);
      setTimeout(() => { if (this.copied() === id) this.copied.set(''); }, 1600);
    });
  }

  download(d: DocumentDto): void {
    this.docService.download(d.id).subscribe(blob => this.saveBlob(blob, d.fileName));
  }

  downloadCertificate(d: DocumentDto): void {
    this.docService.certificate(d.id).subscribe(blob =>
      this.saveBlob(blob, `${d.fileName.replace(/\.[^.]+$/, '')}-certificate.pdf`));
  }

  remove(d: DocumentDto): void {
    if (!confirm(this.i18n.t('docs.confirmDelete'))) return;
    this.busy.set(true);
    this.docService.remove(d.id).subscribe({
      next: () => { this.busy.set(false); this.load(); },
      error: (e) => { this.error.set(e?.error?.message ?? 'Delete failed.'); this.busy.set(false); }
    });
  }

  private saveBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.click();
    URL.revokeObjectURL(url);
  }

  formatBytes(n: number): string {
    if (n < 1024) return `${n} B`;
    if (n < 1024 * 1024) return `${(n / 1024).toFixed(1)} KB`;
    return `${(n / 1024 / 1024).toFixed(1)} MB`;
  }
}
