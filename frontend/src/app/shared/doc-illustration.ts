import { Component } from '@angular/core';

/** Clean line-art illustration: a document, a signature, and a verified seal. */
@Component({
  selector: 'app-doc-illustration',
  standalone: true,
  template: `
    <svg viewBox="0 0 360 360" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
      <circle cx="180" cy="168" r="150" fill="#1f7fc2" opacity="0.05"/>
      <ellipse cx="182" cy="316" rx="112" ry="14" fill="#0f172a" opacity="0.05"/>

      <!-- back sheet -->
      <rect x="104" y="60" width="158" height="220" rx="14" fill="#ffffff" stroke="#eef2f7"
            stroke-width="1.5" transform="rotate(-6 183 170)"/>

      <!-- front sheet -->
      <rect x="92" y="52" width="176" height="236" rx="16" fill="#ffffff" stroke="#e2e8f0" stroke-width="1.5"/>

      <!-- heading + content lines -->
      <rect x="116" y="80" width="86" height="12" rx="6" fill="#0f172a"/>
      <rect x="116" y="102" width="54" height="7" rx="3.5" fill="#cbd5e1"/>
      <rect x="116" y="126" width="136" height="8" rx="4" fill="#eef2f6"/>
      <rect x="116" y="144" width="120" height="8" rx="4" fill="#eef2f6"/>
      <rect x="116" y="162" width="140" height="8" rx="4" fill="#eef2f6"/>
      <rect x="116" y="180" width="96"  height="8" rx="4" fill="#eef2f6"/>

      <!-- signature line + signature -->
      <line x1="116" y1="236" x2="210" y2="236" stroke="#cbd5e1" stroke-width="1.5" stroke-dasharray="3 5"/>
      <path d="M118 230 C128 210 138 248 150 226 C158 212 168 236 178 224 C186 214 197 232 206 222"
            stroke="#1f7fc2" stroke-width="3" stroke-linecap="round" fill="none"/>

      <!-- verified seal -->
      <circle cx="234" cy="252" r="33" fill="#e9f3fb" stroke="#1f7fc2" stroke-width="2"/>
      <circle cx="234" cy="252" r="25" fill="none" stroke="#1f7fc2" stroke-opacity="0.4"
              stroke-width="1.4" stroke-dasharray="1.5 4"/>
      <path d="M223 252l7 7 14-16" stroke="#1f7fc2" stroke-width="3"
            stroke-linecap="round" stroke-linejoin="round" fill="none"/>
    </svg>
  `,
  styles: [':host{display:block;width:100%}']
})
export class DocIllustration {}
