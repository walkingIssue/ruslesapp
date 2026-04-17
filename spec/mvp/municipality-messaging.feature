Feature: Municipality messaging
  Users inside a municipality chat need to read recent messages and contribute new ones.

  Scenario: User sees recent municipality messages
    Given a signed-in user has entered a municipality chat
    When the chat view loads
    Then the user should see recent messages for that municipality

  Scenario: User sends a municipality message
    Given a signed-in user has entered a municipality chat
    When the user sends a valid message
    Then the message should appear in the municipality chat
    And other users in the same municipality chat should be able to receive it

  Scenario: Empty or invalid message is rejected
    Given a signed-in user has entered a municipality chat
    When the user attempts to send an empty or otherwise invalid message
    Then the message should not be sent
    And the user should be told why the message was rejected

  Scenario: User cannot send without chat access
    Given a user does not currently have valid municipality chat access
    When the user attempts to send a municipality message
    Then the message should not be accepted

  Scenario: New municipality messages arrive while the user is present
    Given a signed-in user is currently viewing a municipality chat
    When another user sends a message to the same municipality chat
    Then the current user should receive the new message without needing to reload the page
