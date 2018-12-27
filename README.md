# TcJoy
[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=8YQRPBYQVAJ36&source=url)

## Summary

TcJoy is a free program that connects a USB (Wired and Wireless) Xbox 360 controller to a Beckhoff TwinCAT PLC Runtime.

## Overview

![flowchart](https://github.com/evanmj/TcJoy/blob/master/Screenshots/Flowchart.png)

## Hardware

The hardware tested is as follows:

https://amzn.to/2CBotpW - Wireless USB PC Adapter

https://amzn.to/2RnXXJ2 - Black Xbox 360 Controller

or as a bundle:

https://amzn.to/2Snv0d8 - Wireless Controller + Single USB (untested, but should work)

Or wired:

https://amzn.to/2RgCOAo - Wired controller + Cable

## How it works

The program is written in vb.net.  It utilizes TwincatADS.dll implmentation provided by Beckhoff with the free install of TwinCAT 3.1.x.
It also leverages J2i.net's XInputWrapper, which takes some of the pain out of working with DirectX on windows to access the xbox 360 controller.  
The third component is a "sister" function block that the end user must import and instantiate in the their twincat runtime.  The user must call the function block at all times.  The function block's primary use is to provide standard names and memory space for TcJoy to write over ADS, however, the function block also serves to make the controller data "fail safe" in the event of a network outage, battery rundown, or other lack of connection.  

## Watchdog Functionality

Imagine you are using a controller to jog a large servo axis.  If the network connection fails, or the controller powers down or goes out of range, the jog input could still be set true!  That is bad, and you best hope an E-Stop is handy (as it should be at all times).  I've seen this issue using Beckhoff's built in twincat visualizations, and it is not fun.

To solve the problem, the settings allow a watchdog timer to be set on the PLC that is a bit longer than the ADS cyclic write time.  Values of 100ms and 200ms work well.  In that case, if an update from TcJoy is not received by the TcJoy Function block on the PLC runtime within 200 milliseconds, it will null the outputs for the WD Dead duration, and require the user to release all buttons before the inputs become active again.  

Provided you use "positive" logic when enabling machine functions, it should work as expected and cease machine operation if the controller or TcJoy program lose connection with the PLC runtime.

## Installation

The executable is "run in place", and currently does not come with an installer.  

You can get the latest runtime files here:

https://github.com/evanmj/TcJoy/tree/master/TcJoy%20.NET%20Project/TcJoy/bin/Debug

You'll also need the Function block here as PLCOpen XML:

https://github.com/evanmj/TcJoy/blob/master/TwinCAT%203%20Funciton%20Block%20(PLC%20Open%20XML)/FB_TcJoy.xml

Optionally, you can test with a complete "bare bones" Twincat project here:

https://github.com/evanmj/TcJoy/tree/master/TcJoy%20TwinCAT%20Project

The basic steps are:

- Acquire TcJoy PC App
- Make instance of FB_TcJoy in your PLC program, and call the function block every PLC scan.
- Set appropriate settings
- Connect Xbox Controller
- Use the connect button to connect, then go to the "Usage" section below to use your new PLC variables.


## Settings

There are various settings to configure the TcJoy runtime.  

![settings](https://github.com/evanmj/TcJoy/blob/master/Screenshots/ConnectionTab.png)

The TcJoy.exe program must be run on the PC with the USB connection to the xbox controller.  That means you can hook into the PLC directly, or into a 'dev' computer.  TcJoy has no problems connecting over the network, but the controller and Tcjoy.exe must run on the same PC.

**AMS NetID**: [127.0.0.1.1.1] - If running from a 'dev' laptop, etc, you will need to configure a route to the PLC and input the AMS NetID in this field.  The default of 127.0.0.1.1.1 is like a loopback, meaning "this machine is where I'll find my PLC runtime".

**ADS Port**: [851] - This is the AMS port.  TC2 is 801,811,821,831 (I belive) for runtimes 1..4 respectively.  TC3 is 851,861,871,881.  Most people will have just one runtime, 851, so use that.

**ADS Rate**: [100ms] - This is the rate at which the data will be sent to the PLC over ADS.  20ms is about the max you should do here, but 100ms is 10 times a second, which is typically plenty fast.  Windows, or specifically the code in TcJoy, introduces about 10-12ms of latency, but it is not really an issue for most human control.

**ADS Watchdog**: [200ms] - This is the watchdog timer explained in the "watchdog" section of this readme.  Basically, if no updates make it to the PLC for this amount of time, the PLC will null the outputs to stop machine motion ASAP.  Put this as large as you are comfortable, but the larger it is, the longer the delay will be before the outputs are cut on the PLC side, so keep it just larger than the ADS rate, but hopefully rather small still.

**ADS WD Dead Duration**: [2000ms] - In the event of a watchdog timeout due to network or controller loss while in operation, this timer is how long the outputs will be held at 0.  This is important so the user has time to realize they lost control and let off the inputs to avoid unexpected machine motion.  Additionally, the PLC code ensures the system won't allow new inputs on the PLC side until the user releases all buttons on the controller.

**FB_TcJoy Instance**: [Global_Variables.TcJoy] - This is the instance path of the user-created instance of FB_TcJoy on the PLC Runtime.  I usually put it in global so multiple parts of the code can read the values they want to use for remote control.  Note on TC3, the default is GVL instead of Global_Variables.  If you have this wrong, when you connect up to the PLC you'll get an error.

**Auto Connect On Start**: [unchecked by default] - Hopefully obvious... once you have your settings happy, you can check this box, then start TcJoy on windows startup, or even better from a PLC function block so you don't have to launch it manually.  A service mode and auto-reconnect would be nice but is not in the current plan.  Pull requests welcome!

**Game Controller Analog Stick Dead Zone**: [8000] - This number is the amount of counts (of 32767 total for full travel) that will be ignored and not sent to the PLC.  In order to keep the deadzone logic out of the PLC code, the VB side of TcJoy will send 0 if the actual value is less than this number (absolute).  The reason for this setting is that the joystick springs don't center them perfectly, and as controllers wear out, the "no input" count value on the analog sticks can be as much as 7000 counts from my experience.  You can view the values on the Live Status screen and set your own values for your controller if you need tighter control.

**Game Controller Analog Shoulder Dead Zone**: [0] - This setting is essentially the same as the one above, but for the analog shoulder buttons.  They are 8 bit only, so you get 0-255.  From my experience, a released trigger will always read 0, but setting this to a higher number than 0 will block those low values from being sent to the PLC.


## Status

When connected, you'll see a logical status of the controller inputs:

![settings](https://github.com/evanmj/TcJoy/blob/master/Screenshots/LiveStatusTab.png)

Note that when the controller is worn out a bit, you won't have exact 0 for the analog sticks, so you'll want to set an appropriate deadzone in the settings.

Test the controller here and make sure things work as expected, and compare with the TwinCAT side once that is up and running.



## Usage

On the PLC, you'll end up with these output variables from the Function Block once everything is connected up and working:

(In the screenshot, I'm holding the A button and the right shoulder trigger)

![image](https://github.com/evanmj/TcJoy/blob/master/Screenshots/PLC_Fb.png)

Most are pretty obvious, buttons are booleans, analogs are DINTs.  

The two "non buttons" worth noting are:

**bIsActive** - Set true when TcJoy is communicating properly, goes false if anything goes wrong or a watchdog timeout occurs.
**bControllerConnected** - Set true if TcJoy is communicating with the PLC runtime, and there is a controller connected.

A typical "end user" structured text program will look something like this:

```
// Allow xbox controller control of the machine.
IF TcJoy.bControllerConnected AND TcJoy.bIsActive THEN
    // Make right trigger act as dead-man switch
    IF TcJoy.iRightTrigger_Axis > 230 THEN
        IF TcJoy.X_Button THEN
           ;// Your code here, jog axes, etc.
        END_IF
    END_IF
END_IF
```

## See in action

Here is a video from my Motion Control with PLCs video series where I use the previously closed source version of TcJoy:

[![Video](https://img.youtube.com/vi/mXQ1IAxP74w/0.jpg)](https://youtu.be/mXQ1IAxP74w?t=394)



## Support / Troubleshooting

Please open a ticket if you have issues... this has not been tested that much as of yet, especially on TC2 and WinCE.

As usual, the best bug reports are in the form of a pull request!

If you find this program useful, please consider a small donation to my children's college fund:

[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=8YQRPBYQVAJ36&source=url)




