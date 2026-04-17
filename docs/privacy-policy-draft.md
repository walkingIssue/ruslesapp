# Rustles Privacy Policy Draft

Status: draft for internal review. This document is based on the current MVP application flow and is not legal advice. It must be verified against the final implementation, hosting setup, and release configuration before publication.

Last updated: 2026-04-12

## 1. Who this policy applies to

This policy describes how Rustles processes personal data when a person uses the Rustles MVP service.

Rustles is a location-linked public chat product. In the MVP flow, a user creates an account, confirms age and privacy acknowledgement during registration, is resolved into a municipality based on device location, and can then participate in municipality chat.

## 2. Data controller

Controller: `[Replace with legal name or sole trader name]`

Contact email: `[Replace with privacy/contact email]`

Postal address: `[Replace if needed]`

If a data protection officer is appointed, add their contact details here.

## 3. Product age rule

Rustles is currently intended for users who are at least `16` years old.

As part of registration, Rustles asks the user to confirm that they are at least `16` years old. If the user says they are under `16`, account creation does not continue.

## 4. What personal data Rustles processes

Based on the current MVP flow, Rustles is expected to process the following categories of personal data.

### Account data

- email address
- chosen username
- password-derived authentication data or other password information needed for authentication

### Registration acknowledgement data

- age self-attestation
- privacy-notice acknowledgement
- related timestamps and minimal supporting metadata needed to show that the acknowledgement step was completed

### Blocking data

- records showing which users a given user has blocked

### Live service data

- live location input used to resolve the user's municipality
- live chat messages sent in municipality chat

### Technical and security data

- minimal operational and security logs needed to run, secure, and troubleshoot the service

Important current MVP assumptions:

- Rustles does **not** persist device location data after municipality resolution completes.
- Rustles does **not** persist sent chat messages in the MVP flow.
- Rustles does **not** use cookies in the MVP flow.

## 5. How Rustles uses personal data

Rustles uses personal data for the following purposes:

- to create and maintain user accounts
- to authenticate users and secure access to the service
- to apply the product's age gate during registration
- to record that the user completed the registration acknowledgement step
- to resolve a user's current municipality from live device location
- to connect the user to municipality chat
- to operate user blocking
- to protect the service, investigate abuse, and troubleshoot technical issues

## 6. Legal bases for processing

The current MVP design suggests the following legal bases. These must be verified against the final implementation before launch.

- Account creation, sign-in, password management, municipality chat access, and blocking:
  performance of a contract, or steps requested by the user before entering into a contract
- Registration age gate, privacy-notice acknowledgement recording, and minimal security logging:
  legitimate interests and, where applicable, compliance/accountability obligations under data protection law

If the final implementation relies on any consent-based processing, the live policy must describe that clearly and separately.

## 7. Public-chat warning

Rustles connects the user to a live chat that currently reflects the municipality of the user's device.

Messages sent in that chat are publicly displayed to other users in the same chat context and are available for those users to read while the message is live in the service.

Although Rustles does not currently intend to persist sent messages in the MVP flow, other users may still save, copy, screenshot, record, or otherwise retain what is shared in public chat. Users should avoid sharing information they would not want other people to keep.

## 8. Location data

Rustles uses live device location to determine the municipality context for the user.

Under the current MVP design:

- live location is used for municipality resolution
- location data is not intended to be persisted after that resolution step completes
- Rustles is not intended to build a stored location history for the user

If this changes, the privacy policy must be updated before the new behavior is introduced.

## 9. Message data

Under the current MVP design:

- sent messages are used to operate live municipality chat
- Rustles does not intend to persist sent messages in storage as part of the MVP flow

If message persistence, moderation retention, or archival behavior is added later, the policy must be updated before launch of that behavior.

## 10. Cookies and similar technologies

Rustles does not currently intend to use cookies in the MVP flow.

If cookies, local storage, session identifiers, analytics tags, or similar technologies are added later, this section must be updated before they are used.

## 11. Who Rustles shares personal data with

Rustles may need to share or make personal data available to:

- hosting and infrastructure providers
- email delivery providers, if password recovery or account emails are implemented
- service providers acting on Rustles' behalf
- authorities or other parties where Rustles is legally required to do so

The final published version should name the actual categories and, where appropriate, the actual providers used in production.

## 12. International transfers

This draft assumes Rustles should avoid unnecessary transfers of personal data outside the EU/EEA.

The final published version must be updated to reflect the actual hosting and vendor setup, including any transfers outside the EU/EEA and the legal mechanism used for those transfers.

## 13. How long data is kept

The exact retention periods must be verified against the final implementation. Based on the current MVP flow, the intended direction is:

- account data: while the account is active, and then only as long as needed for security, legal, or account-closure handling
- age self-attestation and privacy-notice acknowledgement: for as long as needed to demonstrate the registration acknowledgement record
- block relationships: until the user removes the block or the account is deleted, unless longer retention is necessary for a legal or security reason
- live location data: not persisted after municipality resolution
- sent messages: not persisted in the MVP flow
- technical/security logs: only for a limited period necessary for operations and security

The final published version should replace this section with actual retention periods or clearly stated retention criteria.

## 14. Data subject rights

Depending on the circumstances, users may have rights under the GDPR, including the right to:

- receive information about how their personal data is processed
- request access to their personal data
- request rectification of inaccurate data
- request erasure of personal data
- request restriction of processing
- object to certain processing
- request data portability where applicable
- lodge a complaint with the Swedish Authority for Privacy Protection (IMY)

Requests can be sent to: `[Replace with rights/privacy email]`

Users in Sweden can also lodge a complaint with IMY: [https://www.imy.se/en/](https://www.imy.se/en/)

## 15. Security

Rustles aims to apply appropriate technical and organisational measures to protect personal data.

At a minimum, the current design direction expects:

- no plaintext password storage
- minimal logging
- access controls for stored account data
- reasonable protection against unauthorised access, loss, or misuse

The final published version should be checked against the real deployment and security implementation.

## 16. Changes to this policy

Rustles may update this policy as the product evolves. If the way personal data is processed changes materially, the policy should be updated before or when that change goes live.
