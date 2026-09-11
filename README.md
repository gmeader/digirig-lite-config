# DigiRig Lite Control Center

**DigiRig Lite Control Center** is a Windows desktop application for monitoring and controlling a **DigiRig Lite** USB audio interface connected to a radio.

It provides a simple graphical interface for monitoring receive audio, controlling transmit audio levels, viewing live audio meters, testing the radio's PTT connection, and generating a test tone.

The application communicates with the DigiRig Lite through its **USB audio interfaces and CM108B HID GPIO3 PTT interface**. It does **not** use a serial or COM port.

## Features

* **Receive audio monitoring**

  * Live RX/input level meter
  * DigiRig input level control
  * Route received radio audio to the Windows speakers
  * Start/stop input audio monitoring independently

* **Transmit audio monitoring**

  * Live stereo TX/output meters
  * Separate Left and Right channel monitoring
  * Left channel for radio transmit audio
  * Right channel available for DigiRig VOX/PTT triggering

* **Transmit audio control**

  * Control the DigiRig output level from the application
  * Monitor output activity in real time

* **Test Tone**

  * Built-in 1 kHz audio test tone
  * Stereo 48 kHz / 16-bit audio
  * Tone can be transmitted through the DigiRig to verify the complete audio path

* **PTT testing**

  * Dedicated `PTT TEST` control
  * Directly controls the DigiRig Lite's **GPIO3 / C-Media HID PTT**
  * Configurable PTT duration
  * Does not depend on the right-channel VOX mechanism
  * Provides precise software-controlled PTT timing

## Hardware

The application is designed around the **DigiRig Lite**, which uses a **C-Media CM108B USB audio codec**.

The DigiRig appears to Windows as USB audio devices rather than as a serial device. The application's PTT implementation communicates with the CM108B HID interface to control GPIO3.

Typical setup:

```text
Radio
  │
  │ Audio / PTT
  │
DigiRig Lite
  │
  │ USB
  │
Windows PC
  │
DigiRig Lite Control Center
```

The application has been tested with a **Baofeng BF-F8HP PRO**.

## PTT Implementation

DigiRig Control Center uses the CM108B HID GPIO3 interface for PTT control.

The CM108B device is identified by:

```text
VID: 0x0D8C
PID: 0x0012
```

GPIO3 corresponds to bit `0x04`.

The HID output reports used by the application are:

```text
PTT ON:
00 00 04 04 00

PTT OFF:
00 00 00 04 00
```

The application uses the Windows HID interface to send these reports directly to the CM108B device.

This provides a much more deterministic PTT mechanism than relying on audio VOX activation.

## Audio Architecture

The application uses Windows audio endpoints exposed by the DigiRig Lite.

The receive path is:

```text
Radio
  ↓
DigiRig Lite RX
  ↓
DigiRig In
  ↓
Audio capture
  ↓
RX level meter
  ↓
Windows speaker output
```

The transmit path is:

```text
Windows audio
  ↓
DigiRig Out
  ↓
TX level meters
  ↓
DigiRig Lite
  ↓
Radio
```

The TX meters use the DigiRig output's stereo audio stream, allowing the Left and Right channels to be monitored independently.

## Requirements

* Windows 10 or Windows 11
* DigiRig Lite
* Compatible radio
* USB connection to the DigiRig Lite

The published application is built as a **self-contained Windows x64 application**, so the .NET runtime does not need to be separately installed on the target computer.

## Installation

For a self-contained release, extract the contents of the zip file found in Releases to a folder and run:

```text
DigiRigControlCenter.exe
```

The entire published directory should be kept together; do not copy only the executable.

## Building from Source

The project uses **.NET 8 / WPF**.

To build:

```powershell
dotnet build -c Release
```

To run the application:

```powershell
dotnet run -c Release
```

To create a self-contained Windows x64 deployment:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

The resulting deployment files are placed in:

```text
bin\Release\net8.0-windows\win-x64\publish\
```

## Design Goals

DigiRig Control Center was developed with a few specific goals:

* Keep the interface simple and focused on radio operation.
* Provide immediate visual feedback for RX and TX audio.
* Make DigiRig audio levels easy to adjust.
* Provide a reliable way to test PTT independently of VOX.
* Avoid unnecessary serial-port/COM-port dependencies.
* Make it possible to diagnose the complete audio and PTT path using only the DigiRig and radio.

## Project Status

This project is currently under active development and testing.

The core RX monitoring, TX monitoring, test-tone transmission, and CM108B GPIO3 PTT functionality have been tested with the DigiRig Lite.

## License

See the repository license for details.

