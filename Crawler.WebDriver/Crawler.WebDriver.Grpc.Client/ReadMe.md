
.NET Web Driver Grpc Client


## Library

Sends a Web request to a Grpc Server and retrieves the response.

The interface supports the following:

- TryOptionAsync<string> **LoadPage**(Option<LoadPageRequest> request );

Returns the HTML as a string

- TryOptionAsync<FileData> **Download**(Option<DownloadRequest> Uri);

Returns the FileData


### LoadPageRequst

The request can be associated with UI-Actions. The actions can be provided as a list.

**Note:** first the UI Actions are completed, afterwards the HTML source is returned.

The following are supported:
- Click
- Input Text
- Checkbox select
- Dropdown select
- Radio select
- List select
- Wait for web element to load (or for a specified millisecond duration)


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