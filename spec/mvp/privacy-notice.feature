Feature: Privacy notice
  Users should be told clearly what Rustles stores, what it does not store, and how the first MVP handles data.

  Scenario: User can read the MVP privacy notice before or during account use
    Given a user is using Rustles
    When the user opens the privacy notice
    Then the user should be able to understand what account data Rustles stores
    And the user should be able to understand what Rustles does not persist

  Scenario: User sees the privacy notice during registration acknowledgement
    Given a user has submitted valid registration details
    When the registration acknowledgement step is shown
    Then the user should be able to read the MVP privacy notice before account creation completes

  Scenario: Privacy notice acceptance is recorded when account creation completes
    Given a user has submitted valid registration details
    And the user accepts the registration acknowledgement step
    When account creation is finalized
    Then the user's privacy-notice acceptance should be recorded

  Scenario: Privacy notice explains stored account data
    Given a user opens the privacy notice
    When the user reads the section about stored data
    Then the notice should say that Rustles stores the user's email address
    And the notice should say that Rustles stores the user's chosen username
    And the notice should say that Rustles stores the user's password information for authentication

  Scenario: Privacy notice explains what is not persisted
    Given a user opens the privacy notice
    When the user reads the section about non-persisted data
    Then the notice should say that location data is not persisted
    And the notice should say that sent messages are not persisted

  Scenario: Privacy notice explains the public-chat nature of messages
    Given a user opens the privacy notice
    When the user reads the section about public chat behavior
    Then the notice should say that Rustles connects the user to a live chat reflecting the municipality of the device
    And the notice should say that sent messages are publicly displayed for others in that chat to read
    And the notice should say that other users may still save or persist messages outside Rustles even though Rustles does not persist them

  Scenario: Privacy notice explains cookie usage
    Given a user opens the privacy notice
    When the user reads the section about cookies
    Then the notice should say that Rustles only uses strictly necessary authentication cookies
