Feature: Paint Draw Automation
    Demonstrates Flawright.Reqnroll click-and-drag drawing in MS Paint (mspaint.exe).
    Uses @launch:mspaint.exe to launch Windows 11 Paint (modern app).

    # NOTE: mspaint.exe is the Windows 11 "Paint" modern app. On Windows Server 2025 / CI
    # images it may not be installed. The PaintDrawPrerequisites hook skips all scenarios
    # gracefully when mspaint.exe is absent, so CI does not fail.
    #
    # Canvas selector: automationid:DrawingIsland is used for modern Windows 11 Paint.
    # If that is absent (older build), class:MSPaintView is tried as a fallback.
    # Both attempts are documented in PaintDrawStepDefinitions.cs.

    Background:
        Given I have the application in focus

    @launch:mspaint.exe
    Scenario: Draw a line on the canvas
        When I draw a line on the Paint canvas from (100, 100) to (300, 200)
        Then the Paint canvas should be visible

    @launch:mspaint.exe
    Scenario: Draw a rectangle shape via toolbar
        When I click the Rectangle tool in Paint
        And  I draw a rectangle on the Paint canvas from (120, 120) to (280, 220)
        Then the Paint canvas should be visible

    @launch:mspaint.exe
    Scenario: Demonstrate DragToAsync on a color swatch
        When I drag color swatch "name:Black" to the canvas in Paint
        Then the Paint canvas should be visible
