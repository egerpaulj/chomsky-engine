# Microservice.Exchange.Endpoints.Command

.NET library provides an implementation of Consumer/Publish Endpoint for Messaging Exchange library.

The library provides the following:
- **DataIn:** Executes a command and provides the output (stdout and stderr) to the Exchange as a message
- **DataOut:** Takes a message from the exchange and provides that as an argument to the configured command

## Usage

- **Command:** The command or process to be executed
- **Arguments**: The arguments that should be passed on to the process

### DataIn

```
"DataIn": {
          "CommandConsumer": {
            "Command": "/bin/bash",
            "Arguments": "-c \"ls -m |grep -i appsettings.Development.json | tail -5\""
          }
        },
"TypeMappings" : {
        "CommandConsumer": "Microservice.Exchange.Endpoints.Command.CommandPublisher, Microservice.Exchange.Endpoints.Command"
      }
```

### DataOut

Two output options are possible:
- **Arguments** in the configuration. Then the text is formatted. See example: **{0}**
- No **Arguments**: then the Input message will be provided as an argument

**E.g. **

```
"DataOut": {        
          "CommandPublisher": {
            "Command": "/bin/bash",
            "Arguments": "-c \"cat > testData/command/out/testoutput <<EOF\n {0} \nEOF\""
          }
      },
"TypeMappings" : {
        "CommandPublisher": "Microservice.Exchange.Endpoints.Command.CommandPublisher, Microservice.Exchange.Endpoints.Command"
      }
```


## License

Copyright (C) 2022  Paul Eger

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.