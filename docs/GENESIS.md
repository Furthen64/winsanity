This is the first prompt that was initially launching the development of "winsanity".





\---



Create the first working version of a Windows-only .NET 10 WinForms application that warns the user when Windows Update is causing sustained heavy system load.



The application is intended to solve this problem:



Windows Update can perform heavy background servicing with very little visible indication. Disk active time can reach 100%, CPU usage can become high, antivirus software may scan changed files, and the machine can become unpleasant or nearly unusable. The user wants a very obvious front-and-center warning when this happens.



The application should remain almost completely idle during normal use. It should NOT continuously poll Windows Update or continuously monitor system load while nothing is happening.



Target environment:

\- Windows 11

\- .NET 10

\- C#

\- WinForms

\- x64

\- No admin privileges should be required for normal operation if possible

\- Prefer built-in .NET / Windows APIs

\- Keep external dependencies minimal

\- No web services

\- No telemetry

\- No database

\- No installer yet

\- Simple source tree and maintainable code



Working name:

WUWatch



The first version should prioritize functionality, observability, and clean architecture over visual polish.



==================================================

CORE BEHAVIOR

==================================================



The program should have three logical states:



1\. IDLE

2\. ARMED

3\. WARNING



IDLE:

\- Application is running in the system tray.

\- No visible window.

\- Do not sample CPU or disk performance continuously.

\- Subscribe to the Windows Update event log and wait for relevant events.

\- This should be event-driven rather than periodically calling PowerShell or Get-WinEvent.



ARMED:

\- Enter this state when a relevant Windows Update event is received.

\- Begin sampling system load once per second.

\- Stay armed for a long period because Windows Update work often happens in multiple bursts separated by quiet periods.

\- Every new relevant Windows Update event resets the armed-period expiration timer.



WARNING:

\- Enter when sustained system load exceeds configured thresholds.

\- Show a large, obvious WinForms window in the center of the screen.

\- The form should be TopMost.

\- The form should clearly state that Windows Update activity was detected and the machine is under heavy system load.

\- Continue monitoring while the warning is shown.

\- Do not immediately close the warning when load briefly falls.



==================================================

WINDOWS UPDATE EVENT WATCHING

==================================================



Monitor:



Microsoft-Windows-WindowsUpdateClient/Operational



Use the Windows event log subscription API, preferably:



System.Diagnostics.Eventing.Reader.EventLogWatcher



Do NOT implement this by periodically spawning PowerShell.



For the first version:



Event ID 41:

"An update was downloaded."



should definitely count as a relevant event and transition IDLE -> ARMED.



Event ID 26:

"Windows Update successfully found N updates."



should NOT by itself arm the application because merely scanning for updates is normally harmless.



Structure the implementation so additional relevant event IDs can easily be added later.



Record the following information about each relevant event:

\- timestamp

\- event ID

\- event message if available



The application should handle the event log being unavailable or inaccessible gracefully rather than crashing.



==================================================

SYSTEM LOAD MONITORING

==================================================



Once ARMED, sample once per second.



Primary metric:

\- Physical disk active time percentage for the Windows system disk / C: drive



Secondary metric:

\- Overall CPU utilization percentage



The important disk metric is ACTIVE TIME, not MB/s throughput.



Example real-world situation this application is intended to detect:



Disk active time: 100%

Read throughput: approximately 130 MB/s

Write throughput: approximately 50 MB/s



The SSD is nowhere near its sequential bandwidth limit, but Windows becomes nearly unusable because many small/random I/O requests saturate active time.



Implement a reliable Windows-compatible way to obtain:

\- total CPU utilization %

\- system disk active time %



If PerformanceCounter or another Windows API is used, verify that it works correctly on modern .NET 10 and Windows 11.



Do not hard-code assumptions about physical disk instance names if they can be discovered.



Make the implementation robust enough to identify the disk containing C:.



==================================================

ROLLING AVERAGES

==================================================



Do not trigger based on a single one-second spike.



Maintain rolling averages.



Initial defaults:



Sample interval:

1 second



Warning evaluation window:

30 seconds



SHOW warning if either:



average disk active time >= 80%



OR



average CPU utilization >= 85%



for the configured rolling window.



Make thresholds configurable.



==================================================

LONG HYSTERESIS

==================================================



This is important.



Windows Update often behaves like this:



10:04 update activity

10:05 very heavy disk load

10:20 quiet

10:45 another servicing burst

11:10 quiet

11:30 another burst



Do not assume that one quiet period means Windows Update is finished.



Initial defaults:



Warning hide thresholds:

\- Disk active time < 50%

\- CPU < 60%



Both conditions must remain satisfied continuously for:



15 minutes



before automatically hiding the large warning window.



If either metric rises above its clear threshold during those 15 minutes:

\- reset the quiet timer to zero



After the warning is hidden:

\- remain in ARMED state



