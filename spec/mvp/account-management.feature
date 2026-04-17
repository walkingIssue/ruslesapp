Feature: Settings
  Signed-in users need a minimal settings area to manage account state and blocked-user management without leaving the product in a broken state.

  Scenario: User views basic account information
    Given a user is signed in
    When the user opens settings
    Then the user should be able to see the core account information the product exposes
    And the user should be able to see the saved email address and username

  Scenario: User changes password while signed in
    Given a user is signed in
    When the user submits a valid password change
    Then the user's password should be updated

  Scenario: User cannot change password with invalid input
    Given a user is signed in
    When the user submits an invalid password change
    Then the password should not be changed
    And the user should be told why the request failed

  Scenario: User cannot change a permanent username
    Given a user is signed in
    And the user already has a saved username
    When the user opens settings
    Then the username should be shown as permanent
    And the user should not be given a username-change action

  Scenario: User can review blocked users in settings
    Given a user is signed in
    And the user has blocked one or more other users
    When the user opens settings
    Then the user should be able to see the users they have blocked

  Scenario: User can unblock someone from settings
    Given a user is signed in
    And the user has previously blocked another user
    When the user removes that user from the blocked list in settings
    Then the blocked user should no longer be treated as blocked for that user
