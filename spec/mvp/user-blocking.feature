Feature: User blocking
  Users need a way to block other users so unwanted messages stop appearing in their chat experience.

  Scenario: User blocks another user
    Given a signed-in user can identify another user in municipality chat
    When the user blocks that other user
    Then the block should be saved for the blocking user

  Scenario: Blocked user's messages are hidden from the blocker
    Given a signed-in user has blocked another user
    When the blocked user has messages in the municipality chat
    Then those messages should not be displayed to the blocking user

  Scenario: Blocking one user does not hide everyone else's messages
    Given a signed-in user has blocked another user
    When the signed-in user views the municipality chat
    Then messages from non-blocked users should still be displayed

  Scenario: User can unblock a previously blocked user
    Given a signed-in user has blocked another user
    When the user removes the block
    Then the blocked user should no longer be treated as blocked for that user

  Scenario: Blocking is personal to the blocker
    Given one signed-in user has blocked another user
    When a different signed-in user views the same municipality chat
    Then the different user should not inherit that block automatically
