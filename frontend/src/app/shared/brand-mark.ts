import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-brand-mark',
  standalone: true,
  template: `
    <svg [attr.width]="size" [attr.height]="size" viewBox="0 0 32 32" fill="none"
         xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
      <rect x="1" y="1" width="30" height="30" rx="9" fill="#111827"/>
      <path d="M9.5 16.6l4 4 9-10" stroke="#fff" stroke-width="2.6"
            stroke-linecap="round" stroke-linejoin="round"/>
      <path d="M9 24.2c4.4-2.2 9.6-2.2 14 0" stroke="#1f7fc2" stroke-width="2.2"
            stroke-linecap="round" fill="none"/>
    </svg>
  `
})
export class BrandMark {
  @Input() size = 30;
}
