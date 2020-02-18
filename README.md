# Doser

[![AppVeyor branch](https://img.shields.io/appveyor/ci/blythmeister/doser)](https://ci.appveyor.com/project/BlythMeister/Doser)
[![Nuget](https://img.shields.io/nuget/v/doser)](https://www.nuget.org/packages/Doser/)
[![GitHub release (latest by date)](https://img.shields.io/github/v/release/BlythMeister/Doser)](https://github.com/BlythMeister/Doser/releases/latest)
[![GitHub issues](https://img.shields.io/github/issues-raw/blythmeister/doser)](https://github.com/BlythMeister/Doser/issues)

A command line application which enables calling/posting on mass in order to test applications under load.

The application can also be used to ensure API up-time during a deployment due to the near constant volume being sent.

# Installation

Run `dotnet tool install --global Doser` to install.

To update, run `dotnet tool update --global Doser`

# Usage

```
Usage: Doser [options]

Options:
  -u|--url <URL>             Required. URL to include in calls (can be provided multiple times)
  -m|--method <HTTP_METHOD>  Required. The HTTP method to use (supports: GET, POST)
  -g|--gap                   Required. Gap between requests (in milliseconds)
  -d|--duration              Required. Duration to run the app for (in seconds)
  -p|--parallel              Required. The number of parallel instances
  -am|--accept-mime          The MimeType for accept
  -pf|--payload-file         A file to use of post as payload content
  -pm|--payload-mime         The MimeType for the payload
  -np|--no-prompt            Never prompt user input
  -v|--verbose               Verbose logging
  -?|-h|--help               Show help information
```

# 3rd Party Libraries

* [CommandLineUtils](https://github.com/natemcmaster/CommandLineUtils)
