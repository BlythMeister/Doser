---
title: Home
description: "A command line application which enables calling/posting on mass in order to test applications under load."
---

A command line application which enables calling/posting on mass in order to test applications under load.

The application can also be used to ensure API up-time during a deployment due to the near constant volume being sent.

## Usage

```cmd
Usage: Doser [options]

Options:
  -u|--url <URL>                  Required. URL to include in calls (can be provided multiple times)
  -m|--method <HTTP_METHOD>       Required. The HTTP method to use (supports: GET, POST)
  -g|--gap <DURATION>             Required. Gap between requests (in milliseconds)
  -d|--duration <DURATION>        Required. Duration to run the app for (in seconds)
  -p|--parallel <NUMBER>          Required. The number of parallel instances
  -am|--accept-mime <MIME_TYPE>   The MimeType for accept
  -pf|--payload-file <FILE_PATH>  A file to use of post as payload content (can be provided multiple times)
  -pm|--payload-mime <MIME_TYPE>  The MimeType for the payload
  -od|--output-dir                A directory to output all HTTP request/responses
  -wm|--watch-mode                Only log HTTP status changes
  -lfo|--log-failures-only        Only log non-success HTTP responses
  -v|--verbose                    Verbose logging
  -np|--no-prompt                 Never prompt user input
  -?|-h|--help                    Show help information
```
