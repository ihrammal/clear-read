Clear Read manual screen reader test sheet.

This sheet is for a person who uses a screen reader. Each step is one sentence. Write down what you hear after each step. If a step behaves differently from the expected result, note the name of the screen reader that was running.

Before you start.

Clear Read is installed from ClearRead-Setup-0.11.1.exe and the Start menu group is called Clear Read. The sample picture called arabic.png sits in the artifacts accessibility-tests folder and contains two Arabic lines: the first line says hello world and the second line says this is a test for Arabic text recognition. If a step asks you to press a shortcut, make sure the Clear Read window itself is the active window first.

Part one. The welcome and the first look.

Start Clear Read from the Start menu without a screen reader running yet. Turn on your screen reader now. Move focus to the Clear Read window. Press the Tab key once. Expected: focus reaches the document area. Press Shift and Tab once. Expected: focus reaches the status area. Press F6. Expected: focus returns to the document area. Press Shift F6. Expected: focus returns to the status area.

Part two. NVDA.

Run NVDA. Start Clear Read. Press Control O. Expected: an Open dialog appears. Choose the arabic.png picture and press Enter. Expected: the status bar says Opened arabic.png. Ready for recognition. and NVDA announces it without you visiting the status area. Press Control R. Expected: NVDA announces Recognition started. almost immediately. Wait until recognition finishes. Expected: NVDA announces Recognition complete. and the caret sits at the start of the Arabic text. Press the Up arrow key. Expected: NVDA reads copy of the first Arabic line word by word with correct Arabic pronunciation. Repeat the Control O step again. Expected: NVDA asks whether to save the recognized text; answer Cancel and the dialog closes without losing your text. Press Enter on the OK button of any message box. Expected: the box closes. Press Escape on the same kind of box. Expected: the box closes again.

Part three. JAWS.

Close NVDA and start JAWS. Note that the free JAWS session lasts only a few minutes, so you may need to restart more than once. Start Clear Read. Open arabic.png with Control O. Recognize with Control R. Expected: JAWS announces the new status messages as they arrive and each message is spoken one time only, not twice. If you hear the completion message twice, write that down. Select one word in the text and press Control F. Expected: the Find dialog appears and its name and the field names are spoken. Press Escape. Expected: the dialog closes and focus returns to the document area. Press the Insert key and the F4 combination anywhere. Expected: JAWS runs its own command and Clear Read keeps working.

Part four. Narrator.

Close JAWS and start Narrator with Control Windows Enter. Start Clear Read. Open arabic.png and recognize it. Expected: Narrator announces the status messages. Turn on scan mode with Caps Lock Space and press the arrow keys through the recognized text. Expected: the text is readable character by character and line by line. Press Control comma. Expected: the preferences dialog appears and every control inside announces its label. Press Escape. Expected: the dialog closes and Narrator announces the document area again.

Part five. A braille display with Arabic text.

Run your screen reader with a braille display connected and set to uncontracted Arabic braille output. Recognize the Arabic picture. Expected: the Arabic text appears on the braille display in readable Arabic. Move the cursor to the end of the document. Expected: the display follows the cursor. Note if the Arabic letters come out in the wrong direction or as English letters without diacritics, because that tells us whether the direction handling has to be fixed in the app or in the braille table itself.

Part six. High Contrast.

Close all screen readers. Turn on Windows High Contrast from the Settings accessibility page in the Contrast themes section and choose the Aquatic theme. Start Clear Read. Expected: the background turns to the High Contrast colors, the borders stay visible and no text disappears. Open a picture and recognize it. Expected: nothing in the process becomes unreadable. Turn High Contrast off again. Expected: Clear Read changes back to its normal colors without restarting.

Part seven. 200 percent scaling.

Close Clear Read. Sign out and change the system scaling of the built-in display to 200 percent, then sign in again. Start Clear Read, note that the automatic sign-out and sign-in is required because the app must draw at the true system scale rather than the current session only. Expected: window, menus, document area, status area and all dialogs draw but never overflow, cut, or become chip-like or off-screen at 200 percent. Open a picture and recognize it, then open every dialog from the menus once. Expected: none of them is clipped or hides its buttons off screen. Also check the window on screen resolution smaller than usual if you can because dialogs are fitted to the working area.

Part eight. Save and export in every reader.

With your screen reader active, recognize the Arabic picture and press Control S. Expected: the Save dialog appears; save to the Documents folder and the status announces the saved file name. Open the saved text file in Notepad with your reader. Expected: the Arabic text is intact.

Part nine. Speech volume during long work.

Turn on your screen reader. Convert more files while reading the document with the arrow keys. Expected: completion messages interrupt politely without stealing focus, and arrow-key reading resumes cleanly.

What to report.

For each part, record the screen reader name, whether expected results match, any repeated or missing announcements, focus jumps, or braille direction problems, and the exact time a problem appears. If a dialog refuses to close or a shortcut stops working, say where focus was and which shortcut produced it.