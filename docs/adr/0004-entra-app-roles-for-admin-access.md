# 0004. Entra ID app roles for admin access

## Status

Accepted

## Context

Anyone may read tide data, but only the owner of the project may save and delete locations and imports. The React app runs in the browser and cannot keep a secret, and the API should hold no credentials either. Microsoft Entra ID is free for this use and fits the Azure deployment. Access could be limited by an app role, by group membership, or by checking a list of accounts in the API.

## Decision

- Register the API and the React app as two Entra app registrations in one tenant. The API exposes the delegated scope `access_as_user`, and the React app has permission to it.
- Define an `Admin` app role on both registrations and assign it to the admin account on both. The React app reads the role from its ID token to show the admin tools, and the API requires it in the access token on every write through an `Admin` policy.
- Turn on "Assignment required" for both enterprise applications, so Entra refuses sign-in to any account that is not assigned.
- Sign in with MSAL's redirect flow and PKCE. The React app is a public client with no secret.

## Consequences

- Who is an admin is managed in Entra, not in the code or the database. Giving or taking away access needs no deployment.
- Assigning a group to an app needs Entra ID P1, and an account list in the API would put personal data in configuration. App roles assigned to users need neither.
- The role must be kept on both registrations. An account with it on only one either sees the admin tools but cannot save, or can save but sees no tools.
- Assignment does not apply to a Global Administrator, so refusing other accounts can only be tested with a second account.
- Every origin the app runs on needs its own redirect URI, because Entra does not accept wildcards.
- The redirect flow leaves the page. A form that is open when the session has ended loses its input when the app signs in again.
