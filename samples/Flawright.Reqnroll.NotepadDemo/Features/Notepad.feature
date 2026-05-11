Feature: Notepad BDD Automation
    Demonstrates Flawright.Reqnroll with Windows Notepad (classic Edit control).
    Uses @launch:notepad.exe which auto-resolves to the packaged WinUI3 Notepad on Windows 11,
    or classic Win32 Notepad on Windows 10 / Server. Both use the classic Edit control.

    Background:
        Given I have the application in focus

    @launch:notepad.exe
    Scenario: Type and verify text in Notepad
        When I fill "class:Edit" with "Hello from Flawright!"
        Then "class:Edit" should contain "Hello"
        And "class:Edit" should contain "Flawright"

    @launch:notepad.exe
    Scenario: Clear text in Notepad
        When I fill "class:Edit" with "Some initial text"
        And  I clear "class:Edit"
        Then "class:Edit" should be empty

    @launch:notepad.exe
    Scenario: Verify window title contains Notepad
        Then the window title should contain "Notepad"

    @launch:notepad.exe
    Scenario: Type character-by-character into Notepad
        When I type "BDD rocks!" into "class:Edit"
        Then "class:Edit" should contain "BDD rocks"

    @launch:notepad.exe
    Scenario: Screenshot after typing
        When I fill "class:Edit" with "Flawright screenshot test"
        Then "class:Edit" should be visible
