# USB-WriteProtect
USBWriteProtect is a .NET project proof of concept at allowing write access to a USB device that is whitelisted and denying write access to a USB device that is blacklisted. All USB devices are blacklisted by default unless specified as a whitelisted USB device in the project (hardcoded for the moment).

It has the ability to have concurrent/simultaneous readwrite and readonly devices on the computer at the same time.

This project can have multiple purpose where you would want to deny write access to all plugged USB devices except a few ones that you define in the whitelist project.

It can be used to prevent transferring files from a computer to a USB drive. The user can still access to the files on its USB drive in a readonly mode and can transfer them to the PC but cannot transfer files from the PC to the USB drive. Only an administrator should have whitelisted his own USB drives.

Of course, the logged in user should be a regular limited user (not administrator), so the Windows settings cannot be changed and the service process cannot be killed. To be really effective, the computer should have limited or no access to internet at all so that people cannot transfer files in any other way (cloud storage) except USB.

You need to run Visual Studio as administrator to use this project.

Look inside Program.cs to see how to whitelist USB devices. This project can be run as a console for debugging purposes or deployed as a service for production use (both mode are currently supported in the Visual Studio solution). In debug mode, you can output USB device details in console by plugging them in the computer. When debugging with the console, remember to gracefully close the console (not X, but hit a key on the console), because the project uses Windows Registry and have to properly clean the registry to come back in the state that all USB devices are allowed write access if the project is not running.

Feel free to try it, test it, play with it and adapt it to your needs! Comments/issues are welcome.
