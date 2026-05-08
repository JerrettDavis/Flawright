Feature: Calculator BDD Automation
    Demonstrates Flawright.Reqnroll with the Windows Calculator (Store app).
    Uses @aumid: to launch the Calculator directly via its Application User Model ID.

    @aumid:Microsoft.WindowsCalculator_8wekyb3d8bbwe!App
    Scenario: Add two numbers
        Given I have the application in focus
        When  I click "name:One"
        And   I click "name:Plus"
        And   I click "name:Two"
        And   I click "name:Equals"
        Then  "name:Display is 3" should be visible

    @aumid:Microsoft.WindowsCalculator_8wekyb3d8bbwe!App
    Scenario: Clear the display
        Given I have the application in focus
        When  I click "name:Five"
        And   I click "name:Clear"
        Then  "name:Display is 0" should be visible

    @aumid:Microsoft.WindowsCalculator_8wekyb3d8bbwe!App
    Scenario: Verify window title contains Calculator
        Given I have the application in focus
        Then the window title should contain "Calculator"

    @aumid:Microsoft.WindowsCalculator_8wekyb3d8bbwe!App
    Scenario: Multiply two numbers
        Given I have the application in focus
        When  I click "name:Three"
        And   I click "name:Multiply by"
        And   I click "name:Four"
        And   I click "name:Equals"
        Then  "name:Display is 12" should be visible
