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

#### Block Number (BN) - Configuration property `blockName`

Refers to the end point block corresponding to some functionality of the endpoint, for level control blocks the most commonly used value is:

- `"blockName": "LEVEL"`

#### Paraemeter Name (PN) - Configuration properties `levelParameter` & `muteParameter`

Parameter names will vary based on the Block Numbers (BN) used to configure the `blockName` property.  The most commonly used values are:

- `"levelParameter": "GAIN"`
- `"muteParameter": "MUTE"`



```json
{
	"properties": {
		"levelControlBlocks": {
			"fader-main": {
				"label": "Main",
				"channelName": "SPKR_ZN_1",
				"blockName": "LEVEL",
				"levelParameter": "GAIN",
				"muteParameter": "MUTE",
				"disabled": false,
				"hasLevel": true,
				"hasMute": true,
				"isMic": false,
				"useAbsoluteValue": false,
				"unmuteOnVolChange": true
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
				"label": "Dialer", 
				"channelName": "VOIP1",
				"isVoip": true, 
				"clearOnHangup": true
			}			
		}
	}
}
```

`isVoip` will change the command sent to include the `endpointType` `UA`.

i.e. `EP UA {channelName} KEY KEY_CALL {dialString}`

### Bridge Configuration

#### Appliance

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
			"ipid": "a2"
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

#### Server (VC-4)

```json
{
	"key": "dsp1-bridge",
	"name": "DSP Bridge",
	"group": "api",
	"type": "vcEiscApiAdvanced",
	"properties": {
		"control": {
			"ipid": "d1",
			"method": "ipid",
			"roomId": "ROOM01SIMPL"
		},
		"devices": [ 
			{ 
				"deviceKey": "dsp-dialer1", 
				"joinStart": 1 
			} 
		]
	}
}
```

## SiMPL EISC Bridge Map

The selection below documents the digital, analog, and serial joins used by the SiMPL EISC. Update the bridge join maps as needed for the plugin being developed.

### ConvergePro2DspJoinMap

#### Digitals

| Join Number | Join Span | Description                        | Type    | Capabilities |
| ----------- | --------- | ---------------------------------- | ------- | ------------ |
| 1           | 1         | Is Online                          | Digital | FromSIMPL    |
| 101         | 100       | Preset Recall                      | Digital | FromSIMPL    |
| 201         | 200       | ControlTag Visible                 | Digital | FromSIMPL    |
| 401         | 200       | ControlTag Mute Toggle             | Digital | ToFromSIMPL  |
| 601         | 200       | ControlTag Mute On                 | Digital | ToSIMPL      |
| 801         | 200       | ControlTag Mute Off                | Digital | ToSIMPL      |
| 1001        | 200       | ControlTag Volume Up               | Digital | FromSIMPL    |
| 1201        | 200       | ControlTag Volume Down             | Digital | FromSIMPL    |
| 3100        | 1         | Call Incoming                      | Digital | ToSIMPL      |
| 3106        | 1         | Answer Incoming Call               | Digital | FromSIMPL    |
| 3107        | 1         | End Call                           | Digital | FromSIMPL    |
| 3110        | 10        | Keypad Digits 0-9                  | Digital | FromSIMPL    |
| 3120        | 1         | Keypad *                           | Digital | FromSIMPL    |
| 3121        | 1         | Keypad #                           | Digital | FromSIMPL    |
| 3122        | 1         | Keypad Clear                       | Digital | FromSIMPL    |
| 3123        | 1         | Keypad Backspace                   | Digital | FromSIMPL    |
| 3124        | 1         | Keypad Dial and Feedback           | Digital | ToFromSIMPL  |
| 3125        | 1         | Auto Answer On and Feedback        | Digital | ToFromSIMPL  |
| 3126        | 1         | Auto Answer Off and Feedback       | Digital | ToFromSIMPL  |
| 3127        | 1         | Auto Answer Toggle and On Feedback | Digital | ToFromSIMPL  |
| 3129        | 1         | On Hook Set and Feedback           | Digital | ToFromSIMPL  |
| 3130        | 1         | Off Hook Set and Feedback          | Digital | ToFromSIMPL  |
| 3132        | 1         | Do Not Disturb Toggle and Feedback | Digital | ToFromSIMPL  |
| 3133        | 1         | Do Not Disturb On Set and Feedback | Digital | ToFromSIMPL  |
| 3134        | 1         | Do Not Disturb Of Set and Feedback | Digital | ToFromSIMPL  |

#### Analogs

| Join Number | Join Span | Description                                  | Type   | Capabilities |
| ----------- | --------- | -------------------------------------------- | ------ | ------------ |
| 1           | 1         | Device communication monitor status feedback | Analog | ToSIMPL      |
| 2           | 1         | Device socket status feedback                | Analog | ToSIMPL      |
| 201         | 200       | ControlTag Volume                            | Analog | ToFromSIMPL  |
| 401         | 200       | ControlTag Type                              | Analog | ToSIMPL      |
| 3100        | 1         | Call State Feedback                          | Analog | ToSIMPL      |

#### Serials

| Join Number | Join Span | Description                   | Type   | Capabilities |
| ----------- | --------- | ----------------------------- | ------ | ------------ |
| 1           | 1         | Device Name                   | Serial | ToSIMPL      |
| 101         | 100       | Preset Name                   | Serial | ToSIMPL      |
| 201         | 200       | Channel Name                  | Serial | ToSIMPL      |
| 3100        | 1         | Dial String Send and Feedback | Serial | ToFromSIMPL  |
| 3101        | 1         | Dialer Label                  | Serial | ToSIMPL      |
| 3102        | 1         | Last Number Dialed Feedback   | Serial | ToSIMPL      |
| 3104        | 1         | Caller ID Number              | Serial | ToSIMPL      |
| 3105        | 1         | Caller ID Name                | Serial | ToSIMPL      |
| 3106        | 1         | This Line's Number            | Serial | ToSIMPL      |


## DEVJSON Commands
```json
devjson:1 {"deviceKey":"dsp1", "methodName":"SetDebugLevels", "params":[2]}
devjson:1 {"deviceKey":"dsp1", "methodName":"ResetDebugLevels", "params":[]}
```