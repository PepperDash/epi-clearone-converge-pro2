![PepperDash Essentials Plugin](./images/essentials-plugin-blue.png)

# ClearOne CONVERGE Pro 2 Plugin

## License

Provided under MIT license

## Device Specific Information

Supports ClearOne CONVERGE Pro 2 evices

### Communication Settings

| Setting      | Value |
| ------------ | ----- |
| Delimiter    | "\n"  |
| Default Baud | 57600 |
| Data Bits    | 8     |
| Stop Bits    | 1     |
| Parity       | none  |
| Flow Control | none  |
| Telnet Port  | 23    |

### Valid communication methods

```c#
com
tcpIp
```

## Configuration Properties

The configuration properties are need to properly structure the commands used.

Command Structure: 

EP Commands: 

- `EP <EPT> <EPN> <BN> <PN> [VALUE]`
- `EP <CHANNEL_NAME> <PN> [VALUE]`

RAMP Commands: 

- `RAMP <EPT> <EPN> <TARGET_LEVEL> <STEP>`
- `RAMP <CHANNEL_NAME> <TARGET_LEVEL> <STEP>`

### Endpoint Types (EPT)

**Inputs**

| Device                           | `endpointType` |
| -------------------------------- | -------------- |
| Microphones                      | MIC            |
| ClearOne BeamForming Mic Array 2 | BFM            |
| USB In                           | USB_RX         |
| Telephone (analog) In            | TELCO_RX       |
| VoIP In                          | VOIP_RX        |

**Outputs**

| Device                                         | `endpointType` |
| ---------------------------------------------- | -------------- |
| Speakers                                       | SPEAKER        |
| Output (any device attached to an output port) | OUTPUT         |
| USB Out                                        | USB_TX         |
| Telephone (analog) Out                         | TELCO_TX       |
| VoIP Out                                       | VOIP_TX        |

**Other**

| Device                            | `endpointType` |
| --------------------------------- | -------------- |
| Fader                             | FADER          |
| GPIO                              | GPIO           |
| Processing Blocks                 | PROC           |
| User Agent (for controlling VoIP) | UA             |
| Signal Generator                  | SGEN           |

### Endpoint Number (EPN)

Endpoint number follows the format `BNN`.

`B` = box number within a stack
`NN` = channel number of the box

### Block Number (BN)

Refers to the end point block corresponding to some functionality of the endpoint

**Example Block Numbers (BN)**

| BN Name      |
| ------------ |
| LEVEL (1)    |
| SETTINGS (2) |
| SIG_GEN (12) |

### Paraemeter Name (PN)

Name of the parameter within a block, values depend on the BN value used.

**Example Parameter Names (PN) - LEVEL (1) BN**

| PN           | Description                                                                             | Value                                                                                                                                                                                                                                              |
| ------------ | --------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| GAIN (1)     | Fine gain.                                                                              | Default decibel range of -65 to 20 unless adjusted with MAX_GAIN or MIN_GAIN, adjust in increments of 0.5 Note: More information about gain or fine gain is available at the beginning of the EP section.<BR>Leave blank to retrieve current value |
| MUTE (2)     | Mute                                                                                    | 0 = Unmute<BR>1 = mute<BR>2 = Toggle Current State<BR>Leave blank to retrieve current value                                                                                                                                                        |
| MAX_GAIN (7) | Maximum gain. This controls how high gain can be set, and also how high ramping can go. | -65 to 20 in increments of 0.5<BR>Leave blank to retrieve current value                                                                                                                                                                            |
| MIN_GAIN (8) | Minimum gain. This controls how low gain can be set, and also how low ramping can go.   | -65 to 20 in increments of 0.5<BR>Leave blank to retrieve current value                                                                                                                                                                            |

## Configuration Objects

Type: `convergepro2dsp`

### Device Configuration

```json
{
	"key": "dsp1",
	"name": "ClearOne Converge Pro 2",
	"type": "convergePro2",
	"group": "plugin",
	"properties": {
		"control": {
			"method": "tcpip",
			"controlPortDevKey": "processor",
			"controlPortNumber": 1,
			"comParams": {
				"protocol": "RS232",
				"baudRate": 57600,
				"dataBits": 8,
				"stopBits": 1,
				"parity": "None",
				"hardwareHandshake": "None",
				"softwareHandshake": "None"
			},
			"tcpSshProperties": {
				"address": "10.0.0.194",
				"port": 23,
				"username": "admin",
				"password": "password",
				"autoReconnect": true,
				"autoReconnectIntervalMs": 10000
			}
		},
		"communicationMonitorProperties": {
			"pollInterval": 60000,
			"timeToWarning": 180000,
			"timeToError": 300000
		},
		"boxName": "DSP1"
	}
}
```

### Level Control Blocks Configuration

Configuration using EPT, EPN, BN

```json
{
	"properties": {
		"levelControlBlocks": {
			"fader1": {                            
				"label": "Room",
				"endpointType": "",				
				"endpointNumber": "",
				"blockNumber": "",
				"disabled": false,
				"hasLevel": true,
				"hasMute": true,
				"isMic": false
			}
		}
	}
}
```

Configuration using Channel Name

```json
{
	"properties": {
		"levelControlBlocks": {
			"fader1": {                            
				"label": "Room",
				"channelName": "ROOM_LEVEL",
				"disabled": false,
				"hasLevel": true,
				"hasMute": true,
				"isMic": false
			}
		}
	}
}
```

### Presets Configuration

