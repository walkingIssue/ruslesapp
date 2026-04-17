Feature: Municipality resolution
  The system needs to resolve a user's location into a municipality before municipality chat can work.

  Scenario: User location resolves to a municipality
    Given a user has granted access to location data
    And the user's coordinates are inside a supported Swedish municipality
    When the system resolves the user's location
    Then the user should be assigned a municipality result
    And the app should clearly tell the user that they have been connected to a municipality context
    And the app should explain that the user is being connected to local conversation in that area

  Scenario: User location is outside supported coverage
    Given a user has granted access to location data
    And the user's coordinates are outside supported coverage
    When the system resolves the user's location
    Then the user should receive an unresolved location result

  Scenario: Location cannot be resolved confidently
    Given a user has granted access to location data
    And the system cannot confidently resolve the coordinates to a municipality
    When the system resolves the user's location
    Then the user should receive an unresolved location result
    And the system should not assign a municipality guess as if it were certain

  Scenario: User has not granted location access
    Given a user has not granted access to location data
    When the app needs to resolve the user's municipality
    Then the user should be asked to provide or enable location access
