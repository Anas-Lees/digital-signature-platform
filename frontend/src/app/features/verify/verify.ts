import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { DocumentService } from '../../core/document.service';
import { VerifyResponse } from '../../core/models';
import { I18n } from '../../core/i18n.service';
import { DocIllustration } from '../../shared/doc-illustration';

@Component({
  selector: 'app-verify',
  imports: [FormsModule, DatePipe, DocIllustration],
  template: `
    <section class="hero">
      <h1>{{ i18n.t('verify.title') }}</h1>
      <p>{{ i18n.t('verify.intro') }}</p>
    </section>

    <section class="stage">
      <div class="illus"><app-doc-illustration></app-doc-illustration></div>

      <div class="card">
        <div class="dropzone" [class.drag]="dragging()" (click)="fileInput.click()"
             (dragover)="onDragOver($event)" (dragleave)="dragging.set(false)" (drop)="onDrop($event)">
          <input #fileInput type="file" accept="application/pdf,.pdf" hidden (change)="onPick($event)" />
          <svg class="dz-ico" width="30" height="30" viewBox="0 0 24 24" fill="none" stroke="#1f7fc2"
               stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <path d="M12 16V4M7 9l5-5 5 5"/><path d="M5 20h14"/>
          </svg>
          <div class="dz-title">{{ i18n.t('verify.dropTitle') }}</div>
          <div class="dz-hint truncate" [title]="fileName()">{{ fileName() || i18n.t('verify.dropHint') }}</div>
        </div>

        @if (busy()) { <p class="muted small center mt">{{ i18n.t('common.loading') }}</p> }

        @if (result(); as r) {
          <div class="result" [class.good]="r.valid" [class.bad]="!r.valid">
            <span class="badge" [class.good]="r.valid" [class.bad]="!r.valid">
              <span class="dot"></span>{{ r.valid ? i18n.t('verify.valid') : i18n.t('verify.invalid') }}
            </span>
            @if (r.valid && r.signerName) {
              <div class="signed-by">
                {{ i18n.t('verify.signedByLabel') }} <b>{{ r.signerName }}</b>
                @if (r.signedAt) { <span class="muted"> · {{ r.signedAt | date:'medium' }}</span> }
              </div>
            }
            <div class="r-sub">{{ r.message }}</div>
            @if (r.fileName) { <div class="r-file truncate" [title]="r.fileName">{{ r.fileName }}</div> }
            @if (r.valid) {
              <div class="r-note">
                {{ i18n.t('verify.openAdobe') }}
                <a href="/api/verify/certificate">{{ i18n.t('verify.trustCert') }}</a>
                {{ i18n.t('verify.trustNote') }}
              </div>
            }
          </div>
          <button class="btn block mt" (click)="another(fileInput)">{{ i18n.t('verify.another') }}</button>
        }

        <details class="byid" [open]="byIdOpen()">
          <summary>{{ i18n.t('verify.byIdTitle') }}</summary>
          <label class="field mt">
            <span>{{ i18n.t('verify.docId') }}</span>
            <input type="text" [(ngModel)]="docId" name="docId"
                   placeholder="00000000-0000-0000-0000-000000000000" />
          </label>
          <button class="btn block" [disabled]="!docId || busy()" (click)="checkById()">
            {{ i18n.t('verify.checkById') }}
          </button>
        </details>
      </div>
    </section>

    <p class="center muted small mt">{{ i18n.t('verify.noLogin') }}</p>
  `
})
export class Verify implements OnInit {
  private docService = inject(DocumentService);
  private route = inject(ActivatedRoute);
  i18n = inject(I18n);

  docId = '';
  fileName = signal('');
  busy = signal(false);
  dragging = signal(false);
  byIdOpen = signal(false);
  result = signal<VerifyResponse | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.queryParamMap.get('doc');
    if (id) { this.docId = id; this.byIdOpen.set(true); this.checkById(); }
  }

  onPick(e: Event): void {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (file) this.verify(file);
  }

  onDragOver(e: DragEvent): void { e.preventDefault(); this.dragging.set(true); }

  onDrop(e: DragEvent): void {
    e.preventDefault();
    this.dragging.set(false);
    const file = e.dataTransfer?.files?.[0];
    if (file) this.verify(file);
  }

  another(input: HTMLInputElement): void {
    this.result.set(null);
    this.fileName.set('');
    input.value = '';
    input.click();
  }

  private verify(file: File): void {
    this.fileName.set(file.name);
    this.busy.set(true);
    this.docService.verifyFile(file).subscribe({
      next: (r) => { this.result.set(r); this.busy.set(false); },
      error: (e) => { this.result.set(e?.error ?? null); this.busy.set(false); }
    });
  }

  checkById(): void {
    if (!this.docId) return;
    this.busy.set(true);
    this.docService.verifyStored(this.docId.trim()).subscribe({
      next: (r) => { this.result.set(r); this.busy.set(false); },
      error: (e) => { this.result.set(e?.error ?? null); this.busy.set(false); }
    });
  }
}
