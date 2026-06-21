export interface UserDto {
  id: string;
  email: string;
  displayName: string;
  role: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: UserDto;
}

export interface SignatureDto {
  id: string;
  documentId: string;
  signerName: string;
  algorithm: string;
  signatureBase64: string;
  certThumbprint: string;
  signedAt: string;
}

export interface DocumentDto {
  id: string;
  fileName: string;
  contentType: string;
  contentHash: string;
  sizeBytes: number;
  status: 'Uploaded' | 'Signed' | 'Revoked';
  createdAt: string;
  signature: SignatureDto | null;
}

export interface VerifyResponse {
  valid: boolean;
  message: string;
  documentId: string | null;
  fileName: string | null;
  signerName: string | null;
  algorithm: string | null;
  certThumbprint: string | null;
  signedAt: string | null;
}
