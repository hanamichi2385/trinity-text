# Publication pipeline

1. Host calls `IPublicationService.Create` and publishes `GenerateWebsiteMessage` (nothing in this repo does).
2. `GenerateWebsiteConsumer` (`generate_queue`): status Generating → `CreateExportFile` (texts, pages, files per `PublicationType`; XML or JSON) under `LocalDirectory/{website}_{id}_{ts}/` → ZIP → stored in DB (`ZIP_FILE`, via raw SQL). With an FTP server → status Publishing + `PublishWebsiteMessage`; otherwise Success + mail. Failure → Failed + mail.
3. `PublishWebsiteConsumer` (`publish_queue`): unzip → `ITransferServiceCoordinator.Upload` (scheme `ftp`/`sftp` selects service; uploads to `{tenant}/{website}`; deletes local dir in `finally`) → mail → publication row removed.
4. Retry/concurrency from `MassTransit:ConcurrentLimit|Retry|RetryIntervalMinutes`; in-memory outbox; consumers throw so MassTransit retries.
5. `ResetCacheMessage` and `SendMailMessage` have **no consumer here** – host app must implement.

Export layout: `Text/{SITE}/{lang}/[Subfolder/]{TextTypeName|OutputFilename}.{xml|json}`, file tree at root, `trinity-text.txt` stamp.
