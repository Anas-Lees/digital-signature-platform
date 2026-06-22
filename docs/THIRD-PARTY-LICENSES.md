# Third-party licenses

## Why this project is AGPL-3.0

SignVault embeds real **PAdES** (ETSI EN 319 142) signatures into PDFs using
**iText for .NET** (`itext`, `itext.bouncy-castle-adapter`), which is licensed under the
**GNU Affero General Public License v3 (AGPL-3.0)**.

AGPL-3.0 is strongly copyleft and includes a network-use clause (Section 13): any larger
work that links iText — **including a hosted/SaaS deployment** — must offer its complete
corresponding source code to its users under AGPL-3.0. To stay compliant, this entire
repository is therefore licensed under **AGPL-3.0** (it was previously MIT).

If a permissive/closed license must be preserved, the options are:

1. Purchase a commercial **iText** license (then the rest of the code can be MIT again), or
2. Replace iText with a permissive stack — e.g. **PDFsharp (MIT) + BouncyCastle.Cryptography
   (MIT)** — and hand-roll the PAdES profile (ByteRange, CMS, timestamps). This is
   significantly more work and is the trade-off documented here.

## Key dependencies

| Package | Purpose | License |
|---|---|---|
| `itext` 9.6.0 | PAdES PDF signing & verification | AGPL-3.0 |
| `itext.bouncy-castle-adapter` 9.6.0 | Cryptography backend for iText | AGPL-3.0 (pulls `BouncyCastle.Cryptography`, MIT) |
| `Microsoft.EntityFrameworkCore.*`, `Npgsql.EntityFrameworkCore.PostgreSQL` | Data access | MIT / PostgreSQL |
| `Microsoft.AspNetCore.Authentication.JwtBearer`, `BCrypt.Net-Next` | Auth | MIT |
| `Scalar.AspNetCore` | API docs UI (dev) | MIT |
| Angular, RxJS | Frontend | MIT |
