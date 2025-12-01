# RetroModemSim

Simulates an old-school, Hayes-compatible analog modem using TCP/IP, allowing retro computers to access BBSs and other machines via the Internet or any TCP/IP network.

Instead of dialing a phone number, you "dial" a host on the Internet, using either a domain name or an IP address, and optionally a port. Incoming connections are also supported, allowing your retro PC to receive incoming "calls" from the Internet or your network.

Not only can you use RetroModemSim to dial up BBSs, but you can also use it as a wireless or Internet null-modem cable. For example, you can connect your C64 to your Tandy 1000 wirelessly using two instances of RetroModemSim -- one calling the other -- to establish the virtual null-modem connection over your Wi-Fi or even the Internet.

RetroModemSim is written using .NET 6, so it can easily be run on Windows, Linux, or MacOS. My favorite is to set it up on a Raspberry Pi, configured to run RetroModemSim on startup, so the Pi acts as a modem for any of my retro machines with an RS-232 port.

# Features

 - Hayes command set, plus extended and proprietary commands
 - PETSCII support, for use with your favorite Commodore machines
 - Full online/offline data mode support, with +++ escape sequence
 - Built-in phonebook, configurable via AT commands
 - Support for incoming connections, including auto-answer
 - Query and change the baud rate via AT command
 - Support for compound commands (for example, `ATQ0V1E1S0=0`)
 - Software (XOn/XOff) support, configurable via AT command
 - Configurable RING, DCD, and DSR support via RTS/DTR
 - Accurate RING simulation, including auto-answer
 - Accurate DCD simulation
 - Accurate support for a variety of S-registers
 - Can be used with the console instead of a COM port
 
# PETSCII Support
AT commands can be issued either in ASCII or PETSCII, which is useful (and sometimes required) on Commodore computers.

ASCII vs. PETSCII is auto-detected at the beginning of each AT command, so for example, one AT command can be issued in ASCII, and the next can be issued in PETSCII.
 
 The one exception is the backspace character. The backspace character is specified in S-register 5 (defaults to 0x08), but a PETSCII backspace character (0x14) will always be interpreted as a backspace, even if the AT command was started in ASCII mode.

This is because when some C64/C128 terminal applications are in graphics mode, they send a PETSCII backspace character, even if they send the AT command in ASCII mode.
 
# Usage

    RetroModemSim comport=<COM_port> baud=<baud_rate> incomingport=<TCP_port>
    
    Baud rate defaults to 1200 if unspecified.
    
    Incomingport defaults to 60000 if unspecified. If incomingport is 0, incoming calls are disabled.
    
    If comport is unspecified, then the console will be used to interact with the modem.

## Examples:
Start on a Linux system using the first USB RS-232 adapter at 19200 baud, accepting incoming connections on port `60000`:
> RetroModemSim comport=/dev/ttyUSB0 baud=19200

Start on a Windows system using `COM4` at the default baud rate of `1200`, disabling incoming connections:
> RetroModemSim comport=COM4 incomingport=0

# Dialing and Call Management

## Dialing
Dial the Particles BBS on port `6400`: 

> ATD@PARTICLESBBS.DYNDNS.ORG:6400

Dial `192.168.0.100` on port `1234`:

> ATD@192.168.0.100:1234

