Feature: Settings App BDD Automation
    Demonstrates Flawright.Reqnroll with the Windows Settings app (modern UIA-driven UI).
    Uses @aumid: to launch Settings via its Application User Model ID.
    Settings has animated page transitions — WaitForSelector steps are used generously.

    # NOTE: All scenarios in this feature will be skipped on Windows Server SKUs
    # (e.g., windows-2025-vs2026 CI runners) where the Settings packaged app is not
    # present or is a stripped version without the full navigation tree.
    # These scenarios are designed to run on Windows 10/11 developer machines.

    @aumid:windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel
    Scenario: Open Settings and navigate to System > About
        Given I have the application in focus
        When  I wait for selector "name:System"
        And   I click "name:System"
        And   I wait for selector "name:About"
        And   I click "name:About"
        # "Device specifications" heading confirms the About page loaded
        Then  I wait for selector "name:Device specifications"
        And   "name:Device specifications" should be visible

    @aumid:windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel
    Scenario: Search within Settings
        Given I have the application in focus
        # The search box may be identified by automationid or name depending on the Windows build.
        # Primary: automationid:SearchBox  Fallback: name:Find a setting
        When  I wait for selector "automationid:SearchBox"
        And   I click "automationid:SearchBox"
        And   I type "display" into "automationid:SearchBox"
        # After typing, at least one search result item should appear in the results list
        And   I wait for selector "controltype:ListItem"
        Then  "controltype:ListItem" should be visible

    @aumid:windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel
    Scenario: Back navigation
        Given I have the application in focus
        When  I wait for selector "name:System"
        And   I click "name:System"
        And   I wait for selector "name:Display"
        And   I click "name:Display"
        And   I wait for selector "name:Back"
        # Navigate back to the System page
        And   I click "name:Back"
        # After going back, the Display nav entry should be visible again in the System page
        And   I wait for selector "name:Display"
        Then  "name:Display" should be visible
