# YoutubeCrawler

A complete, production-ready .NET 8 Console Application that crawls YouTube channels using the YouTube Data API v3 and stores all data in SQL Server.

## Setup

1. Add your API key and connection string to `appsettings.json`.
2. Ensure you have run the schema setup script in `Database/schema.sql` against your target database.
3. Add channels to `TargetChannels` list in `appsettings.json`.

## Features
- Extracts channel info, playlists, videos, comments, replies, and captions metadata.
- Extracts all URLs from channel/video descriptions, resolving shortened URLs and categorizing them.
- Uses Dapper for lightweight DB access with transactions.
- Employs Polly for retry logic.
- Configurable settings via appsettings.json.
