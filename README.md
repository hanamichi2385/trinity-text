# trinity-text

A multi-tenant text and content localizer for managing translations, structured pages, widgets, and media assets across websites, sites, and languages.

## Features

- **Localized texts** with revision history, scoped by website / site / country / language
- **Structured pages** built from typed atoms (text, image, gallery, number, datetime, checkbox, separator)
- **Widgets** as reusable content fragments with placeholder substitution (`@[WIDGET(key)]`, `@[TENANT]`, `@[LANG]`, ...)
- **File manager** with hierarchical folders, per-file content and thumbnail
- **Image optimization** to WebP via SkiaSharp, with thumbnail generation
- **Publications** that export content as XML/JSON, package it as ZIP, and deliver via FTP/SFTP to CDN servers
- **Excel import/export** for texts, pages, and widgets
- **Service Bus integration** (MassTransit) for asynchronous publish and cache-reset messaging

## Page atoms

Pages are composed of typed atoms defined by an XML schema (`<textatom>`, `<imageatom>`, ...). Every atom shares `Id`, `IsRequired`, and `Description`.

| Atom | Purpose | Specific attributes |
|---|---|---|
| `TextAtom` | Plain text or HTML body | `IsHtml`, `MinValue`/`MaxValue` (length), `Extend` |
| `NumberAtom` | Integer value with range check | `MinValue`, `MaxValue` |
| `CheckBoxAtom` | Boolean flag | — |
| `DateTimeAtom` | Date/time, format `dd/MM/yyyy HH:mm:ss` | — |
| `ImageAtom` | Single image with metadata | `Value` (url), `Mobile`, `Caption`, `Link`, `Target`, `UseMobile` |
| `GalleryAtom` | Ordered list of `ImageParticol` (path, mobile path, caption, link, target, order) | `ItemName`, `PathName`/`LinkName`/`CaptionName` (+ descriptions), `UseMobile` |
| `SeparatorAtom` | Visual divider, no value | — |

## Architecture

- `TrinityText.Domain` — entities and `IRepository<T>` abstraction
- `TrinityText.Domain.EF` / `TrinityText.Domain.NH` — EF Core 8 and NHibernate 5 implementations (interchangeable)
- `TrinityText.Business` — services, DTOs, AutoMapper profiles (provider-agnostic)
- `TrinityText.Utilities` — image, zip, FTP/SFTP, Excel
- `TrinityText.ServiceBus[.MassTransit]` — messaging contracts and transport
- `TrinityText.Utilities.AWS` — AWS-specific helpers

Target framework: **.NET 8**