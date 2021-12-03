# AttachmentsOffset - Inactive
## Intro
Attempt was to rework my old mod labeled similarly, more into direct clientside way with its own UI and without having to rely on dummy items that acted as attachment points which would just offset where attachments actually were. This had a few issues like increasing load times a ton due to how EFT has to prepare all different combinations of attachments at load and generally wasn't very user-intuitive.
## Issue / why it got abandoned
### History
Before attempts for actual serialization, it used to use the server as "serialization backend", meaning it would just save the attachment stuff on there and there alone. However this caused some issues, as it wasn't serialized on the client side at all. It would work when messing with it in main menu and also load properly in raid, but at raid end, when items shuffle their reference IDs, the server would lose track of what item the offsets were actually supposed to be on, and therefore they would reset.
### Current
(Taken from my messages on Discord, as this was like 3 months ago)
> Problem was with getting the "offset" component to serialize and eventually there was an obstacle in the sense of a class that needed to be inherited from but it had a method that was internal, meaning that custom assemblies can't inherit from it without some major C# surgery

This method is "internal abstract", meaning it won't let external assemblies see it, but it also NEEDS to be overridden. However I don't remember the actual target method.


[Here's a preview of what the mod did (Discord link => will download a .mp4!)](https://cdn.discordapp.com/attachments/875803116409323562/882375370991624192/eN3z6fSH5L.mp4)
