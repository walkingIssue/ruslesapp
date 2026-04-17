Feature: Account authentication
  Users need a way to create an account, sign in, sign out, and recover access to it.

  Scenario: New user can understand the product before creating an account
    Given a user has not yet created an account
    When the user opens the authentication flow
    Then the user should be able to understand what Rustles does at a high level
    And the user should be able to understand why account creation and location access matter

  Scenario: User creates an account with valid credentials
    Given a user does not yet have an account
    When the user submits a valid email address, username, and password
    Then the user should be shown the registration acknowledgement step

  Scenario: User must confirm age and acknowledge the privacy notice before account creation completes
    Given a user has submitted valid registration details
    When the registration acknowledgement step is shown
    Then the user should be asked to confirm that they are at least 16 years old
    And the user should be shown plain-language privacy and public-chat disclosures

  Scenario: Underage user cannot complete account creation
    Given a user has submitted valid registration details
    When the user says they are under 16 years old in the registration acknowledgement step
    Then the account should not be created
    And the user should be returned to the sign-on page

  Scenario: User can reject the registration acknowledgement step
    Given a user has submitted valid registration details
    When the user rejects the registration acknowledgement step
    Then the account should not be created
    And the user should be returned to the sign-on page

  Scenario: User completes account creation after accepting the registration acknowledgement step
    Given a user has submitted valid registration details
    And the user confirms they are at least 16 years old
    And the user accepts the registration acknowledgement step
    When the registration is finalized
    Then an account should be created
    And the user's age self-attestation should be recorded
    And the user's privacy-notice acceptance should be recorded
    And the user should become signed in

  Scenario: User cannot register with an existing email address
    Given an account already exists for the submitted email address
    When the user attempts to create an account
    Then a new account should not be created
    And the user should be told that the email address is already in use

  Scenario: User signs in with valid credentials
    Given a user already has an account
    When the user signs in with valid credentials
    Then the user should become signed in

  Scenario: User is rejected with invalid credentials
    Given a user already has an account
    When the user signs in with invalid credentials
    Then the user should not become signed in
    And the user should be told that the credentials are invalid

  Scenario: User chooses a permanent username during registration
    Given a user does not yet have an account
    When the user creates an account with a valid username
    Then the chosen username should be saved on the account
    And the username should be treated as permanent

  Scenario: User requests password recovery
    Given a user already has an account
    And the user cannot remember the password
    When the user requests password recovery
    Then the user should be given a way to reset the password

  Scenario: Signed-in user signs out
    Given a user is signed in
    When the user signs out
    Then the user should no longer be signed in
