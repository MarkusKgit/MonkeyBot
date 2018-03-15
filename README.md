[![Build status](https://ci.appveyor.com/api/projects/status/n8jxa05v2o17hyww/branch/master?svg=true)](https://ci.appveyor.com/project/MarkusKgit/monkeybot/branch/master)

A general purpose Discord Bot for the Monkey-Gamers community written in C#

Visit www.monkey-gamers.com

Currently it can only be self-hosted - no invite available. Feel free to contribute by opening a new Issue.

<details>
<summary>Click here for a full list of commands</summary>

## Admin Commands
**Preconditions:** Minimum permission: *ServerAdmin* 

`!addowner username`  
*Preconditions:* Minimum permission: *BotOwner*  
*Remarks:* Adds the specified user to the list of bot owners  
</br>
`!removeowner username`  
*Preconditions:* Minimum permission: *BotOwner*  
*Remarks:* Removes the specified user from the list of bot owners  

## Announcements
**Preconditions:** Minimum permission: *ServerAdmin*, Can only be used in a *channel*  
  
`!announcements addrecurring announcementId cronExpression announcement`  
*Example:* `!announcements addrecurring "weeklyMsg1" "0 19 * * 5" "It is Friday 19:00"`  
*Remarks:* Adds the specified recurring announcement to the current channel  
</br>
`!announcements addrecurring announcementId cronExpression channelName announcement`  
*Example:* `!announcements addrecurring "weeklyMsg1" "0 19 * * 5" "general" "It is Friday 19:00"`  
*Remarks:* Adds the specified recurring announcement to the specified channel  
</br>
`!announcements addsingle announcementId time announcement`  
*Example:* `!announcements addsingle "reminder1" "19:00" "It is 19:00"`  
*Remarks:* Adds the specified single announcement at the given time to the current channel  
</br>
`!announcements addsingle announcementId time channelName announcement`  
*Example:* `!announcements addsingle "reminder1" "19:00" "general" "It is 19:00"`  
*Remarks:* Adds the specified single announcement at the given time to the specified channel  
</br>
`!announcements list `  
*Remarks:* Lists all upcoming announcements  
</br>
`!announcements remove id`  
*Example:* `!announcements remove announcement1`  
*Remarks:* Removes the announcement with the specified ID  
</br>
`!announcements nextrun id`  
*Example:* `!announcements nextrun announcement1`  
*Remarks:* Gets the next execution time of the announcement with the specified ID.  

## Benzen Facts
`!benzen `  
*Remarks:* Returns a random fact about Benzen  
</br>
`!addbenzenfact fact`  
*Remarks:* Add a fact about Benzen  

## Chuck Norris jokes
**Preconditions:** Minimum permission: *User*, Can only be used in a *channel*  
  
`!chuck `  
*Remarks:* Gets a random Chuck Norris fact.  
</br>
`!chuck name`  
*Remarks:* Gets a random Chuck Norris fact and replaces Chuck Norris with the given name.  

## GameServer
**Preconditions:** Minimum permission: *ServerAdmin*, Can only be used in a *channel*  
  
`!gameserver add ip`  
*Example:* `!gameserver add 127.0.0.1:1234`  
*Remarks:* Adds the specified game server and posts it's info info in the current channel  
</br>
`!gameserver add ip channelName`  
*Example:* `!gameserver add "127.0.0.1:1234" "general"`  
*Remarks:* Adds the specified game server and sets the channel where the info will be posted.  
</br>
`!gameserver remove ip`  
*Example:* `!gameserver remove 127.0.0.1:1234`  
*Remarks:* Removes the specified game server  

## Game Subscriptions
**Preconditions:** Minimum permission: *User*, Can only be used in a *channel*  
  
`!subscribe gameName`  
*Example:* `!Subscribe "Battlefield 1"`  
*Remarks:* Subscribes to the specified game. You will get a private message every time someone launches it  
</br>
`!unsubscribe gameName`  
*Example:* `!Unsubscribe "Battlefield 1"`  
*Remarks:* Unsubscribes to the specified game  

## Guild Configuration
**Preconditions:** Minimum permission: *ServerAdmin*, Can only be used in a *channel*  
  
`!setwelcomemessage welcomeMsg`  
*Example:* `!SetWelcomeMessage "Hello %user%, welcome to %server$"`  
*Remarks:* Sets the welcome message for new users. Can make use of %user% and %server%  
</br>
`!addrule rule`  
*Example:* `!AddRule "You shall not pass!"`  
*Remarks:* Adds a rule to the server.  
</br>
`!removerules `  
*Remarks:* Removes all rules from a server.  
</br>
`!addfeedurl url`  
*Remarks:* Adds an atom or RSS feed to the list of listened feeds.  
</br>
`!removefeedurl url`  
*Remarks:* Removes the specified feed from the list of feeds.  
</br>
`!removefeedurls `  
*Remarks:* Removes all feed urls  
</br>
`!enablefeeds channelName`  
*Example:* `!EnableFeeds general`  
*Remarks:* Enables the feed listener in the specified channel  
</br>
`!disablefeeds `  
*Remarks:* Disables the feed listener  

## Help
**Preconditions:** Minimum permission: *User*  
  
`!help `  
*Remarks:* List all usable commands.  
</br>
`!help command`  
*Example:* `!help Chuck`  
*Remarks:* Gets help for the specified command  

## Info
`!rules `  
*Preconditions:* Can only be used in a *channel*  
*Remarks:* The bot replies with the server rules in a private message  

## Simple poll
**Preconditions:** Can only be used in a *channel*, Minimum permission: *User*  
  
`!poll question`  
*Example:* `!poll "Is MonkeyBot awesome?"`  
*Remarks:* Starts a new poll with the specified question and automatically adds reactions  
</br>
`!poll question answers`  
*Example:* `!poll "How cool is MonkeyBot?" "supercool" "over 9000" "bruh..."`  
*Remarks:* Starts a new poll with the specified question and the list answers and automatically adds reactions  

## Roles
**Preconditions:** Minimum permission: *User*, Can only be used in a *channel*  
  
`!roles add roleName`  
*Example:* `!roles add bf`  
*Remarks:* Adds the specified role to your own roles.  
</br>
`!roles remove roleName`  
*Example:* `!roles remove bf`  
*Remarks:* Removes the specified role from your roles.  
</br>
`!roles list `  
*Remarks:* Lists all roles that can be mentioned and assigned.  
</br>
`!roles listmembers `  
*Remarks:* Lists all roles and the users who have these roles  
</br>
`!roles listmembers roleName`  
*Example:* `!roles listmembers bf`  
*Remarks:* Lists all the members of the specified role  

## Trivia
**Preconditions:** Minimum permission: *User*, Can only be used in a *channel*  
  
`!trivia start questionAmount`  
*Example:* `!trivia start 5`  
*Remarks:* Starts a new trivia with the specified amount of questions.  
</br>
`!trivia stop `  
*Remarks:* Stops a running trivia  
</br>
`!trivia skip `  
*Remarks:* Skips the current question  
</br>
`!trivia scores amount`  
*Example:* `!trivia scores 10`  
*Remarks:* Gets the global scores  

## Xkcd
`!xkcd arg`  
*Example:* `!xkcd latest`  
*Remarks:* Gets a random xkcd comic or the latest xkcd comic by appending "latest" to the command  
</br>
`!xkcd number`  
*Example:* `!xkcd 101`  
*Remarks:* Gets the xkcd comic with the specified number  
</details>
