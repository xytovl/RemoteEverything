# RemoteEverything
Http interface for KSP plugins

For users
=========
This plugin lets you interact with KSP with a web browser.

For mod developers
==================
* Copy RemoteEverythingWrapper/RemoteEverythingWrapper.cs into your project
* Change the namespace
* Add [Remotable] annotations to the fields you want to export
* Call the RemotableContainer.Register() function for each instance of the objects you want to export. The logicalId parameter is used to group objects that should be related, for instance ou can use the root part flightId of a vessel to group objects related to a vessel, or the namespace of your mod for singleton objects.
* Call RemotableContainer.Unregister() when your object should not be visible anymore

Tips
====
Remotable annotation has a displayName parameter, which lets you customize what should be shown in the web UI
You can manually register the fields with ManualRegisterMember if annotations are not suitable for your mod
