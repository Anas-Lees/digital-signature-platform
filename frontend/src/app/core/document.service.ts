import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE } from './api.config';
import { DocumentDto, SignatureDto, VerifyResponse } from './models';

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private http = inject(HttpClient);

  list(): Observable<DocumentDto[]> {
    return this.http.get<DocumentDto[]>(`${API_BASE}/documents`);
  }

  upload(file: File): Observable<DocumentDto> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<DocumentDto>(`${API_BASE}/documents/upload`, form);
  }

  sign(id: string): Observable<SignatureDto> {
    return this.http.post<SignatureDto>(`${API_BASE}/documents/${id}/sign`, {});
  }

  download(id: string): Observable<Blob> {
    return this.http.get(`${API_BASE}/documents/${id}/download`, { responseType: 'blob' });
  }

  /** The signed PDF — the digital signature is embedded inside it (Adobe-readable). */
  signed(id: string): Observable<Blob> {
    return this.http.get(`${API_BASE}/documents/${id}/signed`, { responseType: 'blob' });
  }

  verifyStored(documentId: string): Observable<VerifyResponse> {
    return this.http.get<VerifyResponse>(`${API_BASE}/verify/${documentId}`);
  }

  /** Easiest public check: send a file, get back who signed it and whether it's valid. No id needed. */
  verifyFile(file: File): Observable<VerifyResponse> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<VerifyResponse>(`${API_BASE}/verify`, form);
  }

  verifyUpload(documentId: string, file: File): Observable<VerifyResponse> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<VerifyResponse>(`${API_BASE}/verify/${documentId}`, form);
  }

  publicKey(): Observable<{ algorithm: string; thumbprint: string; publicKeyPem: string }> {
    return this.http.get<{ algorithm: string; thumbprint: string; publicKeyPem: string }>(
      `${API_BASE}/verify/public-key`);
  }
}
