<!-- PROJECT SHIELDS -->
[![Build Status][build-shield]][build-url]
[![Contributors][contributors-shield]][contributors-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]
[![Discord][discord-shield]][discord-url]

<!-- PROJECT LOGO -->
<br />
<p align="center">
  <a href="https://github.com/MarkusKgit/MonkeyBot/">
    <img src="https://cdn.discordapp.com/avatars/333959570437177354/102d13432c8b3640262efc35baca0a4a.png?size=128" alt="Logo" width="128" height="128">
  </a>
  <h3 align="center">MonkeyBot</h3>
  <p align="center">
    A general purpose Discord Bot for the <a href="https://www.monkey-gamers.com"><strong>Monkey Gamers</strong></a> community written in C#
    <br />
    <a href="https://github.com/MarkusKgit/MonkeyBot/blob/master/Commands.md"><strong>Explore the available commands »</strong></a>
    <br />
    <br />
    <a href="https://discord.gg/u43XvME">Join Chat</a>
    ·
    <a href="https://github.com/MarkusKgit/MonkeyBot/issues">Report Bug</a>
    ·
    <a href="https://github.com/MarkusKgit/MonkeyBot/issues">Request Feature</a>
  </p>
</p>

<!-- TABLE OF CONTENTS -->
## Table of Contents