```json
{
	"properties": {
		"presets": {
			"preset1": {
				"label": "System On",
				"preset": "1"
			},
			"preset2": {
				"label": "System Off",
				"preset": "2"
			},
			"preset3": {
				"label": "Default Levels",
				"preset": "3"
			}
		}
	}
}
```

`"preset"` is the name of the macro to run, the name is case sensitive.


### Dialer COntrol Blocks Configuration

```json
{
	"properties": {
		"dialers": {
			"dialer1": {
				"label": "Dialer 1",
				"channelName": "DIALER1",
				"endpointType": "TELCO_RX",
				"endpointNumber": "101",
				"blockNumber": "",
				"clearOnHangup": true
			}
		}
	}
}
```

### Bridge Configuration

```json
{
	"key": "dsp1-bridge",
	"name": "DSP Bridge",
	"group": "api",
	"type": "eiscApiAdvanced",
	"properties": {
		"control": {
			"method": "ipidTcp",
			"tcpSshProperties": {
				"address": "127.0.0.2",
				"port": 0
			},
			"ipid": "B1"
		},
		"devices": [
			{
				"deviceKey": "dsp1",
				"joinStart": 1
			}
		]
	}
}
```

## SiMPL EISC Bridge Map

The selection below documents the digital, analog, and serial joins used by the SiMPL EISC. Update the bridge join maps as needed for the plugin being developed.

# Converge Pro 2 DSP Join Map

## Digitals

| Join Number | Join Span | Description                        | Type                | Capabilities |
| ----------- | --------- | ---------------------------------- | ------------------- | ------------ |
| 1           | 1         | Is Online                          | Digital             | FromSIMPL    |
| 100         | 100       | Preset Recall                      | Digital             | FromSIMPL    |
| 200         | 200       | ControlTag Visible                 | Digital             | FromSIMPL    |
| 400         | 200       | ControlTag Mute Toggle             | Digital             | ToFromSIMPL  |
| 600         | 200       | ControlTag Mute On                 | Digital             | ToSIMPL      |
| 800         | 200       | ControlTag Mute Off                | Digital             | ToSIMPL      |
| 1000        | 200       | ControlTag Volume Up               | Digital             | FromSIMPL    |
| 1200        | 200       | ControlTag Volume Down             | Digital             | FromSIMPL    |
| 3100        | 1         | Call Incoming                      | Digital             | ToSIMPL      |
| 3106        | 1         | Answer Incoming Call               | Digital             | FromSIMPL    |
| 3107        | 1         | End Call                           | Digital             | FromSIMPL    |
| 3110        | 10        | Keypad Digits 0-9                  | Digital             | FromSIMPL    |
| 3120        | 1         | Keypad *                           | Digital             | FromSIMPL    |
| 3121        | 1         | Keypad #                           | Digital             | FromSIMPL    |
| 3122        | 1         | Keypad Clear                       | Digital             | FromSIMPL    |
| 3123        | 1         | Keypad Backspace                   | Digital             | FromSIMPL    |
| 3124        | 1         | Keypad Dial and Feedback           | Digital             | ToFromSIMPL  |
| 3125        | 1         | Auto Answer On and Feedback        | Digital             | ToFromSIMPL  |
| 3126        | 1         | Auto Answer Off and Feedback       | Digital             | ToFromSIMPL  |
| 3127        | 1         | Auto Answer Toggle and On Feedback | Digital             | ToFromSIMPL  |
| 3129        | 1         | On Hook Set and Feedback           | Digital             | ToFromSIMPL  |
| 3130        | 1         | Off Hook Set and Feedback          | Digital             | ToFromSIMPL  |
| 3132        | 1         | Do Not Disturb Toggle and Feedback | Digital             | ToFromSIMPL  |
| 3133        | 1         | Do Not Disturb On Set and Feedback | Digital             | ToFromSIMPL  |
| 3134        | 1         | Do Not Disturb Of Set and Feedback | Digital             | ToFromSIMPL  |

## Analogs

| Join Number | Join Span | Description         | Type                | Capabilities |
| ----------- | --------- | ------------------- | ------------------- | ------------ |
| 200         | 200       | ControlTag Volume   | Analog              | ToFromSIMPL  |
| 400         | 200       | ControlTag Type     | Analog              | ToSIMPL      |
| 3100        | 1         | Call State Feedback | Analog              | ToSIMPL      |

## Serials

| Join Number | Join Span | Description                   | Type                | Capabilities |
| ----------- | --------- | ----------------------------- | ------------------- | ------------ |
| 100         | 100       | Preset Name                   | Serial              | ToSIMPL      |
| 200         | 200       | ControlTag Name               | Serial              | ToSIMPL      |
| 3100        | 1         | Dial String Send and Feedback | Serial              | ToFromSIMPL  |
| 3101        | 1         | Dialer Label                  | Serial              | ToSIMPL      |
| 3102        | 1         | Last Number Dialed Feedback   | Serial              | ToSIMPL      |
| 3104        | 1         | Caller ID Number              | Serial              | ToSIMPL      |
| 3105        | 1         | Caller ID Name                | Serial              | ToSIMPL      |
| 3106        | 1         | This Line's Number            | Serial              | ToSIMPL      |

## DEVJSON Commands
```json
devjson:1 {"deviceKey":"dsp1", "methodName":"SetDebugLevels", "params":[2]}
devjson:1 {"deviceKey":"dsp1", "methodName":"ResetDebugLevels", "params":[]}
```