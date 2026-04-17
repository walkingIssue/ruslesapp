Feature: Municipality chat access
  A resolved user should be able to enter the municipality chat that matches the current municipality.

  Scenario: Resolved user enters municipality chat
    Given a signed-in user has a resolved municipality
    When the user opens the municipality chat
    Then the user should enter the chat for that municipality
    And the municipality name should be visible to the user

  Scenario: User sees chat availability information
    Given a signed-in user has a resolved municipality
    When the user opens the municipality chat
    Then the user should be able to see whether the chat is active
    And the user should be able to see lightweight participation information for that municipality

  Scenario: Unresolved user cannot enter municipality chat
    Given a signed-in user does not have a resolved municipality
    When the user opens the municipality chat
    Then the user should not enter a municipality chat
    And the user should be shown how to retry location resolution

  Scenario: Municipality changes after a new resolution
    Given a signed-in user already has a municipality chat context
    And a later location resolution maps the user to a different municipality
    When the app refreshes municipality chat access
    Then the user should not continue to be treated as if they are still in the old municipality