Remain ARMED for:



8 hours



after the most recent relevant Windows Update event.



Every new relevant Windows Update event resets the 8-hour timer.



Return ARMED -> IDLE only when:

\- 8 hours have passed since the most recent relevant Windows Update event

\- there is no active warning condition



When returning to IDLE:

\- stop the one-second CPU/disk sampling

\- return to only waiting for Windows Update events



==================================================

WARNING WINDOW

==================================================



Create a large, obvious WinForms warning form.



It should be:

\- centered

\- TopMost

\- visually difficult to overlook

\- normal window borders are acceptable

\- user must still retain ultimate control over the program



Display at least:



WINDOWS UPDATE ACTIVE



Windows Update activity was detected and is currently associated with significant system load.



Current:

\- Disk active time

\- Disk rolling average

\- CPU utilization

\- CPU rolling average

\- Memory usage if easily available



Episode information:

\- Time first update event was seen

\- Time most recent update event was seen

\- Time warning started

\- Current quiet-time countdown, if applicable



Example:



WINDOWS UPDATE ACTIVE



Disk active time:     97%

30 sec disk average:  92%



CPU:                  48%

30 sec CPU average:   51%



Update activity began: 10:04:41

Last update event:     10:17:03



System quiet time:

02:14 / 15:00 required before warning closes



The form should update periodically without freezing.



==================================================

DISMISSAL / FORCE CLOSE BEHAVIOR

==================================================



The user must be able to close or dismiss the warning, but it should not be effortless to accidentally disregard it.



Clicking the normal X while WARNING is active should NOT immediately make the warning disappear.



Instead show a confirmation dialog explaining:



"Windows Update activity is still being monitored.



Closing this warning may hide ongoing system load."



Provide two choices:



1\. Keep Warning Open

2\. Dismiss Warning



Dismiss Warning should require deliberate confirmation.



A simple first-version implementation is fine, such as:

\- second confirmation dialog

or

\- a button that must be held for approximately 3 seconds



Do not create intentionally hostile behavior.

Do not try to make the process impossible to terminate.

Task Manager must always be able to terminate it normally.



==================================================

TRAY ICON

==================================================



The application should use NotifyIcon and primarily live in the Windows system tray.



Tray menu:



Status

\- show current state: IDLE / ARMED / WARNING



Show Window

\- available when appropriate



Settings

\- can be minimal for first version



Exit

\- exits application completely

\- require a confirmation dialog



The application should use ApplicationContext rather than depending on the warning form as the application's lifetime owner.



The application must continue running when no form is visible.



==================================================

SETTINGS

==================================================



Store settings locally in a simple JSON file.



Suggested defaults:



{

&#x20; "sampleIntervalSeconds": 1,

&#x20; "warningAverageSeconds": 30,



&#x20; "diskShowPercent": 80,

&#x20; "cpuShowPercent": 85,



&#x20; "diskHidePercent": 50,

&#x20; "cpuHidePercent": 60,



&#x20; "warningHideQuietMinutes": 15,

&#x20; "armedEpisodeHours": 8,



&#x20; "relevantWindowsUpdateEventIds": \[41]

}



Keep settings implementation simple.



If the settings file does not exist:

\- create/use defaults



If it is malformed:

\- do not crash

\- fall back to defaults

\- log the error



A graphical settings editor is optional for this first version.

Editing the JSON manually is acceptable initially.



==================================================

IN-MEMORY EPISODE HISTORY

==================================================



Maintain a small history for the current Windows Update episode.



Examples:



10:04:41 Windows Update event 41

10:04:41 Monitoring armed

10:05:18 Disk warning threshold crossed

10:05:48 Warning displayed

10:18:52 Load entered quiet range

10:24:15 Quiet timer reset

10:32:40 Load entered quiet range



This does not need to be a high-frequency telemetry database.



Keep:

\- important state changes

\- relevant Windows Update events

\- warning transitions

\- quiet timer transitions



Expose this history through either:

\- a Details area on the warning form

or

\- a simple tray menu action / separate small form



==================================================

LOGGING

==================================================



Add simple local diagnostic logging.



For example:



%LOCALAPPDATA%\\WUWatch\\wuwatch.log



Log:

\- startup

\- shutdown

\- state changes

\- event-log watcher startup/failure

\- relevant Windows Update events

\- monitor start/stop

\- threshold crossings

\- warning show/hide/dismiss

\- exceptions



Avoid logging once per second unless debugging is enabled.

The normal log should remain small.



==================================================

SUGGESTED PROJECT STRUCTURE

==================================================



Keep the project small and understandable.



Something similar to:



WUWatch/

&#x20;   WUWatch.csproj

&#x20;   Program.cs

&#x20;   TrayApplicationContext.cs

&#x20;   WindowsUpdateWatcher.cs

&#x20;   SystemLoadMonitor.cs