When dialing a destination, the `@` is optional if the destination does not begin with a `T` or `P`, but required if it does (otherwise the `T` or `P` is interpreted as touch-tone or pulse dialing.

## Dialing from Phone Book
If the dialing destination matches a phone book entry name, then the phone book entry is dialed.

Dial phone book entry named `#1`:

> ATD@#1
 
or

> ATD#1

## Dialing Touch-Tone or Pulse
You can specify tone or pulse dialing with `T` or `P`, although these have no effect.

Dial the Particles BBS using touch-tone dialing:

    ATDT@PARTICLESBBS.DYNDNS.ORG:6400
 
## Dial, But Remain in Command Mode
If you end the dial command with `;` the modem will dial, but you will remain in command mode:

> ATD@PARTICLESBBS.DYNDNS.ORG:6400;

If the connection is established, you will see `CONNECT`. You can then enter online mode with `ATO`.

## Hang Up (Terminate Remote Connection)

You can terminate a remote connection while in command mode:
> ATH

Note that if you are in online data mode, you must first return to command mode by issuing the escape sequence `+++`.

# Command Mode vs. Online Data Mode
When RetroModemSim starts, the modem is in command mode, where you can enter AT commands.

When a connection is established, the modem typically enters online data mode, where every character you type is sent to the remote destination.

You can switch between command mode and online data mode via the escape sequence and the `ATO` command.

## Exiting Online Data Mode and Returning to Command Mode
To return to command mode from online data mode, you can issue the escape sequence:

 - Wait one second
 - Quickly type `+++`, with very little time between each `+`
 - Wait one second

You will see `OK`, and you will be back in command mode, ready to issue AT commands.

Note that proper timing is critical when issuing the escape sequence. Also note that the escape sequence timing and character value depend on the value of several S registers, which can be changed.

## Returning on Online Data Mode
If you are in command mode, and you are connected to a remote host, you can return to online data mode:

> ATO

# Incoming Calls
If RetroModemSim is not started with the `incomingport=0` option, then it listens for incoming calls on the specified TCP/IP port (default is 60000).

When a call is incoming, and there is not already a connection established, `RING` will be displayed each time the virtual phone rings, and the `RI` pin will be asserted.

You can manually answer an incoming call:

> ATA

S-register 0 `S0` specifies the number of rings before an incoming call is auto-answered. It defaults to `2`, so by default, an incoming call will be auto-answered after about two seconds.

You can disable automatic answering by setting `S0` to `0`:
> ATS0=0

# Baud Rate
The default baud rate is set according to the command-line parameter `baud`. If not specified, it defaults to `1200`.

You can query the current baud rate:
> AT+IPR?

You can change the current baud rate, for example to `9600`:
> AT+IPR=9600

When changing the baud rate, the command response is sent using the *original* baud rate. After the response is sent, the new baud rate will become active.

# Phone Book
Phone book entries can be created as a shortcut to dialing. Each phone book entry consists of a name and a value.

When dialing, if the destination specified in the dial command matches a phone book name, then the phone book value is dialed.

Query all phonebook entries:
> AT$PB?

Add a new phone book entry named `#1`, which dials `LOCALHOST:50000`:
> AT$PB=#1,LOCALHOST:50000

Delete the phone book entry named `#1`:
> AT$PB=#1

# DSR/DCD/RING Configuration
By default, RetroModemSim will not output any physical signal in response to DSR/DCD/RING state changes, but you can configure RetroModemSim to use the DTR/RTS outputs to simulate the modem's DSR/DCD/RING signals. This is useful for application software which monitors these signals, for example, to determine when a call is connected (DCD), or when the line is ringing (RING).

## Examples
Suppose the cable you are using to connect the machine running RetroModemSim to your retro machine has DTR connected to DCD, and RTS connected to RING.

In this case, you can configure the retro machine's DCD line to be controlled by the PC's DTR line:
> AT$DCD=DTR

And you can configure the retro machine's RING line to be controlled by the PC's RTS line:
> AT$RING=RTS

Now suppose your retro application requires the DSR signal, and your cable wires DTR to DSR. In that case, you can configure the retro machine's DSR line to be controlled by the PC's DTR line:
> AT$DSR=DTR

## Persistence
DSR/DCD/RING configuration is persistent across power cycles, and are not affected by the zap (`ATZ`) command.

You can un-assign DCD/DSR/RING output configuration by assigning it to `NONE`:

> AT$DCD=NONE
>
> AT$DSR=NONE
>
> AT$RING=NONE 

## Querying Configuration
You can query the current DSR/DCD/RING configuration:

> AT$DCD?
> 
> AT$DSR?
> 
> AT$RING?

## Inverted Outputs

In some cases, your application may use/require inverted logic for the DSR/DCD/RING signals. In that case, use `!` when configuring the signal.

For example, to configure the retro machine's DCD line to use inverted logic, and be controlled by the PC's DTR line:

>AT$DCD=!DTR

# Misc.

## Online Data Mode Buffering

Buffer data received when connected while in command mode (buffered data will be received when returning to online data mode):
> AT$B1

Do not buffer data when connected while in command mode. Any data received when connected while in command mode is discarded:
> AT$B0

## Software Flow Control (XOn/XOff)
Software flow control can be enabled between RetroModemSim and the retro computer. This can be useful in cases where the remote destination tries to send data to the retro PC faster than the retro PC can accept it. This is especially the case when using fast baud rates, especially on slow retro PCs.

Enable software flow control:
> AT$SWFC=1

Disable software flow control:
> AT$SWFC
> 
> AT$SWCF=0

Query the current status of software flow control:
> AT$SWFC?

## S-Registers
There are several S-registers which control various aspects of the modem simulation:

| S-Register  | Description                 
|-------------|--------------------------------------------------
| 0           | Number of rings before automatically answering
| 1           | Number of rings so far
| 2           | Escape character
| 3           | CR character
| 4           | LF character
| 5           | BS character
| 6           | Dial tone delay (unused)
| 7           | Carrier delay (unused)
| 8           | Dial pulse (unused)
| 9           | Unused
| 10          | Carrier loss delay (unused)
| 11          | Touch tone delay (unused)
| 12          | Escape sequence guard time

You can query the value of an S-register, for example, the number of rings so far:
> ATS1?

You can set the value of an S-register, for example, to set the number of rings to 10 before auto-answering:
> ATS1=10

## Tone/Pulse Dialing
These commands are recognized, but have no effect:
> ATT
> ATP

## Zap
This command resets most settings to the default. The following are unaffected:

 - Baud rate
 - Phone book contents
 - Software flow control
 - DSR, DCD, and RING configuration

>ATZ

## Carrier
This command is recognized, but has no effect:

Enable carrier:
> ATC
> ATC0

Disable carrier:
> ATC1

## Echo On/Off
Enable echo:
> ATE
> ATE1

Disable echo:
> ATE0

## Full/Half Duplex
Full-duplex mode:
> ATF
> ATF1

Half-duplex mode:
> ATF0

## Quiet Mode
Enable quiet mode (hides command responses):
> ATQ1

Disable quiet mode (shows command responses):
> ATQ
> ATQ0

## Verbal Mode
Enable verbal mode (use text responses):
> ATV
> ATV1

Disable verbal mode (use numerical responses):
> ATV0

## Monitor
This command is recognized, but has no effect:

Speaker on when in command mode:
> ATM
> ATM1

Speaker off:
> ATM0

Speaker on (always):
> ATM2

## Result Code Levels

| CMD  | Report Connection Speed | Detect Dial Tone | Detect Busy Signal
|------|-------------------------|------------------|-------------------
| ATX0 | No                      | No               | No
| ATX1 | Yes                     | No               | No
| ATX2 | Yes                     | Yes              | No
| ATX3 | Yes                     | No               | Yes
| ATX  | Yes                     | Yes              | Yes
| ATX4 | Yes                     | Yes              | Yes

Note that dial tone and busy signal detection have no real effect on the modem simulation.

## Repeat the Last AT command:

> A/

Note that the command is executed as `A/` is entered, and no `CR` is required.

# Null-Modem Example
Suppose you want to create a virtual NULL-modem connection between two remote retro machines.

One retro machine is connected to RetroModemSim running on a Raspberry Pi with local IP address `192.168.0.100`. This machine will receive an incoming call from the other, so the default settings are ok, and port `60000` will be used.

The other retro machine is connected to another Raspberry Pi. This machine will dial the first machine at its IP address and port:
> ATD@192.168.0.100:60000

The first machine will indicate the incoming call, and with the default settings, will auto-answer after two virtual rings. Once the incoming call is answered, both RetroModemSims will go into online data mode, and the two retro machines will be connected via a virtual NULL-modem cable.

# PC Connection Example
Suppose you want to transfer a file using X-modem from your modern PC to your retro machine. In this case, you can use a terminal emulator that supports file transfers, and supports using TCP/IP connections.

Let's use ClearTerminal as an example. In this case, we use ClearTerminal to create a TCP server connection on some port, let's say `55000`. And suppose our PC's IP address is `192.168.0.123`.

Then on the retro machine, we can simply connect to the PC:
> ATD@192.168.0.123:55000

Once connected, we can use X-Modem to transfer files to/from the retro machine.

Or, if your PC terminal program only supports creating TCP client connections, then you can initiate an incoming call to the RetroModemSim machine.

# Wiring
To use RetroModemSim, you will need a cable to connect the serial port on the machine that is running RetroModemSim (PC) to the serial port on the retro machine. At a minimum, you need the following connections:

| PC Pin (DE-9) | PC Signal | Direction | Retro Machine Signal
|---------------|-----------|-----------|---------------------
| 2             | RXD       | <-        | TXD
| 3             | TXD       | ->        | RXD
| 5             | GND       | <->       | GND

Now suppose you want to control your retro machine's RING signal with RTS, and its DCD signal with DTR. In that case, make the following connections (and set the appropriate configuration via AT commands):

| PC Pin (DE-9) | PC Signal | Direction | Retro Machine Signal
|---------------|-----------|-----------|---------------------
| 4             | DTR       | ->        | DCD
| 7             | RTS       | ->        | RING

If necessary, you can connect lines that are driven by the retro machine into the PC's inputs:

| PC Pin (DE-9) | PC Signal | Direction | Retro Machine Signal
|---------------|-----------|-----------|---------------------
| 1             | DCD       | <-        | DTR
| 6             | DSR       | <-        | DTR
| 8             | CTS       | <-        | RTS

