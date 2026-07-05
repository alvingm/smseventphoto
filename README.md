# smseventphoto

Azure C# web application for sharing event photos that arrive via SMS/MMS-style submissions.

## What the app does

- displays a live montage of event photos with their accompanying messages
- accepts multipart form posts at `/api/messages` with `photo`, `message`, and `from` fields for SMS gateway integration
- stores both photo binaries and message metadata in Azure Blob Storage to keep persistence on the lowest-cost Azure storage service
- falls back to local file storage during development when no Azure Blob connection string is configured

## Run locally

```bash
dotnet run
```

Then open the printed local URL and submit a test photo from the home page.

## Azure Blob Storage configuration

Set the following values in `appsettings.json` or environment variables:

- `Storage__AzureBlobConnectionString`
- `Storage__ContainerName` (defaults to `eventphotos`)

When the connection string is present, the app stores:

- images under `photos/`
- message JSON sidecars under `entries/`

inside the same blob container.