* [About MonkeyBot](#about-monkeybot)
  * [Built With](#built-with)
* [Getting Started](#getting-started)
  * [Prerequisites](#prerequisites)
  * [Installation](#installation)
* [Usage](#usage)
* [Troubleshooting](#troubleshooting)
* [Roadmap](#roadmap)
* [Contributing](#contributing)
* [License](#license)
* [Contact](#contact)
* [Acknowledgements](#acknowledgements)



<!-- ABOUT THE PROJECT -->
## About MonkeyBot

MonkeyBot is a general purpose Discord Bot that was created for the needs of the [Monkey Gamers](https://www.monkey-gamers.com) community. Its main functions are:
* Welcoming new users
* Get updates from various feeds (ATOM/RSS) including our own Forums and Website
* (Self) role assignments
* Scheduled announcements
* Game server tracking
* Silly stuff like Trivia, Chuck Norris jokes, Benzen Facts, xkcd...


### Built With

* [.NET Core](https://docs.microsoft.com/en-us/dotnet/core/)
* [CloudinaryDotNet](https://github.com/cloudinary/CloudinaryDotNet)
* [Discord.Net](https://github.com/RogueException/Discord.Net)
* [CodeHollow.FeedReader](https://github.com/codehollow/FeedReader/)
* [Fluent Command Line Parser](https://github.com/PingmanTools/fluent-command-line-parser/tree/netstandard)
* [FluentScheduler](https://github.com/fluentscheduler/FluentScheduler)
* [Html Agility Pack](https://github.com/zzzprojects/html-agility-pack)
* [Humanizer](https://github.com/Humanizr/Humanizer)
* [Microsoft.EntityFrameworkCore](https://github.com/aspnet/EntityFrameworkCore)
* [NCrontab](https://github.com/atifaziz/NCrontab)
* [NLog](https://github.com/NLog/NLog)
* [SQLite](https://www.sqlite.org/index.html)

<!-- GETTING STARTED -->
## Getting Started


### Prerequisites

* Latest .NET core SDK for your platform (3.0 or later) - you can download it [here](https://dotnet.microsoft.com/download)
* A registered Discord application with a bot access token. If you don't have one, you can create one with your existing Discord account [here](https://discordapp.com/developers/applications/). There you have to add a Bot and need to copy both **Bot Token** and **Client ID**. To then add the bot to your Discord server go to `https://discordapp.com/oauth2/authorize?scope=bot&permissions=0&client_id=[ID]`, replacing *[ID]* with the **Client ID** of your bot (not the token)
* Optional: A cloudinary API key (only required for the Minecraft server status image). You can register one [here](https://cloudinary.com/users/register/free)
* Optional: An IDE of your choice (Visual Studio 2019 recommended)

### Installation

1. Clone the repo

   ```sh
   git clone https://github.com/MarkusKgit/MonkeyBot.git
   ```
   or download it from GitHub and unzip it

2. Build

   ```sh
   cd MonkeyBot
   dotnet build
   ```
   or open the solution in Visual Studio and build

3. Run

   ```sh   
   dotnet run
   ```

   On the first run of the Bot the configuration file will automatically be created by prompts on the command line. For a first test you only need to provide the Bot access token. The configuration will be stored in `/config/configuration.json`. Look at [exampleconfig.json](exampleconfig.json) to see the structure of the config file if you wish to create it manually.

4. Publish 

   To permanently run the bot you should publish it first:
   ```sh
   dotnet publish -c Release --output published
   ```
   Then you can create a daemon/service that automatically runs `dotnet published/MonkeyBot.dll`
   Here is an example for a system.d config file (tested on Ubuntu server):
   ```sh
    # /etc/systemd/system/MonkeyBot.service
    # To enable: sudo systemctl enable MonkeyBot.service
    # To start: sudo systemctl start MonkeyBot.service
    
    [Unit]
    Description=MonkeyBot service
  
    [Service]
    WorkingDirectory=/home/markus/MonkeyBot/MonkeyBot/published
    ExecStart=/usr/bin/dotnet /home/markus/MonkeyBot/MonkeyBot/published/MonkeyBot.dll
    Restart=on-failure
    RestartSec=10
    SyslogIdentifier=monkeybot-service
  
    [Install]
    WantedBy=multi-user.target
   ```
   Updates to new versions are then as easy as:
   ```sh
   sudo systemctl stop MonkeyBot.service
   cd /home/markus/MonkeyBot
   git pull
   dotnet publish -c Release --output published
   sudo systemctl start MonkeyBot.service
   ```

## Usage

Once the bot is running and connected it will respond to [commands](Commands.md). 
Try !help to get you started.


## Troubleshooting

Logfiles are stored in `Logs` directory and fatal errors will also appear in the command line output. The SQLite database is located in the `Data` directory.
If you have any questions or found a bug you can open an [issue](https://github.com/MarkusKgit/MonkeyBot/issues) or get in touch on [Discord][discord-url]

<!-- ROADMAP -->
## Roadmap

See the [open issues](https://github.com/MarkusKgit/MonkeyBot/issues) for a list of proposed features (and known issues).



<!-- CONTRIBUTING -->
## Contributing

Contributions are what make the open source community such an amazing place to be learn, inspire, and create. Any contributions you make are **greatly appreciated**. Have a look at our [Contributing guidelines](CONTRIBUTING.md) for more info.

tl;dr:

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request



<!-- LICENSE -->
## License

Distributed under the MIT License. See [LICENSE](LICENSE) for more information.



<!-- CONTACT -->
## Contact

Project Link: [https://github.com/MarkusKgit/MonkeyBot](https://github.com/MarkusKgit/MonkeyBot)

Discord: [https://discord.gg/u43XvME](https://discord.gg/u43XvME)



<!-- ACKNOWLEDGEMENTS -->
## Acknowledgements
* [Img Shields](https://shields.io)
* [Choose an Open Source License](https://choosealicense.com)
* [README template](https://github.com/othneildrew/Best-README-Template)


## Features:
For a full list of commands see [Commands](Commands.md)

<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[build-shield]: https://github.com/markuskgit/MonkeyBot/workflows/Build/badge.svg
[build-url]: https://github.com/MarkusKgit/MonkeyBot
[contributors-shield]: https://img.shields.io/github/contributors/MarkusKgit/MonkeyBot.svg?style=flat-square
[contributors-url]: https://github.com/MarkusKgit/MonkeyBot/graphs/contributors
[issues-shield]: https://img.shields.io/github/issues/Markuskgit/MonkeyBot.svg?style=flat-square
[issues-url]: https://github.com/MarkusKgit/MonkeyBot/issues
[license-shield]: https://img.shields.io/badge/license-MIT-green.svg?style=flat-square
[license-url]: LICENSE
[discord-shield]: https://img.shields.io/discord/333960047761817601.svg?style=flat-square
[discord-url]: https://discord.gg/u43XvME
