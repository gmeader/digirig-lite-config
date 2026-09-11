# DigiRig Control Center

Diagnostic build for DigiRig Lite CM108B GPIO3 PTT.

## GPIO3 diagnostic

Use **GPIO3 ON (HOLD)**. The application sends one native Windows HID GPIO3 ON report and then performs no further GPIO writes until **GPIO3 OFF** is pressed.

The diagnostic text displays the HID path, report bytes, report lengths, CreateFile result, and WriteFile result.

Expected report for GPIO3 ON:
`00-00-04-04-00`

Expected report for GPIO3 OFF:
`00-00-00-04-00`

This application does not use COM ports.
