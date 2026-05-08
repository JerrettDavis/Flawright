Feature: Notepad BDD Automation
    Demonstrates Flawright.Reqnroll with Windows 11 Notepad.
    Uses @launch:notepad.exe which auto-resolves to the packaged WinUI3 Notepad on Windows 11.

    Background:
        Given I have the application in focus

    @launch:notepad.exe
    Scenario: Type and verify text in Notepad
        When I fill "[name=\"Text editor\"]" with "Hello from Flawright!"
        Then "[name=\"Text editor\"]" should contain "Hello"
        And "[name=\"Text editor\"]" should contain "Flawright"

    @launch:notepad.exe
    Scenario: Clear text in Notepad
        When I fill "[name=\"Text editor\"]" with "Some initial text"
        And  I clear "[name=\"Text editor\"]"
        Then "[name=\"Text editor\"]" should be empty

    @launch:notepad.exe
    Scenario: Verify window title contains Notepad
        Then the window title should contain "Notepad"

    @launch:notepad.exe
    Scenario: Type character-by-character into Notepad
        When I type "BDD rocks!" into "[name=\"Text editor\"]"
        Then "[name=\"Text editor\"]" should contain "BDD rocks"

    @launch:notepad.exe
    Scenario: Screenshot after typing
        When I fill "[name=\"Text editor\"]" with "Flawright screenshot test"
        Then "[name=\"Text editor\"]" should be visible
