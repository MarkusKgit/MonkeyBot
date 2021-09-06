## Full list of Commands (grouped by module):

- [Announcements](#announcements)
- [Benzen Facts](#benzen-facts)
- [Cat pictures](#cat-pictures)
- [Chuck Norris jokes](#chuck-norris-jokes)
- [Dog pictures](#dog-pictures)
- [Feeds](#feeds)
- [Guild Info](#guild-info)
- [Guild Configuration](#guild-configuration)
- [Help](#help)
- [Moderator Commands](#moderator-commands)
- [Misc](#misc-stuff)
- [Owner Only Commands](#owner-commands)
- [Picture Search](#picture-search)
- [Simple poll](#simple-poll)
- [Role Buttons](#role-buttons-management)
- [Self Role assignment](#self-role-management)
- [Trivia](#trivia)
- [Xkcd](#xkcd-comics)


## Announcements
### announcements addrecurring
Adds the specified recurring announcement to the specified channel  
#### Usage
`!announcements addrecurring announcementId cronExpression announcement (channel)`  
├ `announcementId (string): The id of the announcement.`  
├ `cronExpression (string): The cron expression to use.`  
├ `announcement (string): The message to announce.`  
├ `channel (channel): Optional: The channel where the announcement should be posted`  
### Example usage
`!announcements addrecurring "weeklyMsg1" "0 19 * * 5" "It is Friday 19:00" "general"`
### announcements addsingle
Adds the specified single announcement at the given time to the specified channel  
#### Usage
`!announcements addsingle announcementId time announcement (channel)`  
├ `announcementId (string): The id of the announcement.`  
├ `time (string): The time when the message should be announced.`  
├ `announcement (string): The message to announce.`  
├ `channel (channel): Optional: The channel where the announcement should be posted`  
### Example usage
`!announcements addsingle "reminder1" "19:00" "It is 19:00" "general"`
### announcements list
Lists all upcoming announcements  
#### Usage
`!announcements list `  

### announcements remove
Removes the announcement with the specified ID  
#### Usage
`!announcements remove id`  
├ `id (string): The id of the announcement.`  
### Example usage
`!announcements remove announcement1`
### announcements nextrun
Gets the next execution time of the announcement with the specified ID.  
#### Usage
`!announcements nextrun id`  
├ `id (string): The id of the announcement.`  
### Example usage
`!announcements nextrun announcement1`

---  

## Benzen Facts
### benzen
Returns a random fact about Benzen  
#### Usage
`!benzen `  

### addbenzenfact
Add a fact about Benzen  
#### Usage
`!addbenzenfact fact`  
├ `fact (string): The fact you want to add`  

---  

## Cat pictures
### cat
Gets a random Cat picture. Optionally a breed can be provided.  
### Aliases
cate ,kitty ,pussy  
#### Usage
`!cat (breed)`  
├ `breed (string): Optional: The breed of the cat`  
### Required permissions
You need to be User, Can only be used in a guild (not in DMs)
### catbreeds
Gets a list of available cat breeds.  
### Aliases
catebreeds ,kittybreeds ,pussybreeds  
#### Usage
`!catbreeds `  

### Required permissions
You need to be User, Can only be used in a guild (not in DMs)

---  

## Chuck Norris jokes
### chuck
Gets a random Chuck Norris fact and replaces Chuck Norris with the given name.  
#### Usage
`!chuck `  

`!chuck user`  
├ `user (user): The person to chuck`  
### Required permissions
You need to be User, Can only be used in a guild (not in DMs)

---  

## Dog pictures
### dog
Gets a random Dog picture. Optionally a breed can be provided.  
### Aliases
dogger ,doggo  
#### Usage
`!dog (breed)`  
├ `breed (string): Optional: The breed of the dogger`  
### Required permissions
You need to be User, Can only be used in a guild (not in DMs)
### dogbreeds
Gets a list of available dog breeds.  
### Aliases
doggerbreeds ,doggobreeds  
#### Usage
`!dogbreeds `  

### Required permissions
You need to be User, Can only be used in a guild (not in DMs)

---  

## Feeds
### feeds
View all options to manage feeds  
#### Usage
`!feeds `  

### Required permissions
You need to be ServerAdmin, Can only be used in a guild (not in DMs)
### addfeed
Adds an atom or RSS feed to the list of listened feeds.  
#### Usage
`!addfeed name url (channel)`  
├ `name (string): The name/title of the feed`  
├ `url (string): The url to the feed (Atom/RSS)`  
├ `channel (channel): Optional: The channel where the Feed updates should be posted. Defaults to current channel`  
### Required permissions
You need to be ServerAdmin, Can only be used in a guild (not in DMs)
### Example usage
`!AddFeed DotNet https://blogs.msdn.microsoft.com/dotnet/feed/`
`!AddFeed DotNet https://blogs.msdn.microsoft.com/dotnet/feed/ #news`
### removefeed
Removes the specified feed from the list of feeds.  
#### Usage
`!removefeed nameOrUrl`  
├ `nameOrUrl (string): The name or the url of the feed`  
### Required permissions
You need to be ServerAdmin, Can only be used in a guild (not in DMs)
### Example usage
`!RemoveFeed DotNet`
`!RemoveFeed https://blogs.msdn.microsoft.com/dotnet/feed/`
### listfeeds
List all current feed urls  
#### Usage
`!listfeeds (channel)`  
├ `channel (channel): Optional: The channel where the Feed urls should be listed for. Defaults to all channels`  
### Required permissions
You need to be ServerAdmin, Can only be used in a guild (not in DMs)
### Example usage
`!ListFeeds #news`
### removeallfeeds
Removes all feed urls  
#### Usage
`!removeallfeeds (channel)`  
├ `channel (channel): Optional: The channel where the Feed urls should be removed. Defaults to all channels`  
### Required permissions
You need to be ServerAdmin, Can only be used in a guild (not in DMs)
### Example usage
`!RemoveAllFeeds #news`

---  

## Guild Info
### rules
The bot replies with the server rules  
#### Usage
`!rules `  

### Required permissions
Can only be used in a guild (not in DMs)

---  

## Guild Configuration
### setprefix
Sets the command prefix the bot will react to from there on  
### Aliases
setcommandprefix  
#### Usage
`!setprefix prefix`  
├ `prefix (string): The new command prefix`  
### Required permissions
You need to be ServerAdmin, Can only be used in a guild (not in DMs)
### Example usage
`!SetPrefix >`
### setdefaultchannel
Sets the default channel for the guild where info will be posted  
#### Usage
`!setdefaultchannel channel`  
├ `channel (channel): The channel which should become the default`  
### Required permissions
You need to be ServerAdmin, Can only be used in a guild (not in DMs)
### Example usage
`!SetDefaultChannel #general`
### setwelcomemessage
Sets the welcome message for new users. Can make use of %user% and %server%  
#### Usage
`!setwelcomemessage welcomeMsg`  
├ `welcomeMsg (string): The welcome message`  
### Required permissions
You need to be ServerAdmin, Can only be used in a guild (not in DMs)
### Example usage
`!SetWelcomeMessage Hello %user%, welcome to %server%`
### setwelcomechannel
Sets the channel where the welcome message will be posted  
#### Usage
`!setwelcomechannel channel`  
├ `channel (channel): The channel where the welcome message should be posted`  
### Required permissions
You need to be ServerAdmin, Can only be used in a guild (not in DMs)
### Example usage
`!SetWelcomeChannel #general`
### setgoodbyemessage
Sets the Goodbye message for new users. Can make use of %user% and %server%  
#### Usage
`!setgoodbyemessage goodbyeMsg`  
├ `goodbyeMsg (string): The Goodbye message`  
### Required permissions
You need to be ServerAdmin, Can only be used in a guild (not in DMs)
### Example usage
`!SetGoodbyeMessage Goodbye %user%, farewell!`
### setgoodbyechannel
Sets the channel where the Goodbye message will be posted  
#### Usage
`!setgoodbyechannel channel`  
├ `channel (channel): The channel where the goodbye message should be posted`  
### Required permissions
You need to be ServerAdmin, Can only be used in a guild (not in DMs)
### Example usage
`!SetGoodbyeChannel #general`
### addrule
Adds a rule to the server.  
#### Usage
`!addrule rule`  
├ `rule (string): The rule to add`  
### Required permissions
You need to be ServerAdmin, Can only be used in a guild (not in DMs)
### Example usage
`!AddRule You shall not pass!`
### removerules
Removes all rules from a server.  
#### Usage
`!removerules `  

### Required permissions
You need to be ServerAdmin, Can only be used in a guild (not in DMs)
### enablebattlefieldupdates
Enables automated posting of Battlefield update news in provided channel  
#### Usage
`!enablebattlefieldupdates channel`  
├ `channel (channel): The channel where the Battlefield updates should be posted`  
### Required permissions
You need to be ServerAdmin, Can only be used in a guild (not in DMs)
### Example usage
`!EnableBattlefieldUpdates #general`
### disablebattlefieldupdates
Disables automated posting of Battlefield update news  
#### Usage
`!disablebattlefieldupdates `  

### Required permissions
You need to be ServerAdmin, Can only be used in a guild (not in DMs)
### enablestreamingnotifications
Enables automated notifications of people that start streaming (if they have enabled it for themselves). Info will be posted in the default channel of the guild  
#### Usage
`!enablestreamingnotifications `  

### Required permissions
You need to be ServerAdmin, Can only be used in a guild (not in DMs)
### Example usage
`!EnableStreamingNotifications`
### disablestreamingnotifications
Disables automated notifications of people that start streaming  
#### Usage
`!disablestreamingnotifications `  

### Required permissions
You need to be ServerAdmin, Can only be used in a guild (not in DMs)
### announcemystreams
Enable automatic posting of your stream info when you start streaming  
#### Usage
`!announcemystreams `  

### Required permissions
You need to be ServerAdmin, Can only be used in a guild (not in DMs), You need to be User

---  

## Help
### help
Displays command help.  
#### Usage
`!help command`  
├ `command (string): Command to provide help for.`  

---  

## Moderator Commands
### prune
Deletes the specified amount of messages for the specified user  
#### Usage
`!prune (count)`  
├ `count (int): The amount of messages to delete`  
`!prune user (count)`  
├ `user (user): The user whose messages should be deleted`  
├ `count (int): The amount of the user's messages to delete`  
### Required permissions
You need to be ServerMod, Can only be used in a guild (not in DMs)
### Example usage
`!Prune 10`
`!Prune @JohnDoe 10`

---  

## Misc stuff
### findmessageid
Gets the message id of a message in the current channel with the provided message text  
#### Usage
`!findmessageid messageContent`  
├ `messageContent (string): The content of the message to search for`  
### Required permissions
Can only be used in a guild (not in DMs)
### Example usage
`!FindMessageID The quick brown fox jumps over the lazy dog`
### lmgtfy
Generate a 'let me google that for you' link  
#### Usage
`!lmgtfy searchText`  
├ `searchText (string): Search Text`  
### Example usage
`!lmgtfy Monkey Gamers`

---  

## Owner Commands
### say
Say something in a specific guild's channel  
#### Usage
`!say guildId channelId message`  
├ `guildId (unsigned long): Id of the guild where to post`  
├ `channelId (unsigned long): Id of the text channel where to post`  
├ `message (string): Message to post`  
### Required permissions
You need to be BotOwner
### listguilds
List all the guilds the Bot joined  
#### Usage
`!listguilds `  

### Required permissions
You need to be BotOwner
### addowner
Adds the specified user to the list of bot owners  
#### Usage
`!addowner user`  
├ `user (user): The user to add as an owner`  
### Required permissions
You need to be BotOwner
### removeowner
Removes the specified user from the list of bot owners  
#### Usage
`!removeowner user`  
├ `user (user): The user to remove from the owners`  
### Required permissions
You need to be BotOwner

---  

## Picture search
### picture
Gets a random picture for the given search term.  
### Aliases
pic  
#### Usage
`!picture searchterm`  
├ `searchterm (string): The term to search for`  
### Required permissions
You need to be User, Can only be used in a guild (not in DMs)

---  

## Simple poll
### poll
Starts a new poll with the specified question and automatically adds reactions  
### Aliases
vote  
#### Usage
`!poll `  

### Required permissions
Can only be used in a guild (not in DMs), You need to be User, Requires Bot permission: AddReactions, ManageMessages

---  

## Role Buttons management
### addrolelink
Adds a reaction to the specified message with a link to the specified role  
#### Usage
`!addrolelink message role emoji`  
├ `message (message): Message to set up the link for`  
├ `role (role): Role to link`  
├ `emoji (emoji): Emoji to link`  
### Required permissions
You need to be ServerAdmin, Requires Bot permission: AddReactions, ManageMessages, ManageRoles
### removerolelink
Removes a reaction from the specified message with a link to the specified role  
#### Usage
`!removerolelink message role`  
├ `message (message): Message to remove the link from`  
├ `role (role): Role to remove the link from`  
### Required permissions
You need to be ServerAdmin, Requires Bot permission: AddReactions, ManageMessages, ManageRoles
### removeallrolelinks
Removes all Role Button Links  
#### Usage
`!removeallrolelinks `  

### Required permissions
You need to be ServerAdmin, Requires Bot permission: AddReactions, ManageMessages, ManageRoles
### listrolelinks
Lists all Role Button Links  
#### Usage
`!listrolelinks `  

### Required permissions
You need to be ServerAdmin, Requires Bot permission: AddReactions, ManageMessages, ManageRoles

---  

## Self role management
### giverole
Adds the specified role to your own roles.  
### Aliases
grantrole ,addrole  
#### Usage
`!giverole role`  
├ `role (role): The role you want to have`  
### Required permissions
You need to be User, Can only be used in a guild (not in DMs)
### Example usage
`!giverole @bf`
### removerole
Removes the specified role from your roles.  
### Aliases
revokerole  
#### Usage
`!removerole role`  
├ `role (role): The role you want to get rid of`  
### Required permissions
You need to be User, Can only be used in a guild (not in DMs)
### Example usage
`!RemoveRole @bf`
### listroles
Lists all roles that can be mentioned and assigned.  
#### Usage
`!listroles `  

### Required permissions
You need to be User, Can only be used in a guild (not in DMs)
### listroleswithmembers
Lists all roles and the users who have these roles  
#### Usage
`!listroleswithmembers `  

### Required permissions
You need to be User, Can only be used in a guild (not in DMs)
### listrolemembers
Lists all the members of the specified role  
#### Usage
`!listrolemembers role`  
├ `role (role): The role to display members for`  
### Required permissions
You need to be User, Can only be used in a guild (not in DMs)
### Example usage
`!ListRoleMembers @bf`

---  

## Trivia
### trivia
Starts a new trivia with the specified amount of questions.  
#### Usage
`!trivia (questionAmount)`  
├ `questionAmount (int): The number of questions to play.`  
### Required permissions
You need to be User, Can only be used in a guild (not in DMs)
### Example usage
`!trivia 5`
### stop
Stops a running trivia  
#### Usage
`!stop `  

### Required permissions
You need to be User, Can only be used in a guild (not in DMs)
### triviascores
Gets the global high scores  
#### Usage
`!triviascores (amount)`  
├ `amount (int): The amount of scores to get.`  
### Required permissions
You need to be User, Can only be used in a guild (not in DMs)
### Example usage
`!triviascores 10`

---  

## Xkcd comics
### xkcd
Gets a random xkcd comic if the argument is left empty. Gets the latest xkcd comment by supplying "latest" as the arg or a specific comic by supplying the number  
#### Usage
`!xkcd (arg)`  
├ `arg (string): Random comic if left empty, specific comic by number or latest by supplying "latest"`  
### Example usage
`!xkcd`
`!xkcd 101`
`!xkcd latest`

---  
