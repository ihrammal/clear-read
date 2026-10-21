# TextBridge automatic update hosting

The canonical feed is https://api.github.com/repos/ihrammal/textbridge/releases/latest.
The owner selected the original clear-read feed on 20 October 2026 and requested
the repository rename on 21 October 2026. Existing blank feed settings migrate
to this address. The app asks permission before downloading; No cancels without
a download. GitHub stable releases need a matching installer asset and digest.
A complete real update and installed upgrade still require validation.

## Alternative hosting

Set UpdateFeedUrl at build/distribution time, or enter it in Settings > App settings
> Updates. It must be a trusted public HTTPS URL under the author's control.
Do not place GitHub access tokens in the app or in a download URL.

Option 1: a separate public repository containing only binary releases. Configure
its GitHub REST /repos/OWNER/BINARY-REPO/releases/latest endpoint. Upload a stable
release with a TextBridge-Setup-VERSION.exe asset. The updater reads the asset's
GitHub-provided sha256 digest. Draft and prerelease entries are not installed.

Option 2: serve JSON and the installer from a public HTTPS download host:

```json
{
  "version": "1.0.2",
  "installerUrl": "https://YOUR-DOWNLOAD-HOST/TextBridge-Setup-1.0.2.exe",
  "sha256": "REPLACE_WITH_THE_64_CHARACTER_SHA256_OF_THE_FINAL_INSTALLER"
}
```

Never publish the placeholder example as an active update feed. Generate the
checksum after building the final installer. The feed is a trust boundary: it
must be writable only by the publisher. A checksum verifies download integrity;
it does not replace publisher code signing or protection of the hosting account.

Automatic downloads use a fresh local temporary directory, reject insecure
addresses, check the final URL after redirects, limit downloads to 1 GiB and
remove rejected/partial installers. Windows elevation remains visible to the user.
Install runs with /SILENT /NORESTART /NOCLOSEAPPLICATIONS and the current app folder.
The app closes only after the installer starts and unsaved-text protection has
completed. Windows handles elevation; TextBridge does not force-close other apps.

Validation: simulated HTTPS responses exercise version selection, download and
checksum rejection without executing test payloads. A real hosted update and
administrative upgrade on a disposable Windows machine remain required before
claiming end-to-end automatic installation is verified. No feed has been deployed
as part of this local revision.

References: https://docs.github.com/en/rest/releases/releases
https://jrsoftware.org/ishelp/topic_setupcmdline.htm

## TextBridge rename (1.0.1)

The canonical repository/feed now uses ihrammal/textbridge. TextBridge expects
TextBridge-Setup-VERSION.exe. To offer this release to older Clear Read clients,
also publish the identical installer as ClearRead-Setup-VERSION.exe with its
GitHub SHA-256 digest. Both names must contain identical bytes. The production
installer keeps the existing AppId so Windows upgrades the existing app.

## Repository rename — 21 October 2026

The owner requested renaming ihrammal/clear-read to ihrammal/textbridge. The same
repository, commits, tags, issues and releases were retained. Anonymous checks
verified both canonical and old update-feed URLs resolve to v1.0.2, and full
installer downloads through both names match the published SHA-256.

Source defaults and known saved repository/feed URLs now migrate to the canonical
name. Custom endpoints remain unchanged. Published v1.0.2 binaries are unchanged
and continue using GitHub's redirect for their original stored/default URLs.
