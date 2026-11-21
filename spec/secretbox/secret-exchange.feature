@proposal @secretbox
Feature: Exchange sealed secrets using a Rustles account
  A participant contributes a secret before reading the other participant's secret.
  Private reading remains possible without recording or location permission.
  These scenarios describe intended behavior and are not executable tests yet.

  Background:
    Given SecretBox is enabled for the test accounts

  Scenario: Open SecretBox through the existing platform menu
    Given I am signed in with a normal Rustles account
    When I select "SecretBox" below "Chat" in the navigation menu
    Then I see my secret exchanges
    And I do not need another account or location permission

  Scenario: Return a new invitee after account confirmation
    Given I opened a valid SecretBox invitation while signed out
    When I register a Rustles account and confirm my email
    And I sign in using the continuation offered
    Then I return to that invitation
    And I have not joined until I choose to join

  Scenario: Confirm the joining person before releasing secrets
    Given I created a box and shared its invitation
    And another account has requested to join
    When both accounts have sealed a secret
    Then neither can read the other's secret until I confirm the joining account

  Scenario: Wait for the other contribution
    Given two accounts are confirmed participants in a box
    And I have sealed my secret
    But the other participant has not sealed theirs
    When I view the box or request to open it
    Then I see that I am waiting
    And I do not receive the other participant's message

  Scenario: Complete an asynchronous exchange
    Given the peer account is confirmed
    And one participant has sealed a secret and signed out
    When the other participant seals their secret
    Then both secrets become available to their respective recipients
    And the first participant can open theirs after signing in later

  Scenario: Preserve fairness when requests race
    Given a box is waiting for its final contribution
    When sealing and cancellation are requested at the same time
    Then the box has one consistent outcome
    And no secret is released from a cancelled box

  Scenario: Recover an uncertain sealing result
    Given I submitted a secret but did not receive a response
    When I retry the same sealing operation
    Then I receive the existing receipt if it was already accepted
    And I do not create or replace a second secret

  Scenario: An unrelated account cannot read a box
    Given I am not a participant in a box
    When I request its details or attempt to open it
    Then I receive no message or participant details

  Scenario: Refuse reused or expired invitations
    Given an invitation was consumed, revoked, or expired
    When another account attempts to join with that invitation
    Then no additional participant is admitted
    And no secret is disclosed

  Scenario: Open privately without device permissions
    Given my box is ready
    And I have denied camera, microphone, and location access
    When I choose "Read privately"
    Then I can read the message addressed to me
    And no recording is started

  Scenario: Start recording before revealing
    Given my box is ready and I have not opened its message
    When I choose "Record and reveal" and allow recording
    Then the message remains hidden until recording has started and I choose to reveal
    And I can stop and preview my recording

  Scenario: Recover when recording is unavailable
    Given my box is ready and its message is still closed
    When the camera is unavailable before revealing
    Then the message remains closed
    And I can choose to read privately instead

  Scenario: Automatic opening does not reveal in the background
    Given I selected automatic opening
    And the application is backgrounded
    When my box becomes ready
    Then no message is revealed in a notification or background view
    And I choose to open it when I return

  Scenario: Require approval to include another person's material
    Given I have recorded my own reaction
    When I prepare a clip containing the other participant's message
    Then they must approve that exact version before the platform exports it
    And changing the approved content requires approval again
    And I can instead export a version containing only my own permitted material

  Scenario: Keep the exchange private when exporting
    Given I approve a permitted clip preview
    When I export it
    Then the file contains the selected video and reviewed captions
    And it contains no private invitation token, account email, or box-access URL
    And it is not automatically posted to a social platform

  Scenario: Cancel the device share flow
    Given a permitted clip is ready
    When I cancel the device share flow
    Then I return to the preview
    And the application does not claim that the clip was posted

  Scenario: Do not bypass sealing through personal-data export
    Given the other participant's secret is not available to me
    When I download my account data
    Then their sealed message is not included

  Scenario: Remove content through expiry or deletion
    Given a box has expired or its content was deleted
    When a participant requests its message or a new media export
    Then the removed content is unavailable
    And the application does not claim to recall previously exported copies
