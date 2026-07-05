# smseventphoto

Event photo sharing using SMS.

## Landing page

This repository includes a static landing page that automatically refreshes and shows the latest SMS photo/message entries.

- Open `/home/runner/work/smseventphoto/smseventphoto/index.html` in a browser.
- The page fetches data from `GET /api/sms-events` every 10 seconds.

### Expected API response

The frontend accepts either:

- An array of entries, or
- An object with an `events` array

Each entry can include:

- `message` or `text`
- `phoneNumber` or `from`
- `receivedAt` or `timestamp` (ISO date/time)
- `photoUrl`, `mediaUrl`, or `imageUrl`
