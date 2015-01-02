VBoxTextGrab
============

(c) 2014, 2015 Michael Schierl


Introduction
------------

VBoxTextGrab can be used to grab text from VirtualBox VMs running in text mode
(either VGA or framebuffer), providing that a fixed width font is used that is
known by this application.

To grab a screen, click the Grab button and then focus the window of the VM to
be grabbed.

If the grab fails (or is of bad quality), probably the font is not known to
VBoxTextGrab. In this case, attach the Calibrate.VFD image (which is bootable
in BIOS and UEFI mode, and contains standalone calibration tools as COM program
and Perl script) and invoke it while the same font is used as on the screen you
want to grab. Then click the Calibrate button and focus the window of the VM.
In case of multiple calibration screens (the font uses more than 256 glyphs),
the taskbar button will flash or (in the Windows 7 version) a progress bar in
the taskbar will show you when it is safe to advance to the next screen.

Calibration may fail in case the framebuffer does not completely fill the
(virtual) screen. In that case, there is a border calibration pattern you can
calibrate first, then try to calibrate the font again.


Requirements
------------

VBoxTextGrab requires the .NET Framework to run, either Version 4 (or above),
or Version 2 (or above) if you use the .NET 2 version. There is also a version
for Windows 7 (and above) that shows progress bars in the task bar buttons
(which requires Windows 7 and .NET Framework 4 to work).

To compile the source code yourself, you will need "Microsoft Visual Studio
2013 Express for Windows Desktop".


License
-------

VBoxTextGrab is Free Software licensed under GNU General Public License
2 or later.
