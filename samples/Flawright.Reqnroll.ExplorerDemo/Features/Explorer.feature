Feature: File Explorer BDD Automation
    Demonstrates Flawright.Reqnroll with Windows File Explorer (shell32 / explorer.exe).
    Uses @launch: to open Explorer directly. Explorer's automation tree varies across
    Windows builds — fallback selectors are documented in comments and in the README.

    # NOTE: Explorer scenarios depend on the Windows shell being available and
    # responsive. On headless CI runners the scenarios may skip if Explorer cannot
    # be launched or attached. The prerequisite hook handles this gracefully.

    @launch:explorer.exe
    Scenario: Open Explorer and verify window is visible
        # After launch, bring the window to the front — Explorer may open behind
        # other windows when triggered programmatically.
        Given I have the application in focus
        Then  the window title should contain "File Explorer"

    @launch:explorer.exe
    Scenario: Use the Search box to search for files
        Given I have the application in focus
        When  I bring the application to the front
        # Primary automationid: SearchBox   Fallback name: Search Box
        And   I wait for selector "automationid:SearchBox"
        And   I click "automationid:SearchBox"
        And   I fill "automationid:SearchBox" with "*.txt"
        And   I press "Enter" on "automationid:SearchBox"
        # After pressing Enter the search results pane or a results header should appear.
        # The results panel control type is typically a List or DataGrid.
        And   I wait for 2000 milliseconds
        Then  the window title should contain "File Explorer"

    @launch:explorer.exe
    Scenario: Navigate to the Windows folder via address bar
        Given I have the application in focus
        When  I bring the application to the front
        # The breadcrumb / address bar.
        # Primary automationid: Address   Fallback name: Address band toolbar
        And   I wait for selector "name:Address band toolbar"
        # Alt+D focuses the address bar — use the keyboard shortcut for reliability
        And   I press "Alt+D" globally
        And   I wait for 500 milliseconds
        And   I type "C:\Windows" globally
        And   I press "Enter" globally
        And   I wait for 1500 milliseconds
        Then  the window title should contain "Windows"
