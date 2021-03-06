# Horizon Changelog
### Beta 0.3.2
* Connections Queue added to the bottom bar
* Settings Menu added
	* Accent Color
	* Allow Unknown IPs
	* Connection Timeout
	* Accept/Deny Timeout
* Error Logging
* Move Accent Color option to setting menu
* Sorting Contacts A-Z
* Made Contacts Load Asynchronously
* Published to Github
### Beta 0.3.3
* Info Popup dismisses when clicking the contacts pane and any button
* Removed blocking server popup
* Changed from Double click to single click with flyout
### Beta 0.3.4
* Added the ability to request a file from the server
	* This means only one person has to operate as the server for both parties to send and receive.
### Beta 0.3.5
* Fixed Request file not sending because the socket closes to early
* Fixed Request Dialog not closing on disconnect
* Changed "Receive File" to "Request File"
* Cancel Flyout not closes the socket rather than letting it disconnect on it's own
### Beta 0.3.6
* Fixed requesting a file causes the server to only see that file (Issue #5)
* Fixed IP Button not closing when clicked twice
* Moved Progress bar for upload / download into the connection view at the bottom.
* Added support for multiple downloads
* Adjusted Help/About layout 
* Changed Help/About Text to reflect [About.md](https://github.com/EpsiRho/Horizon/blob/main/Docs/About.mdown)
* Minor Code Cleanup
* Removed Help Teaching Tips
#### Beta 0.3.6.1 Patch
* One stray line got left on line 1181 causing a crash when sending a file
* Adjusted some text in the help menu
### Beta 0.3.7
* Hitting the enter key while in the add contact dialog boxes will act like hitting the add button
* More Code Cleanup and Commenting
* Fixed an issue with the connection list item not closing when sending a file was denied
* Added a button to quickly open the file explorer at the downloads location
* Updated Github link and Email link
### Beta 0.4.0
* Removed Donate button 
* Switched to port 62832
* Prepping for windows store upload