&#x20;   RollingAverage.cs

&#x20;   UpdateEpisode.cs

&#x20;   WarningForm.cs

&#x20;   AppSettings.cs

&#x20;   AppLogger.cs



You may adjust this structure if there is a clear reason.



Responsibilities should remain separated.



WindowsUpdateWatcher:

\- event log subscription only



SystemLoadMonitor:

\- system metrics

\- one-second sampling

\- rolling averages



UpdateEpisode:

\- current episode timestamps/history/state-related data



TrayApplicationContext:

\- application lifetime

\- state machine

\- transitions

\- tray icon



WarningForm:

\- UI only

\- display current state

\- deliberate dismissal behavior



AppSettings:

\- JSON configuration



==================================================

STATE MACHINE

==================================================



Implement the state machine explicitly.



Expected normal flow:



IDLE

&#x20; -> Windows Update Event 41

ARMED

&#x20; -> sustained high load

WARNING

&#x20; -> 15 minutes sustained low load

ARMED

&#x20; -> no update event for 8 hours and no warning condition

IDLE



Additional flow:



WARNING

&#x20; -> user deliberately dismisses warning

ARMED



If load becomes severe again after manual dismissal:

\- the warning MAY appear again

\- do not treat manual dismissal as disabling monitoring for the whole episode



For the first version, allow it to reappear after load has first fallen below the show threshold and then later crosses the warning threshold again.



Avoid repeatedly reopening the window every few seconds while load remains continuously high.



==================================================

DEVELOPMENT / TESTABILITY

==================================================



Real Windows Update events are inconvenient for development.



Add a development/test mechanism that allows the state machine to be exercised without waiting for Windows Update.



For example:

\- debug-only tray menu entries

or

\- command-line switches



Useful simulated actions:



Simulate Windows Update Event

Simulate High Load

Simulate Normal Load

Force Warning

Return To Idle



Do not bake fake data into production behavior.



The simulation mechanism should make it possible to verify:



IDLE -> ARMED

ARMED -> WARNING

WARNING -> quiet countdown

quiet countdown reset

WARNING -> ARMED

ARMED -> IDLE



Make time-related thresholds injectable/configurable enough that automated tests can use seconds rather than waiting 15 minutes or 8 hours.



==================================================

TESTS

==================================================



Add unit tests where practical.



The most important part to test is the logic rather than WinForms itself.



Test:

\- rolling-average calculation

\- warning threshold crossing

\- OR semantics between disk and CPU warning thresholds

\- clear condition requires BOTH disk and CPU below clear thresholds

\- quiet timer resets correctly

\- new update event resets episode timeout

\- state transitions

\- manual warning dismissal returns to ARMED

\- warning is not immediately reopened while continuous high-load condition has never cleared



Keep Windows-specific API wrappers separable from pure state logic so the state machine can be tested easily.



==================================================

STARTUP

==================================================



For this first version, do NOT automatically install the app into Windows startup.



However, structure things so startup support can easily be added later.



Do not create a Windows service.



This is a normal per-user tray application.



==================================================

BUILD REQUIREMENTS

==================================================



Use .NET 10.



The project should build using:



dotnet build



Prefer:



<TargetFramework>net10.0-windows</TargetFramework>

<UseWindowsForms>true</UseWindowsForms>



Enable nullable reference types.



Use modern C# style but avoid unnecessary abstractions or dependency-injection frameworks.



Do not use MVVM.



Do not introduce Avalonia, WPF, Electron, ASP.NET, SQLite, or other frameworks that are unnecessary for this application.



==================================================

IMPORTANT ENGINEERING RULES

==================================================



Before implementing Windows performance counters or EventLogWatcher details:



1\. Verify the APIs actually work under .NET 10 / Windows 11.

2\. If an additional Microsoft/System package is required, add the minimal appropriate package.

3\. Do not invent API names.

4\. Build after establishing the initial project.

5\. Fix compiler errors before continuing.

6\. Run tests.

7\. Keep the first version functional rather than overengineered.



If disk active-time detection turns out to have complications due to Windows counter instance naming, solve that properly rather than replacing the metric with MB/s.



The application exists specifically because active-time saturation can make the machine unusable even when throughput is relatively low.



==================================================

DELIVERABLE

==================================================



Produce a complete first working version.



At the end:



1\. Build the application.

2\. Run the automated tests.

3\. Report the resulting project structure.

4\. Explain how Windows Update event detection is implemented.

5\. Explain how CPU and disk active time are measured.

6\. Explain the state transitions.

7\. Give exact instructions for launching the application for the first test.

8\. Explain how to use the simulation/debug controls to test it without waiting for a real Windows Update.

9\. Mention any Windows permissions or counter-access limitations discovered during implementation.



Do not stop after scaffolding.

Do not only write a design document.

Implement, build, test, and leave the repository in a runnable state.

