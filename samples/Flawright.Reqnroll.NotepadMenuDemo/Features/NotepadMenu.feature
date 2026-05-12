Feature: Notepad Menu Navigation
    Demonstrates Flawright.Reqnroll classic menu navigation and message-box handling
    with Windows Notepad. Uses @launch:notepad.exe which auto-resolves to the packaged
    WinUI3 Notepad on Windows 11, or classic Win32 Notepad on Windows 10 / Server.

    # NOTE: The modern WinUI3 Notepad (Windows 11) exposes menus via UIA Name properties
    # such as "name:File" and "name:Edit". On older builds that do not expose these names,
    # the keyboard accelerators Alt+F and Alt+E can be used via the built-in
    # I press "Alt+F" globally step instead of I click "name:File".

    Background:
        Given I have the application in focus

    @launch:notepad.exe
    Scenario: Navigate the File menu
        When I click "name:File"
        Then "name:New tab" should be visible
        And  "name:Open..." should be visible
        And  "name:Save" should be visible
        When I press "Escape" globally
        Then "name:New tab" should be hidden

    @launch:notepad.exe
    Scenario: Use Edit menu to select-all and copy
        When I fill "class:Edit" with "Select-all copy demo"
        And  I click "name:Edit"
        And  I click "name:Select all"
        And  I click "name:Copy"
        Then "class:Edit" should be visible

    @launch:notepad.exe
    Scenario: Type then trigger unsaved-changes dialog
        When I fill "class:Edit" with "Unsaved content for dialog test"
        And  I trigger unsaved-changes close on Notepad
        Then the unsaved-changes dialog should be visible
        When I click the "Don't save" button in the dialog
